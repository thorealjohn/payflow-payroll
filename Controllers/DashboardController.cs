using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Utilities;
using itpayroll.ViewModels;
using itpayroll.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string period = "ThisMonth", string customDateFrom = null, string customDateTo = null)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var isHrOrAdmin = User.IsInRole(Roles.SuperAdmin) || User.IsInRole(Roles.Admin) || User.IsInRole(Roles.HR);

            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;

            // Analytics data for HR/Admin
            if (isHrOrAdmin)
            {
                // Monthly payroll totals for chart
                var monthlyPayrollQuery = _context.Payrolls.AsQueryable();

                if (from.HasValue)
                    monthlyPayrollQuery = monthlyPayrollQuery.Where(p => p.PeriodStart >= from.Value);
                if (to.HasValue)
                    monthlyPayrollQuery = monthlyPayrollQuery.Where(p => p.PeriodEnd <= to.Value);

                var monthlyPayrollRaw = await monthlyPayrollQuery
                    .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Total = g.Sum(p => p.NetPay)
                    })
                    .ToListAsync();

                // Format month string on client side (after data is in memory)
                var monthlyPayroll = monthlyPayrollRaw
                    .Select(x => new
                    {
                        Month = $"{x.Year}-{x.Month:D2}",
                        Total = x.Total
                    })
                    .OrderBy(x => x.Month)
                    .ToList();

                // Attendance trends
                var attendanceQuery = _context.Attendances.AsQueryable();

                if (from.HasValue)
                    attendanceQuery = attendanceQuery.Where(a => a.Date >= from.Value);
                if (to.HasValue)
                    attendanceQuery = attendanceQuery.Where(a => a.Date <= to.Value);

                var attendanceTrend = await attendanceQuery
                    .GroupBy(a => a.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                // Leave statistics
                var leaveQuery = _context.LeaveRequests.AsQueryable();

                if (from.HasValue)
                    leaveQuery = leaveQuery.Where(l => l.StartDate >= from.Value);
                if (to.HasValue)
                    leaveQuery = leaveQuery.Where(l => l.EndDate <= to.Value);

                var leaveStats = await leaveQuery
                    .GroupBy(l => l.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                ViewBag.MonthlyPayroll = monthlyPayroll;
                ViewBag.AttendanceTrend = attendanceTrend;
                ViewBag.LeaveStats = leaveStats;
            }

            if (isHrOrAdmin)
            {
                var totalEmployees = await _context.Employees.CountAsync(e => e.Status == Models.EmploymentStatus.Active);

                var payrollQuery = _context.Payrolls.AsQueryable();
                if (from.HasValue)
                    payrollQuery = payrollQuery.Where(p => p.PeriodStart >= from.Value);
                if (to.HasValue)
                    payrollQuery = payrollQuery.Where(p => p.PeriodEnd <= to.Value);

                var totalPayrolls = await payrollQuery.CountAsync();
                var totalPayrollAmount = await payrollQuery.SumAsync(p => p.NetPay);

                var attendanceQuery = _context.Attendances.AsQueryable();
                if (from.HasValue)
                    attendanceQuery = attendanceQuery.Where(a => a.Date >= from.Value);
                if (to.HasValue)
                    attendanceQuery = attendanceQuery.Where(a => a.Date <= to.Value);

                var todayAttendance = await attendanceQuery.CountAsync(a => a.Date == DateTime.Today);
                var pendingPayrolls = await payrollQuery.CountAsync(p => p.Status == Models.PayrollStatus.Processed);

                ViewBag.TotalEmployees = totalEmployees;
                ViewBag.TotalPayrolls = totalPayrolls;
                ViewBag.TodayAttendance = todayAttendance;
                ViewBag.TotalPayrollAmount = totalPayrollAmount;
                ViewBag.PendingPayrolls = pendingPayrolls;

                var recentPayrolls = await payrollQuery
                    .Include(p => p.Employee)
                    .ThenInclude(e => e.User)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var recentAttendances = await attendanceQuery
                    .Include(a => a.Employee)
                    .ThenInclude(e => e.User)
                    .OrderByDescending(a => a.Date)
                    .ThenByDescending(a => a.TimeIn)
                    .Take(5)
                    .ToListAsync();

                ViewBag.RecentPayrolls = recentPayrolls;
                ViewBag.RecentAttendances = recentAttendances;
            }
            else
            {
                var userId = User.Identity.Name;
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee != null)
                {
                    var myPayrollsQuery = _context.Payrolls
                        .Where(p => p.EmployeeId == employee.EmployeeId)
                        .AsQueryable();

                    if (from.HasValue)
                        myPayrollsQuery = myPayrollsQuery.Where(p => p.PeriodStart >= from.Value);
                    if (to.HasValue)
                        myPayrollsQuery = myPayrollsQuery.Where(p => p.PeriodEnd <= to.Value);

                    var myPayrolls = await myPayrollsQuery
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(5)
                        .ToListAsync();

                    var myAttendance = await _context.Attendances
                        .Where(a => a.EmployeeId == employee.EmployeeId)
                        .OrderByDescending(a => a.Date)
                        .Take(5)
                        .ToListAsync();

                    var totalNetPay = await _context.Payrolls
                        .Where(p => p.EmployeeId == employee.EmployeeId)
                        .SumAsync(p => p.NetPay);

                    ViewBag.Employee = employee;
                    ViewBag.MyPayrolls = myPayrolls;
                    ViewBag.MyAttendance = myAttendance;
                    ViewBag.TotalNetPay = totalNetPay;
                }
            }

            return View();
        }

        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var employee = await _context.Employees
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ProfileViewModel
            {
                FirstName = user?.FirstName ?? "",
                LastName = user?.LastName ?? "",
                Email = user?.Email ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                EmployeeNumber = employee.EmployeeNumber,
                BasicSalary = employee.BasicSalary,
                HireDate = employee.HireDate,
                ShiftName = employee.Shift?.ShiftName ?? "Not Assigned",
                Status = employee.Status.ToString()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update user info
            if (user != null)
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
