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
using Microsoft.AspNetCore.Hosting;

namespace itpayroll.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostingEnvironment = hostingEnvironment;
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
                // Monthly payroll totals for chart - materialize fully before next query
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

                // Attendance trends - only start after payroll query materialized
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

                // Leave statistics - only start after attendance query materialized
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
                ViewBag.ShowPayrollChart = monthlyPayroll.Count >= 3;
                ViewBag.ShowAttendanceChart = attendanceTrend.Count >= 5;
            }

            if (isHrOrAdmin)
            {
                var totalEmployees = await _context.Employees.CountAsync(e => e.Status == Models.EmploymentStatus.Active);

                // Payroll stats - materialize all payroll data first
                var payrollQuery = _context.Payrolls.AsQueryable();
                if (from.HasValue)
                    payrollQuery = payrollQuery.Where(p => p.PeriodStart >= from.Value);
                if (to.HasValue)
                    payrollQuery = payrollQuery.Where(p => p.PeriodEnd <= to.Value);

                // Execute payroll queries sequentially and materialize
                var totalPayrolls = await payrollQuery.CountAsync();
                var totalPayrollAmount = await payrollQuery.SumAsync(p => p.NetPay);
                var pendingPayrolls = await payrollQuery.CountAsync(p => p.Status == Models.PayrollStatus.Processed);
                var monthlyPayrollExpense = await _context.Payrolls
                    .Where(p => p.PeriodStart.Month == DateTime.Today.Month
                        && p.PeriodStart.Year == DateTime.Today.Year)
                    .SumAsync(p => p.NetPay);

                // Attendance stats - start only after payroll queries are done
                var attendanceQuery = _context.Attendances.AsQueryable();
                if (from.HasValue)
                    attendanceQuery = attendanceQuery.Where(a => a.Date >= from.Value);
                if (to.HasValue)
                    attendanceQuery = attendanceQuery.Where(a => a.Date <= to.Value);

                var todayAttendance = await attendanceQuery.CountAsync(a => a.Date == DateTime.Today);

                ViewBag.TotalEmployees = totalEmployees;
                ViewBag.TotalPayrolls = totalPayrolls;
                ViewBag.TodayAttendance = todayAttendance;
                ViewBag.TotalPayrollAmount = totalPayrollAmount;
                ViewBag.PendingPayrolls = pendingPayrolls;
                ViewBag.MonthlyPayrollExpense = monthlyPayrollExpense;

                // Recent data - materialize separately
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

                // Operational awareness signals for alerts panel
                var openAttendanceCount = await _context.Attendances
                    .CountAsync(a => a.Date == DateTime.Today && (a.TimeOut == default || a.TimeOut <= a.TimeIn));

                var incompleteAttendanceEmployee = await _context.Attendances
                    .Include(a => a.Employee)
                    .ThenInclude(e => e.User)
                    .Where(a => a.Date == DateTime.Today && (a.TimeOut == default || a.TimeOut <= a.TimeIn))
                    .OrderByDescending(a => a.TimeIn)
                    .Select(a => a.Employee.User.FirstName + " " + a.Employee.User.LastName)
                    .FirstOrDefaultAsync();

                var upcomingPayrollDate = await _context.Payrolls
                    .Where(p => p.PeriodEnd >= DateTime.Today && p.Status != Models.PayrollStatus.Released)
                    .OrderBy(p => p.PeriodEnd)
                    .Select(p => (DateTime?)p.PeriodEnd)
                    .FirstOrDefaultAsync();

                int upcomingPayrollEmployees = 0;
                decimal upcomingPayrollEstimate = 0;
                if (upcomingPayrollDate.HasValue)
                {
                    upcomingPayrollEmployees = await _context.Payrolls
                        .Where(p => p.PeriodEnd == upcomingPayrollDate.Value && p.Status != Models.PayrollStatus.Released)
                        .Select(p => p.EmployeeId)
                        .Distinct()
                        .CountAsync();

                    upcomingPayrollEstimate = await _context.Payrolls
                        .Where(p => p.PeriodEnd == upcomingPayrollDate.Value && p.Status != Models.PayrollStatus.Released)
                        .SumAsync(p => p.NetPay);
                }

                ViewBag.IncompleteAttendanceEmployee = incompleteAttendanceEmployee;
                ViewBag.OpenAttendanceCount = openAttendanceCount;
                ViewBag.UpcomingPayrollDate = upcomingPayrollDate;
                ViewBag.UpcomingPayrollEmployees = upcomingPayrollEmployees;
                ViewBag.UpcomingPayrollEstimate = upcomingPayrollEstimate;
            }
            else
            {
                var user = await _userManager.GetUserAsync(User);
                var userId = user?.Id;
                var employee = await _context.Employees
                    .Include(e => e.Shift)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee != null)
                {
                    // My Payrolls - materialize first
                    var myPayrollsQuery = _context.Payrolls
                        .Where(p => p.EmployeeId == employee.EmployeeId);

                    if (from.HasValue)
                        myPayrollsQuery = myPayrollsQuery.Where(p => p.PeriodStart >= from.Value);
                    if (to.HasValue)
                        myPayrollsQuery = myPayrollsQuery.Where(p => p.PeriodEnd <= to.Value);

                    var myPayrolls = await myPayrollsQuery
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(5)
                        .ToListAsync();

                    // My Attendance - start after payroll query materialized
                    var myAttendance = await _context.Attendances
                        .Where(a => a.EmployeeId == employee.EmployeeId)
                        .OrderByDescending(a => a.Date)
                        .Take(5)
                        .ToListAsync();

                    // KPIs - sequential queries
                    var totalNetPay = await _context.Payrolls
                        .Where(p => p.EmployeeId == employee.EmployeeId)
                        .SumAsync(p => p.NetPay);

                    // Employee KPI: Today's Attendance Status
                    var todayAttendance = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == DateTime.Today);

                    string todayStatus = "Absent";
                    if (todayAttendance != null)
                    {
                        todayStatus = todayAttendance.LateMinutes > 0 ? "Late" : "Present";
                    }
                    ViewBag.TodayAttendanceStatus = todayStatus;
                    ViewBag.TodayAttendance = todayAttendance;

                    // Employee KPI: This Month Hours Worked
                    var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    var monthHoursWorked = await _context.Attendances
                        .Where(a => a.EmployeeId == employee.EmployeeId && a.Date >= monthStart && a.Date <= DateTime.Today)
                        .SumAsync(a => (decimal?)a.TotalHours) ?? 0;
                    ViewBag.MonthHoursWorked = monthHoursWorked;

                    // Employee KPI: Pending Requests Count - batch into one query
                    var pendingOvertime = await _context.Overtimes
                        .CountAsync(o => o.EmployeeId == employee.EmployeeId && o.Status == Models.OvertimeStatus.Pending);
                    var pendingLeave = await _context.LeaveRequests
                        .CountAsync(l => l.EmployeeId == employee.EmployeeId && l.Status == Models.LeaveStatus.Pending);
                    ViewBag.PendingRequestsCount = pendingOvertime + pendingLeave;

                    // Employee KPI: Latest Net Pay (Released)
                    var latestPayslip = await _context.Payrolls
                        .Where(p => p.EmployeeId == employee.EmployeeId && p.Status == Models.PayrollStatus.Released)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync();
                    ViewBag.LatestNetPay = latestPayslip?.NetPay;

                    // Last Request Status (for Recent Activity)
                    var lastOvertime = await _context.Overtimes
                        .Where(o => o.EmployeeId == employee.EmployeeId)
                        .OrderByDescending(o => o.CreatedAt)
                        .FirstOrDefaultAsync();
                    var lastLeave = await _context.LeaveRequests
                        .Where(l => l.EmployeeId == employee.EmployeeId)
                        .OrderByDescending(l => l.CreatedDate)
                        .FirstOrDefaultAsync();

                    ViewBag.LastOvertimeStatus = lastOvertime?.Status.ToString();
                    ViewBag.LastLeaveStatus = lastLeave?.Status.ToString();

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

            // Auto-create Employee record if missing but user is in Employee role
            if (employee == null && user != null && await _userManager.IsInRoleAsync(user, Roles.Employee))
            {
                employee = new Models.Employee
                {
                    UserId = userId ?? "",
                    EmployeeNumber = "EMP-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    Status = Models.EmploymentStatus.Active,
                    BasicSalary = 0,
                    HireDate = DateTime.UtcNow.Date,
                    CreatedBy = "System-Auto",
                    CreatedDate = DateTime.UtcNow
                };
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Reload with Shift included
                employee = await _context.Employees
                    .Include(e => e.Shift)
                    .FirstOrDefaultAsync(e => e.UserId == userId);
            }

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ProfileViewModel
            {
                FirstName = user?.FirstName ?? "",
                MiddleName = user?.MiddleName,
                LastName = user?.LastName ?? "",
                Suffix = user?.Suffix,
                DateOfBirth = user?.DateOfBirth,
                Gender = user?.Gender?.ToString(),
                CivilStatus = user?.CivilStatus?.ToString(),
                Nationality = user?.Nationality ?? "Filipino",
                Email = user?.Email ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                AlternatePhone = user?.AlternatePhone,
                AddressStreet = user?.AddressStreet,
                AddressBarangay = user?.AddressBarangay,
                AddressCity = user?.AddressCity,
                AddressProvince = user?.AddressProvince,
                AddressZipCode = user?.AddressZipCode,
                EmergencyContactName = user?.EmergencyContactName,
                EmergencyContactRelationship = user?.EmergencyContactRelationship,
                EmergencyContactPhone = user?.EmergencyContactPhone,
                ProfilePicturePath = user?.ProfilePicturePath,
                EmployeeNumber = employee.EmployeeNumber,
                Department = employee.Department,
                Position = employee.Position,
                EmploymentType = employee.EmploymentType,
                BasicSalary = employee.BasicSalary,
                SalaryType = employee.SalaryType,
                PayFrequency = employee.PayFrequency,
                BankName = employee.BankName,
                BankAccountNumber = employee.BankAccountNumber,
                TIN = employee.TIN,
                SSSNumber = employee.SSSNumber,
                PhilHealthNumber = employee.PhilHealthNumber,
                PagIBIGNumber = employee.PagIBIGNumber,
                HireDate = employee.HireDate,
                ShiftName = employee.Shift?.ShiftName ?? "Not Assigned",
                Status = employee.Status.ToString()
            };

            ViewBag.User = user;
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
                ViewBag.User = user;
                return View(model);
            }

            // Update user info
            if (user != null)
            {
                user.FirstName = model.FirstName;
                user.MiddleName = model.MiddleName;
                user.LastName = model.LastName;
                user.Suffix = model.Suffix;
                user.DateOfBirth = model.DateOfBirth?.Date;
                user.Nationality = model.Nationality;
                user.PhoneNumber = model.PhoneNumber;
                user.AlternatePhone = model.AlternatePhone;
                user.AddressStreet = model.AddressStreet;
                user.AddressBarangay = model.AddressBarangay;
                user.AddressCity = model.AddressCity;
                user.AddressProvince = model.AddressProvince;
                user.AddressZipCode = model.AddressZipCode;
                user.EmergencyContactName = model.EmergencyContactName;
                user.EmergencyContactRelationship = model.EmergencyContactRelationship;
                user.EmergencyContactPhone = model.EmergencyContactPhone;
                user.ModifiedBy = User.Identity?.Name ?? "System";
                user.ModifiedDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> EditEmployee(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Index));

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);
            
            if (employee == null)
            {
                TempData["Error"] = "Employee record not found.";
                return RedirectToAction(nameof(Profile));
            }

            // Update employee info (HR/Admin only)
            employee.Department = model.Department;
            employee.Position = model.Position;
            employee.EmploymentType = model.EmploymentType;
            employee.BasicSalary = model.BasicSalary;
            employee.SalaryType = model.SalaryType;
            employee.PayFrequency = model.PayFrequency;
            employee.BankName = model.BankName;
            employee.BankAccountNumber = model.BankAccountNumber;
            employee.TIN = model.TIN;
            employee.SSSNumber = model.SSSNumber;
            employee.PhilHealthNumber = model.PhilHealthNumber;
            employee.PagIBIGNumber = model.PagIBIGNumber;
            employee.ModifiedBy = User.Identity?.Name ?? "System";
            employee.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Employment information updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file.";
                return RedirectToAction(nameof(Profile));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only image files are allowed.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Profile));
            }

            var fileName = $"{user.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
            var uploadsPath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "profiles");

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.ProfilePicturePath = $"/uploads/profiles/{fileName}";
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile picture updated.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
