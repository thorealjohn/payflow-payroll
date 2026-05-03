using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
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

        public async Task<IActionResult> Index()
        {
            var isHrOrAdmin = User.IsInRole(Roles.SuperAdmin) || User.IsInRole(Roles.Admin) || User.IsInRole(Roles.HR);

            // Analytics data for HR/Admin
            if (isHrOrAdmin)
            {
                // Monthly payroll totals for chart
                var monthlyPayrollRaw = await _context.Payrolls
                    .Where(p => p.CreatedAt >= DateTime.UtcNow.AddMonths(-6))
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
                var attendanceTrend = await _context.Attendances
                    .Where(a => a.Date >= DateTime.UtcNow.AddDays(-30))
                    .GroupBy(a => a.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                // Leave statistics
                var leaveStats = await _context.LeaveRequests
                    .Where(l => l.CreatedDate >= DateTime.UtcNow.AddMonths(-3))
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
                var totalPayrolls = await _context.Payrolls.CountAsync();
                var todayAttendance = await _context.Attendances.CountAsync(a => a.Date == DateTime.Today);
                var totalPayrollAmount = await _context.Payrolls.SumAsync(p => p.NetPay);
                var pendingPayrolls = await _context.Payrolls.CountAsync(p => p.Status == Models.PayrollStatus.Processed);

                ViewBag.TotalEmployees = totalEmployees;
                ViewBag.TotalPayrolls = totalPayrolls;
                ViewBag.TodayAttendance = todayAttendance;
                ViewBag.TotalPayrollAmount = totalPayrollAmount;
                ViewBag.PendingPayrolls = pendingPayrolls;

                var recentPayrolls = await _context.Payrolls
                    .Include(p => p.Employee)
                    .ThenInclude(e => e.User)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var recentAttendances = await _context.Attendances
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
                    var myPayrolls = await _context.Payrolls
                        .Where(p => p.EmployeeId == employee.EmployeeId)
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
