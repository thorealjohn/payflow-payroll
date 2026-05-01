using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Services;
using itpayroll.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PayrollService _payrollService;

        public PayrollController(ApplicationDbContext context, PayrollService payrollService)
        {
            _context = context;
            _payrollService = payrollService;
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Index()
        {
            var payrolls = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payrolls);
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

            return View(viewModel);
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
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
        public async Task<IActionResult> MyPayslips()
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == User.Identity.Name);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return View(new List<PayrollDetailViewModel>());
            }

            var payrolls = await _context.Payrolls
                .Where(p => p.EmployeeId == employee.EmployeeId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

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
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == User.Identity.Name);

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

        private async Task PopulateEmployeeDropdown()
        {
            var employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.Status == EmploymentStatus.Active)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = $"{e.EmployeeNumber} - {e.User.FirstName} {e.User.LastName}"
                })
                .ToListAsync();

            ViewBag.Employees = employees;
        }
    }
}
