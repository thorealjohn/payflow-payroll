using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Services;
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
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AttendanceService _attendanceService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceController(ApplicationDbContext context, AttendanceService attendanceService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _attendanceService = attendanceService;
            _userManager = userManager;
        }

        private async Task<Shift?> GetCurrentShiftForEmployee(int employeeId)
        {
            var today = DateTime.Today;

            // 1. Check shift assignment first
            var assignment = await _context.EmployeeShiftAssignments
                .Include(a => a.Shift)
                .Where(a => a.EmployeeId == employeeId
                    && a.DateFrom <= today
                    && (a.DateTo == null || a.DateTo >= today))
                .Select(a => a.Shift)
                .FirstOrDefaultAsync();

            if (assignment != null)
                return assignment;

            // 2. Fallback to default shift (from Employee)
            var employee = await _context.Employees
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            return employee?.Shift;
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Index(string period = "ThisMonth", string? customDateFrom = null, string? customDateTo = null, int page = 1)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(a => a.Date >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.Date <= to.Value);
            }

            int pageSize = 10;
            int totalAttendances = await query.CountAsync();

            var attendances = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.TimeIn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalAttendances / pageSize);
            return View(attendances);
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Create()
        {
            await PopulateEmployeeDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Create(AttendanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateEmployeeDropdown();
                return View(model);
            }

            if (model.TimeOut <= model.TimeIn)
            {
                ModelState.AddModelError(nameof(model.TimeOut), "Time Out must be after Time In.");
                await PopulateEmployeeDropdown();
                return View(model);
            }

            var existingAttendance = await _context.Attendances
                .AnyAsync(a => a.EmployeeId == model.EmployeeId && a.Date == model.Date.Date);

            if (existingAttendance)
            {
                ModelState.AddModelError("", "Attendance record for this employee and date already exists.");
                await PopulateEmployeeDropdown();
                return View(model);
            }

            // Prevent self-editing (unless SuperAdmin)
            if (!User.IsInRole(Roles.SuperAdmin) && await IsOwnAttendance(model.EmployeeId))
            {
                ModelState.AddModelError("", "You cannot create or edit your own attendance record.");
                await PopulateEmployeeDropdown();
                return View(model);
            }

            var totalHours = (model.TimeOut - model.TimeIn).TotalHours;

            // Get employee's shift to calculate late/undertime
            var employee = await _context.Employees
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.EmployeeId == model.EmployeeId);

            var attendance = new Attendance
            {
                EmployeeId = model.EmployeeId,
                Date = model.Date.Date,
                TimeIn = model.TimeIn,
                TimeOut = model.TimeOut,
                TotalHours = totalHours,
                OvertimeHours = model.OvertimeHours,
                CreatedDate = DateTime.UtcNow
            };

            // Calculate late and undertime if employee has a shift
            if (employee?.Shift != null)
            {
                var (lateMinutes, undertimeMinutes) = _attendanceService.CalculateLateAndUndertime(
                    model.TimeIn, model.TimeOut, employee.Shift, model.Date.Date);
                attendance.LateMinutes = lateMinutes;
                attendance.UndertimeMinutes = undertimeMinutes;
            }

            // Calculate night shift hours
            attendance.NightShiftHours = _attendanceService.CalculateNightShiftHours(
                model.TimeIn, model.TimeOut, employee?.Shift, model.Date.Date);

            // Set day type
            attendance.DayType = model.DayType;

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Attendance recorded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Edit(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
                return NotFound();

            var model = new AttendanceViewModel
            {
                AttendanceId = attendance.AttendanceId,
                EmployeeId = attendance.EmployeeId,
                Date = attendance.Date,
                TimeIn = attendance.TimeIn,
                TimeOut = attendance.TimeOut,
                OvertimeHours = attendance.OvertimeHours,
                DayType = attendance.DayType
            };

            await PopulateEmployeeDropdown();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Edit(int id, AttendanceViewModel model)
        {
            if (id != model.AttendanceId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateEmployeeDropdown();
                return View(model);
            }

            if (model.TimeOut <= model.TimeIn)
            {
                ModelState.AddModelError(nameof(model.TimeOut), "Time Out must be after Time In.");
                await PopulateEmployeeDropdown();
                return View(model);
            }

            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
                return NotFound();

            // Prevent self-editing (unless SuperAdmin)
            if (!User.IsInRole(Roles.SuperAdmin) && await IsOwnAttendance(attendance.EmployeeId))
            {
                TempData["Error"] = "You cannot create or edit your own attendance record.";
                return RedirectToAction(nameof(Index));
            }

            attendance.TimeIn = model.TimeIn;
            attendance.TimeOut = model.TimeOut;
            attendance.TotalHours = (model.TimeOut - model.TimeIn).TotalHours;
            attendance.OvertimeHours = model.OvertimeHours;

            // Recalculate late and undertime
            var employee = await _context.Employees
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.EmployeeId == attendance.EmployeeId);

            if (employee?.Shift != null)
            {
                var (lateMinutes, undertimeMinutes) = _attendanceService.CalculateLateAndUndertime(
                    model.TimeIn, model.TimeOut, employee.Shift, attendance.Date);
                attendance.LateMinutes = lateMinutes;
                attendance.UndertimeMinutes = undertimeMinutes;
            }

            // Recalculate night shift hours
            attendance.NightShiftHours = _attendanceService.CalculateNightShiftHours(
                model.TimeIn, model.TimeOut, employee?.Shift, attendance.Date);

            // Update day type
            attendance.DayType = model.DayType;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttendanceExists(id))
                    return NotFound();
                throw;
            }

            TempData["Success"] = "Attendance updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Delete(int id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(a => a.AttendanceId == id);

            if (attendance == null)
                return NotFound();

            return View(attendance);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
                return NotFound();

            // Prevent self-editing (unless SuperAdmin)
            if (!User.IsInRole(Roles.SuperAdmin) && await IsOwnAttendance(attendance.EmployeeId))
            {
                TempData["Error"] = "You cannot delete your own attendance record.";
                return RedirectToAction(nameof(Index));
            }

            attendance.IsActive = !attendance.IsActive;
            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();

            TempData["Success"] = attendance.IsActive ? "Attendance record restored successfully." : "Attendance record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR},{Roles.Employee}")]
        public async Task<IActionResult> MyAttendance(string period = "ThisMonth", string? customDateFrom = null, string? customDateTo = null, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return View(new List<Attendance>());
            }

            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var query = _context.Attendances
                .Where(a => a.EmployeeId == employee.EmployeeId);

            if (from.HasValue)
            {
                query = query.Where(a => a.Date >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.Date <= to.Value);
            }

            int pageSize = 10;
            int total = await query.CountAsync();

            var attendances = await query
                .OrderByDescending(a => a.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(attendances);
        }

        private bool AttendanceExists(int id)
        {
            return _context.Attendances.Any(a => a.AttendanceId == id);
        }

        private async Task<Employee?> GetCurrentEmployee()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            return await _context.Employees
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.UserId == userId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR},{Roles.Employee}")]
        public async Task<IActionResult> TimeIn()
        {
            var employee = await GetCurrentEmployee();
            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return RedirectToAction(nameof(MyAttendance));
            }

            var today = DateTime.UtcNow.Date;
            var existing = await _context.Attendances
                .AnyAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == today);

            if (existing)
            {
                TempData["Error"] = "You already timed in today.";
                return RedirectToAction(nameof(MyAttendance));
            }

             var now = DateTime.UtcNow;
             var shift = await GetCurrentShiftForEmployee(employee.EmployeeId);
             var attendance = new Attendance
             {
                 EmployeeId = employee.EmployeeId,
                 Date = today,
                 TimeIn = new TimeSpan(now.Hour, now.Minute, now.Second),
                 ShiftId = shift?.ShiftId,
                 DayType = _attendanceService.IsRestDay(today, shift) ? DayType.RestDay : DayType.Regular,
                 CreatedDate = DateTime.UtcNow
             };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Time In recorded successfully.";
            return RedirectToAction(nameof(MyAttendance));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR},{Roles.Employee}")]
        public async Task<IActionResult> TimeOut()
        {
            var employee = await GetCurrentEmployee();
            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return RedirectToAction(nameof(MyAttendance));
            }

            var today = DateTime.UtcNow.Date;
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == today);

            if (attendance == null)
            {
                TempData["Error"] = "No Time In record found for today.";
                return RedirectToAction(nameof(MyAttendance));
            }

            if (attendance.TimeOut != default)
            {
                TempData["Error"] = "You already timed out today.";
                return RedirectToAction(nameof(MyAttendance));
            }

            var now = DateTime.UtcNow;
            attendance.TimeOut = new TimeSpan(now.Hour, now.Minute, now.Second);
            attendance.TotalHours = (attendance.TimeOut - attendance.TimeIn).TotalHours;

              // Calculate late/undertime if shift exists
              var shift = await GetCurrentShiftForEmployee(employee.EmployeeId);
              if (shift != null)
              {
                  var (lateMinutes, undertimeMinutes) = _attendanceService.CalculateLateAndUndertime(
                      attendance.TimeIn, attendance.TimeOut, shift, today);
                  attendance.LateMinutes = lateMinutes;
                  attendance.UndertimeMinutes = undertimeMinutes;
              }

              attendance.DayType = _attendanceService.IsRestDay(today, shift) ? DayType.RestDay : DayType.Regular;

              attendance.NightShiftHours = _attendanceService.CalculateNightShiftHours(
                  attendance.TimeIn, attendance.TimeOut, shift, today);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Time Out recorded successfully.";
            return RedirectToAction(nameof(MyAttendance));
        }

        private async Task<bool> IsOwnAttendance(int employeeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return false;

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            return employee != null && employee.EmployeeId == employeeId;
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
