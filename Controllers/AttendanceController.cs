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
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AttendanceService _attendanceService;

        public AttendanceController(ApplicationDbContext context, AttendanceService attendanceService)
        {
            _context = context;
            _attendanceService = attendanceService;
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Index(DateTime? date)
        {
            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            if (date.HasValue)
            {
                query = query.Where(a => a.Date == date.Value.Date);
            }

            var attendances = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.TimeIn)
                .ToListAsync();

            ViewBag.SelectedDate = date?.Date;
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

            // Calculate night shift hours (10pm-6am)
            attendance.NightShiftHours = _attendanceService.CalculateNightShiftHours(
                model.TimeIn, model.TimeOut, model.Date.Date);

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
                model.TimeIn, model.TimeOut, attendance.Date);

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

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Attendance record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.Employee}")]
        public async Task<IActionResult> MyAttendance()
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == User.Identity.Name);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return View(new List<Attendance>());
            }

            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employee.EmployeeId)
                .OrderByDescending(a => a.Date)
                .Take(50)
                .ToListAsync();

            return View(attendances);
        }

        private bool AttendanceExists(int id)
        {
            return _context.Attendances.Any(a => a.AttendanceId == id);
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
