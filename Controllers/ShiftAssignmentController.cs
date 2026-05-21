using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using itpayroll.Models;
using itpayroll.Data;

namespace itpayroll.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public class ShiftAssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShiftAssignmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var assignments = await _context.EmployeeShiftAssignments
                .Include(a => a.Employee)
                .Include(a => a.Shift)
                .OrderByDescending(a => a.DateFrom)
                .ToListAsync();

            return View(assignments);
        }

        [HttpGet]
        public async Task<IActionResult> Assign(int? employeeId)
        {
            ViewBag.Employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.Status == EmploymentStatus.Active && e.User != null)
                .Select(e => new
                {
                    EmployeeId = e.EmployeeId,
                    DisplayText = $"{e.EmployeeNumber} - {e.User!.FirstName} {e.User.LastName}"
                })
                .ToListAsync();

            ViewBag.Shifts = await _context.Shifts
                .Where(s => s.IsActive)
                .ToListAsync();

            var model = new EmployeeShiftAssignment();
            if (employeeId.HasValue)
            {
                model.EmployeeId = employeeId.Value;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(EmployeeShiftAssignment model)
        {
            // Check for overlapping assignments
            var newStart = model.DateFrom;
            var newEnd = model.DateTo ?? DateTime.MaxValue;

            var overlapping = await _context.EmployeeShiftAssignments
                .Where(a => a.EmployeeId == model.EmployeeId)
                .AnyAsync(a =>
                    newStart <= (a.DateTo ?? DateTime.MaxValue) &&
                    a.DateFrom <= newEnd);

            if (overlapping)
            {
                ModelState.AddModelError("", "Employee already has an active shift assignment overlapping this period.");
            }

            if (ModelState.IsValid)
            {
                _context.EmployeeShiftAssignments.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Shift assigned successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.Status == EmploymentStatus.Active && e.User != null)
                .Select(e => new
                {
                    EmployeeId = e.EmployeeId,
                    DisplayText = $"{e.EmployeeNumber} - {e.User!.FirstName} {e.User.LastName}"
                })
                .ToListAsync();

            ViewBag.Shifts = await _context.Shifts
                .Where(s => s.IsActive)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndAssignment(int id)
        {
            var assignment = await _context.EmployeeShiftAssignments.FindAsync(id);
            if (assignment == null) return NotFound();

            assignment.DateTo = DateTime.Today;
            _context.Update(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Shift assignment ended successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
