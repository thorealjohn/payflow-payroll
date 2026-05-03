using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var employees = _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
                .Where(e => !e.User.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e =>
                    e.EmployeeNumber.Contains(searchString) ||
                    e.User.FirstName.Contains(searchString) ||
                    e.User.LastName.Contains(searchString) ||
                    (e.Shift != null && e.Shift.ShiftName.Contains(searchString)));
            }

            ViewBag.CurrentFilter = searchString;
            var result = await employees
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
            return View(result);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateUserDropdown();
            await PopulateShiftDropdown();
            return View();
        }

        private async Task PopulateShiftDropdown(object? selectedShift = null)
        {
            var selectedShiftValue = selectedShift?.ToString() ?? "";
            var shifts = await _context.Shifts
                .Where(s => s.IsActive)
                .OrderBy(s => s.ShiftName)
                .Select(s => new SelectListItem
                {
                    Value = s.ShiftId.ToString(),
                    Text = s.ShiftName,
                    Selected = s.ShiftId.ToString() == selectedShiftValue
                })
                .ToListAsync();

            ViewBag.Shifts = shifts;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUserDropdown();
                await PopulateShiftDropdown(model.ShiftId);
                return View(model);
            }

            var existingEmployee = await _context.Employees
                .AnyAsync(e => e.EmployeeNumber == model.EmployeeNumber);

            if (existingEmployee)
            {
                ModelState.AddModelError(nameof(model.EmployeeNumber), "Employee number already exists.");
                await PopulateUserDropdown();
                return View(model);
            }

            var employee = new Employee
            {
                UserId = model.UserId,
                EmployeeNumber = model.EmployeeNumber,
                Status = model.Status,
                BasicSalary = model.BasicSalary,
                HireDate = model.HireDate,
                TerminationDate = model.TerminationDate,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Employee created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            var model = new EmployeeViewModel
            {
                EmployeeId = employee.EmployeeId,
                UserId = employee.UserId,
                EmployeeNumber = employee.EmployeeNumber,
                BasicSalary = employee.BasicSalary,
                Status = employee.Status,
                HireDate = employee.HireDate,
                TerminationDate = employee.TerminationDate,
                ShiftId = employee.ShiftId
            };

            await PopulateUserDropdown();
            await PopulateShiftDropdown(employee.ShiftId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeViewModel model)
        {
            if (id != model.EmployeeId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateUserDropdown();
                await PopulateShiftDropdown(model.ShiftId);
                return View(model);
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            employee.EmployeeNumber = model.EmployeeNumber;
            employee.Status = model.Status;
            employee.BasicSalary = model.BasicSalary;
            employee.HireDate = model.HireDate;
            employee.TerminationDate = model.TerminationDate;
            employee.ModifiedDate = DateTime.UtcNow;
            employee.ModifiedBy = User.Identity?.Name ?? "System";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
                    return NotFound();
                throw;
            }

            TempData["Success"] = "Employee updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Employee deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }

        private async Task PopulateUserDropdown()
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted && u.IsActive)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName} ({u.Email})"
                })
                .ToListAsync();

            ViewBag.Users = users;
        }
    }
}
