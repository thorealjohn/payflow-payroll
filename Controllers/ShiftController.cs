using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using itpayroll.Models;
using itpayroll.Data;

namespace itpayroll.Controllers
{
    public class ShiftController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShiftController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "SuperAdmin,Admin,HR")]
        public async Task<IActionResult> Index()
        {
            var shifts = await _context.Shifts.ToListAsync();
            return View(shifts);
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Create(Shift shift)
        {
            if (ModelState.IsValid)
            {
                shift.CreatedDate = DateTime.UtcNow;
                _context.Shifts.Add(shift);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Shift created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(shift);
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null) return NotFound();
            return View(shift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Edit(int id, Shift shift)
        {
            if (id != shift.ShiftId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(shift);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Shift updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(shift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null) return NotFound();

            shift.IsActive = false;
            _context.Update(shift);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Shift deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
