using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize(Roles = $"{Roles.SuperAdmin}")]
    public class PositionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PositionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var positions = await _context.Positions
                .Include(p => p.Department)
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Department.Name)
                .ThenBy(p => p.Name)
                .ToListAsync();

            ViewBag.EmployeeCounts = await _context.Employees
                .Where(e => e.PositionId != null)
                .GroupBy(e => e.PositionId!.Value)
                .Select(g => new { PositionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PositionId, x => x.Count);

            return View(positions);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDepartments();
            return View(new Position());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Position position)
        {
            if (await _context.Positions.AnyAsync(p => p.DepartmentId == position.DepartmentId && p.Name == position.Name))
            {
                ModelState.AddModelError(nameof(position.Name), "A position with this name already exists in the selected department.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDepartments(position.DepartmentId);
                return View(position);
            }

            _context.Positions.Add(position);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Position created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null)
                return NotFound();

            await PopulateDepartments(position.DepartmentId);
            return View(position);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Position position)
        {
            if (id != position.PositionId)
                return NotFound();

            if (await _context.Positions.AnyAsync(p => p.PositionId != id && p.DepartmentId == position.DepartmentId && p.Name == position.Name))
            {
                ModelState.AddModelError(nameof(position.Name), "A position with this name already exists in the selected department.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDepartments(position.DepartmentId);
                return View(position);
            }

            _context.Update(position);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Position updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var position = await _context.Positions
                .Include(p => p.Department)
                .FirstOrDefaultAsync(p => p.PositionId == id);

            if (position == null)
                return NotFound();

            ViewBag.EmployeeCount = await _context.Employees.CountAsync(e => e.PositionId == id);
            return View(position);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null)
                return NotFound();

            position.IsActive = !position.IsActive;
            _context.Positions.Update(position);
            await _context.SaveChangesAsync();

            TempData["Success"] = position.IsActive ? "Position restored successfully." : "Position deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDepartments(int? selectedDepartmentId = null)
        {
            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new { d.DepartmentId, d.Name })
                .ToListAsync();

            ViewBag.Departments = new SelectList(departments, "DepartmentId", "Name", selectedDepartmentId);
        }
    }
}
