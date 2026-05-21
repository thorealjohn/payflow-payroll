using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize(Roles = $"{Roles.SuperAdmin}")]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .OrderByDescending(d => d.IsActive)
                .ThenBy(d => d.Name)
                .ToListAsync();

            ViewBag.EmployeeCounts = await _context.Employees
                .Where(e => e.DepartmentId != null)
                .GroupBy(e => e.DepartmentId!.Value)
                .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DepartmentId, x => x.Count);

            ViewBag.PositionCounts = await _context.Positions
                .GroupBy(p => p.DepartmentId)
                .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DepartmentId, x => x.Count);

            return View(departments);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Department());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (await _context.Departments.AnyAsync(d => d.Name == department.Name))
            {
                ModelState.AddModelError(nameof(department.Name), "A department with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(department);
            }

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Department created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.DepartmentId)
                return NotFound();

            if (await _context.Departments.AnyAsync(d => d.DepartmentId != id && d.Name == department.Name))
            {
                ModelState.AddModelError(nameof(department.Name), "A department with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(department);
            }

            _context.Update(department);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Department updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
                return NotFound();

            var positionCount = await _context.Positions.CountAsync(p => p.DepartmentId == id);
            var employeeCount = await _context.Employees.CountAsync(e => e.DepartmentId == id);

            ViewBag.PositionCount = positionCount;
            ViewBag.EmployeeCount = employeeCount;

            return View(department);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            department.IsActive = !department.IsActive;
            _context.Departments.Update(department);
            await _context.SaveChangesAsync();

            TempData["Success"] = department.IsActive ? "Department restored successfully." : "Department deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
