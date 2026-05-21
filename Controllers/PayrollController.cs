using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Services;
using itpayroll.Utilities;
using itpayroll.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itpayroll.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;

namespace itpayroll.Controllers
{
    [Authorize]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PayrollService _payrollService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PayrollController(ApplicationDbContext context, PayrollService payrollService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _payrollService = payrollService;
            _userManager = userManager;
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Index(string searchString, string status, string period = "ThisMonth", string customDateFrom = "", string customDateTo = "", int page = 1)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var payrolls = _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            if (from.HasValue)
            {
                payrolls = payrolls.Where(p => p.PeriodStart >= from.Value);
            }

            if (to.HasValue)
            {
                payrolls = payrolls.Where(p => p.PeriodEnd <= to.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                payrolls = payrolls.Where(p =>
                    p.Employee.User != null &&
                    (p.Employee.User.FirstName.Contains(searchString) ||
                     p.Employee.User.LastName.Contains(searchString) ||
                     p.Employee.EmployeeNumber.Contains(searchString)));

                if (!string.IsNullOrEmpty(status))
                {
                    payrolls = payrolls.Where(p => p.Status.ToString() == status);
                }

            }


            int pageSize = 10;
            int totalPayrolls = await payrolls.CountAsync();

            ViewBag.CurrentFilter = searchString;
            ViewBag.Period = period;
            ViewBag.Status = status; 
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;
            ViewBag.TotalNet = await payrolls.SumAsync(p => p.NetPay);
            ViewBag.ProcessedCount = await payrolls.CountAsync(p => p.Status == PayrollStatus.Processed);
            ViewBag.ReleasedCount = await payrolls.CountAsync(p => p.Status == PayrollStatus.Released);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalPayrolls / pageSize);

            var result = await payrolls
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return View(result);
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Process()
        {
            await PopulateEmployeeDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Process(PayrollProcessViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateEmployeeDropdown();
                return View(model);
            }

            if (model.PeriodEnd < model.PeriodStart)
            {
                ModelState.AddModelError(nameof(model.PeriodEnd), "Period end must be after period start.");
                await PopulateEmployeeDropdown();
                return View(model);
            }

            var existingPayroll = await _context.Payrolls
                .AnyAsync(p => p.EmployeeId == model.EmployeeId
                    && p.PeriodStart == model.PeriodStart
                    && p.PeriodEnd == model.PeriodEnd);

            if (existingPayroll)
            {
                ModelState.AddModelError("", "Payroll for this employee and period already exists.");
                await PopulateEmployeeDropdown();
                return View(model);
            }

            try
            {
                var payroll = await _payrollService.ProcessPayrollAsync(
                    model.EmployeeId,
                    model.PeriodStart,
                    model.PeriodEnd);

                TempData["Success"] = $"Payroll processed successfully. Net Pay: {payroll.NetPay:C}";
                return RedirectToAction(nameof(Details), new { id = payroll.PayrollId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error processing payroll: {ex.Message}");
                await PopulateEmployeeDropdown();
                return View(model);
            }
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Details(int id)
        {
            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(p => p.PayrollId == id);

            if (payroll == null)
                return NotFound();

            var earnings = await _context.Earnings
                .Where(e => e.PayrollId == id)
                .ToListAsync();

            var deductions = await _context.Deductions
                .Where(d => d.PayrollId == id)
                .ToListAsync();

            var viewModel = new PayrollDetailViewModel
            {
                Payroll = payroll,
                Earnings = earnings,
                Deductions = deductions,
                Employee = payroll.Employee
            };
            var totalHours = await _context.Attendances
    .Where(a => a.EmployeeId == payroll.EmployeeId &&
                a.Date >= payroll.PeriodStart &&
                a.Date <= payroll.PeriodEnd)
    .SumAsync(a => a.TotalHours);

            var overtimeHours = await _context.Attendances
                .Where(a => a.EmployeeId == payroll.EmployeeId &&
                            a.Date >= payroll.PeriodStart &&
                            a.Date <= payroll.PeriodEnd)
                .SumAsync(a => a.OvertimeHours);

            viewModel.TotalHours = (decimal)totalHours;
            viewModel.OvertimeHours = (decimal)overtimeHours;

            return View(viewModel);
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Release(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll == null)
                return NotFound();

            if (payroll.Status != PayrollStatus.Processed)
            {
                TempData["Error"] = "Only processed payrolls can be released.";
                return RedirectToAction(nameof(Index));
            }

            payroll.Status = PayrollStatus.Released;
            payroll.ModifiedAt = DateTime.UtcNow;
            payroll.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payroll released successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> MyPayslips(string period = "ThisMonth", string customDateFrom = "", string customDateTo = "")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return View(new List<PayrollDetailViewModel>());
            }

            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.Payrolls
                .Where(p => p.EmployeeId == employee.EmployeeId);

            if (from.HasValue)
            {
                query = query.Where(p => p.PeriodStart >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(p => p.PeriodEnd <= to.Value);
            }

            var payrolls = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;

            var viewModel = new List<PayrollDetailViewModel>();

            foreach (var payroll in payrolls)
            {
                var earnings = await _context.Earnings
                    .Where(e => e.PayrollId == payroll.PayrollId)
                    .ToListAsync();

                var deductions = await _context.Deductions
                    .Where(d => d.PayrollId == payroll.PayrollId)
                    .ToListAsync();

                viewModel.Add(new PayrollDetailViewModel
                {
                    Payroll = payroll,
                    Earnings = earnings,
                    Deductions = deductions,
                    Employee = employee
                });
            }

            return View(viewModel);
        }

        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> MyPayslip(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
                return NotFound();

            var payroll = await _context.Payrolls
                .FirstOrDefaultAsync(p => p.PayrollId == id && p.EmployeeId == employee.EmployeeId);

            if (payroll == null)
                return NotFound();

            var earnings = await _context.Earnings
                .Where(e => e.PayrollId == id)
                .ToListAsync();

            var deductions = await _context.Deductions
                .Where(d => d.PayrollId == id)
                .ToListAsync();

            var viewModel = new PayrollDetailViewModel
            {
                Payroll = payroll,
                Earnings = earnings,
                Deductions = deductions,
                Employee = employee
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Void(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll == null)
                return NotFound();

            payroll.Status = PayrollStatus.Draft; // or create "Voided" enum if you want
            payroll.ModifiedAt = DateTime.UtcNow;
            payroll.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payroll voided successfully.";
            return RedirectToAction(nameof(Index));
        }


        private async Task PopulateEmployeeDropdown()
        {
            var employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.Status == EmploymentStatus.Active && e.User != null)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = $"{e.EmployeeNumber} - {e.User!.FirstName} {e.User.LastName}"
                })
                .ToListAsync();

            ViewBag.Employees = employees;
        }
    }
}
            
