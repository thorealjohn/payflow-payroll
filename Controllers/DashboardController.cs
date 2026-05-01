using itpayroll.Constant;
using itpayroll.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var isHrOrAdmin = User.IsInRole(Roles.SuperAdmin) || User.IsInRole(Roles.Admin) || User.IsInRole(Roles.HR);

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
    }
}
