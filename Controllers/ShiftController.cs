using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
    public class ShiftController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShiftController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var shifts = await _context.Shifts
                .OrderBy(s => s.ShiftName)
                .ToListAsync();
            return View(shifts);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Shift shift)
        {
            if (!ModelState.IsValid)
            {
                return View(shift);
            }

            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Shift created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
                return NotFound();

            return View(shift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Shift shift)
        {
            if (id != shift.ShiftId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                return View(shift);
            }

            _context.Update(shift);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Shift updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
                return NotFound();

            return View(shift);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
                return NotFound();

            // Check if any employees are using this shift
            var employeesUsingShift = await _context.Employees
                .AnyAsync(e => e.ShiftId == id);

            if (employeesUsingShift)
            {
                TempData["Error"] = "Cannot delete shift. Employees are assigned to this shift.";
                return RedirectToAction(nameof(Index));
            }

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Shift deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
