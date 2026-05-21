using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Utilities;
using itpayroll.ViewModels;
using itpayroll.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize]
    public class OvertimeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OvertimeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Index(string searchString, string period = "ThisMonth", string? customDateFrom = null, string? customDateTo = null, int page = 1)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var overtimes = _context.Overtimes
                .Include(o => o.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            if (from.HasValue)
            {
                overtimes = overtimes.Where(o => o.Date >= from.Value);
            }

            if (to.HasValue)
            {
                overtimes = overtimes.Where(o => o.Date <= to.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                overtimes = overtimes.Where(o =>
                    o.Employee.User != null &&
                    (o.Employee.User.FirstName.Contains(searchString) ||
                    o.Employee.User.LastName.Contains(searchString) ||
                    o.Employee.EmployeeNumber.Contains(searchString)));
            }

            int pageSize = 10;
            int totalOvertimes = await overtimes.CountAsync();

            ViewBag.CurrentFilter = searchString;
            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalOvertimes / pageSize);

            var result = await overtimes
                .OrderByDescending(o => o.Date)
                .ThenBy(o => o.Status)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(result);
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Details(int id)
        {
            var overtime = await _context.Overtimes
                .Include(o => o.Employee)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(o => o.OvertimeId == id);

            if (overtime == null)
                return NotFound();

            return View(overtime);
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Edit(int id)
        {
            var overtime = await _context.Overtimes.FindAsync(id);
            if (overtime == null)
                return NotFound();

            var model = new OvertimeViewModel
            {
                OvertimeId = overtime.OvertimeId,
                EmployeeId = overtime.EmployeeId,
                Date = overtime.Date,
                Hours = overtime.Hours,
                Reason = overtime.Reason,
                Status = overtime.Status,
                ApprovedBy = overtime.ApprovedBy,
                RejectionReason = overtime.RejectionReason
            };

            await PopulateEmployeeDropdown();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Edit(int id, OvertimeViewModel model)
        {
            if (id != model.OvertimeId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateEmployeeDropdown();
                return View(model);
            }

            var overtime = await _context.Overtimes.FindAsync(id);
            if (overtime == null)
                return NotFound();

            overtime.Hours = model.Hours;
            overtime.Reason = model.Reason;
            overtime.Status = model.Status;
            overtime.RejectionReason = model.RejectionReason;

            if (model.Status == OvertimeStatus.Approved && string.IsNullOrEmpty(overtime.ApprovedBy))
            {
                overtime.ApprovedBy = User.Identity?.Name;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OvertimeExists(id))
                    return NotFound();
                throw;
            }

            TempData["Success"] = "Overtime record updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Approve(int id)
        {
            var overtime = await _context.Overtimes.FindAsync(id);
            if (overtime == null)
                return NotFound();

            overtime.Status = OvertimeStatus.Approved;
            overtime.ApprovedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Overtime approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var overtime = await _context.Overtimes.FindAsync(id);
            if (overtime == null)
                return NotFound();

            overtime.Status = OvertimeStatus.Rejected;
            overtime.RejectionReason = rejectionReason;
            overtime.ApprovedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Overtime rejected successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Delete(int id)
        {
            var overtime = await _context.Overtimes
                .Include(o => o.Employee)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(o => o.OvertimeId == id);

            if (overtime == null)
                return NotFound();

            return View(overtime);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var overtime = await _context.Overtimes.FindAsync(id);
            if (overtime == null)
                return NotFound();

            _context.Overtimes.Remove(overtime);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Overtime record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> MyOvertime(string period = "ThisMonth", string? customDateFrom = null, string? customDateTo = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.Overtimes
                .Include(o => o.Employee)
                .Where(o => o.Employee.UserId == userId)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(o => o.Date >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(o => o.Date <= to.Value);
            }

            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;

            var overtimes = await query
                .OrderByDescending(o => o.Date)
                .ToListAsync();

            return View(overtimes);
        }

        [Authorize(Roles = $"{Roles.Employee}")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> Create(OvertimeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return View(model);
            }

            var existingOvertime = await _context.Overtimes
                .AnyAsync(o => o.EmployeeId == employee.EmployeeId && o.Date == model.Date.Date);

            if (existingOvertime)
            {
                ModelState.AddModelError("", "You already have an overtime request for this date.");
                return View(model);
            }

            var overtime = new Overtime
            {
                EmployeeId = employee.EmployeeId,
                Date = model.Date.Date,
                Hours = model.Hours,
                Reason = model.Reason,
                Status = OvertimeStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Overtimes.Add(overtime);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Overtime request submitted successfully.";
            return RedirectToAction(nameof(MyOvertime));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);

            var employeeId = employee?.EmployeeId;
            var overtime = await _context.Overtimes
                .FirstOrDefaultAsync(o => o.OvertimeId == id &&
                                         o.EmployeeId == employeeId);

            if (overtime == null)
                return NotFound();

            if (overtime.Status != OvertimeStatus.Pending)
            {
                TempData["Error"] = "Only pending overtime requests can be cancelled.";
                return RedirectToAction(nameof(MyOvertime));
            }

            _context.Overtimes.Remove(overtime);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Overtime request cancelled.";
            return RedirectToAction(nameof(MyOvertime));
        }

        private bool OvertimeExists(int id)
        {
            return _context.Overtimes.Any(o => o.OvertimeId == id);
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
