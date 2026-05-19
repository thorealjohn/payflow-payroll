using itpayroll.Areas.Identity.Data;
using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itpayroll.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace itpayroll.Controllers
{
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly AuditService _auditService;

        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration, AuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _auditService = auditService;
        }

        private async Task PopulateShiftDropdown(int? selectedShiftId)
        {
            var shifts = await _context.Shifts
                .Where(s => s.IsActive)
                .Select(s => new { s.ShiftId, s.ShiftName })
                .ToListAsync();
            ViewBag.Shifts = new SelectList(shifts, "ShiftId", "ShiftName", selectedShiftId);
        }

        public async Task<IActionResult> Index(string? searchString = null)
        {
            IQueryable<Employee> employees = _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
                .Where(e => e.Status == EmploymentStatus.Active);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                employees = employees.Where(e =>
                    e.EmployeeNumber.Contains(searchString) ||
                    (e.User != null && (e.User.FirstName.Contains(searchString) || e.User.LastName.Contains(searchString))) ||
                    (e.Shift != null && e.Shift.ShiftName.Contains(searchString)));
            }

            var model = await employees.ToListAsync();

            // Populate role badges
            var employeeRoles = new Dictionary<int, string>();
            foreach (var emp in model.Where(e => e.User != null))
            {
                var roles = await _userManager.GetRolesAsync(emp.User!);
                employeeRoles[emp.EmployeeId] = roles.FirstOrDefault() ?? "—";
            }
            ViewBag.UserRoles = employeeRoles;
            ViewBag.CurrentFilter = searchString;

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateShiftDropdown(null);
            var currentRole = GetCurrentUserRole();
            var allowedRoles = RoleHierarchy.GetAllowedRoles(currentRole);
            ViewBag.AllowedRoles = new SelectList(allowedRoles);
            return View();
        }

        private string GetCurrentUserRole()
        {
            if (User.IsInRole(Roles.SuperAdmin)) return Roles.SuperAdmin;
            if (User.IsInRole(Roles.Admin)) return Roles.Admin;
            if (User.IsInRole(Roles.HR)) return Roles.HR;
            return Roles.Employee;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            var currentRole = GetCurrentUserRole();
            var allowedRoles = RoleHierarchy.GetAllowedRoles(currentRole);
            ViewBag.AllowedRoles = new SelectList(allowedRoles);

            // Remove auto-generated fields from ModelState
            ModelState.Remove(nameof(model.EmployeeNumber));
            ModelState.Remove(nameof(model.Email));

            if (!RoleHierarchy.CanAssignRole(currentRole, model.Role))
            {
                ModelState.AddModelError(string.Empty, "You are not authorized to assign this role.");
            }

            // Role-specific validation
            if (model.Role == Roles.Employee && model.BasicSalary <= 0)
            {
                ModelState.AddModelError(nameof(model.BasicSalary), "Basic salary is required and must be greater than zero.");
            }

            // Generate auto-values
            if (string.IsNullOrEmpty(model.EmployeeNumber))
                model.EmployeeNumber = await GenerateEmployeeNumber();

            if (string.IsNullOrEmpty(model.Email))
            {
                var domain = _configuration["CompanySettings:Domain"] ?? "payflow.com";
                model.Email = await GenerateUniqueEmail(model.FirstName, model.LastName, domain);
            }

            if (!ModelState.IsValid)
            {
                await PopulateShiftDropdown(model.ShiftId);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedBy = User.Identity?.Name ?? "System",
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                MustChangePassword = true,
                PasswordLastChanged = DateTime.UtcNow
            };

            var tempPassword = "Temp@" + Guid.NewGuid().ToString("N").Substring(0, 6);

            var result = await _userManager.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                await PopulateShiftDropdown(model.ShiftId);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            var employee = new Employee
            {
                UserId = user.Id,
                EmployeeNumber = model.EmployeeNumber,
                Status = model.Status,
                BasicSalary = model.Role == Roles.Employee ? model.BasicSalary : 0,
                HireDate = model.HireDate == default ? DateTime.UtcNow : model.HireDate,
                TerminationDate = model.TerminationDate,
                ShiftId = model.Role == Roles.Employee ? model.ShiftId : null,
                Department = model.Department,
                Position = model.Position,
                EmploymentType = model.Role == Roles.Employee ? model.EmploymentType : null,
                SalaryType = model.Role == Roles.Employee ? model.SalaryType : SalaryType.Monthly,
                PayFrequency = model.Role == Roles.Employee ? model.PayFrequency : PayFrequency.Monthly,
                BankName = model.Role == Roles.Employee ? model.BankName : null,
                BankAccountNumber = model.Role == Roles.Employee ? model.BankAccountNumber : null,
                TIN = model.Role == Roles.Employee ? model.TIN : null,
                SSSNumber = model.Role == Roles.Employee ? model.SSSNumber : null,
                PhilHealthNumber = model.Role == Roles.Employee ? model.PhilHealthNumber : null,
                PagIBIGNumber = model.Role == Roles.Employee ? model.PagIBIGNumber : null,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            ViewBag.CreatedEmail = user.Email;
            ViewBag.TempPassword = tempPassword;
            return View("CreationSuccess", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (employee == null)
                return NotFound();

            var roles = employee.User != null
                ? await _userManager.GetRolesAsync(employee.User)
                : new List<string>();
            var currentRole = roles.FirstOrDefault() ?? Roles.Employee;

            var currentUserRole = GetCurrentUserRole();
            var allowedRoles = RoleHierarchy.GetAllowedRoles(currentUserRole);
            if (!allowedRoles.Contains(currentRole))
            {
                allowedRoles = allowedRoles.Concat(new[] { currentRole }).ToArray();
            }
            ViewBag.AllowedRoles = new SelectList(allowedRoles, currentRole);
            ViewBag.CurrentRole = currentRole;

            var model = new EmployeeViewModel
            {
                EmployeeId = employee.EmployeeId,
                Role = currentRole,
                FirstName = employee.User?.FirstName ?? string.Empty,
                LastName = employee.User?.LastName ?? string.Empty,
                EmployeeNumber = employee.EmployeeNumber,
                Status = employee.Status,
                BasicSalary = employee.BasicSalary,
                HireDate = employee.HireDate,
                TerminationDate = employee.TerminationDate,
                ShiftId = employee.ShiftId,
                Department = employee.Department,
                Position = employee.Position,
                EmploymentType = employee.EmploymentType,
                SalaryType = employee.SalaryType,
                PayFrequency = employee.PayFrequency,
                BankName = employee.BankName,
                BankAccountNumber = employee.BankAccountNumber,
                TIN = employee.TIN,
                SSSNumber = employee.SSSNumber,
                PhilHealthNumber = employee.PhilHealthNumber,
                PagIBIGNumber = employee.PagIBIGNumber
            };

            await PopulateShiftDropdown(employee.ShiftId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeViewModel model)
        {
            var currentUserRole = GetCurrentUserRole();
            var allowedRoles = RoleHierarchy.GetAllowedRoles(currentUserRole);
            var currentRole = await GetUserRoleAsync(model.EmployeeId);

            if (!string.IsNullOrEmpty(currentRole))
            {
                ViewBag.AllowedRoles = new SelectList(
                    allowedRoles.Contains(currentRole)
                        ? allowedRoles
                        : allowedRoles.Concat(new[] { currentRole }),
                    model.Role);
            }
            ViewBag.CurrentRole = currentRole;

            if (!RoleHierarchy.CanAssignRole(currentUserRole, model.Role))
            {
                ModelState.AddModelError(string.Empty, "You are not authorized to assign this role.");
            }

            if (model.Role == Roles.Employee && model.BasicSalary <= 0)
            {
                ModelState.AddModelError(nameof(model.BasicSalary), "Basic salary is required and must be greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateShiftDropdown(model.ShiftId);
                return View(model);
            }

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == model.EmployeeId);
            if (employee == null)
                return NotFound();

            var before = BuildEmployeeAuditSnapshot(employee);

            if (employee.User != null)
            {
                employee.User.FirstName = model.FirstName;
                employee.User.LastName = model.LastName;
                employee.User.ModifiedBy = User.Identity?.Name ?? "System";
                employee.User.ModifiedDate = DateTime.UtcNow;
            }

            // Update role if changed
            if (employee.User != null && currentRole != model.Role)
            {
                var currentRoles = await _userManager.GetRolesAsync(employee.User);
                await _userManager.RemoveFromRolesAsync(employee.User, currentRoles);
                await _userManager.AddToRoleAsync(employee.User, model.Role);
            }

            employee.EmployeeNumber = model.EmployeeNumber;
            employee.Status = model.Status;
            employee.BasicSalary = model.Role == Roles.Employee ? model.BasicSalary : 0;
            employee.HireDate = model.HireDate == default ? DateTime.UtcNow : model.HireDate;
            employee.TerminationDate = model.TerminationDate;
            employee.ShiftId = model.Role == Roles.Employee ? model.ShiftId : null;
            employee.Department = model.Department;
            employee.Position = model.Position;
            employee.EmploymentType = model.Role == Roles.Employee ? model.EmploymentType : null;
            employee.SalaryType = model.Role == Roles.Employee ? model.SalaryType : SalaryType.Monthly;
            employee.PayFrequency = model.Role == Roles.Employee ? model.PayFrequency : PayFrequency.Monthly;
            employee.BankName = model.Role == Roles.Employee ? model.BankName : null;
            employee.BankAccountNumber = model.Role == Roles.Employee ? model.BankAccountNumber : null;
            employee.TIN = model.Role == Roles.Employee ? model.TIN : null;
            employee.SSSNumber = model.Role == Roles.Employee ? model.SSSNumber : null;
            employee.PhilHealthNumber = model.Role == Roles.Employee ? model.PhilHealthNumber : null;
            employee.PagIBIGNumber = model.Role == Roles.Employee ? model.PagIBIGNumber : null;
            employee.ModifiedBy = User.Identity?.Name ?? "System";
            employee.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(
                AuditAction.Update,
                $"Employee: {employee.EmployeeNumber}",
                LogType.System,
                "Employee",
                employee.EmployeeId.ToString(),
                BuildChangeMetadata(before, BuildEmployeeAuditSnapshot(employee)));

            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> GetUserRoleAsync(int? employeeId)
        {
            if (employeeId == null) return null;
            var employee = await _context.Employees.FindAsync(employeeId.Value);
            if (employee?.User == null) return null;
            var roles = await _userManager.GetRolesAsync(employee.User);
            return roles.FirstOrDefault();
        }

        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
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
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            var before = BuildEmployeeAuditSnapshot(employee);
            employee.Status = EmploymentStatus.Inactive;
            employee.TerminationDate ??= DateTime.UtcNow.Date;
            employee.ModifiedBy = User.Identity?.Name ?? "System";
            employee.ModifiedDate = DateTime.UtcNow;

            if (employee.User != null)
            {
                employee.User.IsActive = false;
                employee.User.ModifiedBy = User.Identity?.Name ?? "System";
                employee.User.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(
                AuditAction.Delete,
                $"Employee: {employee.EmployeeNumber}",
                LogType.System,
                "Employee",
                employee.EmployeeId.ToString(),
                BuildChangeMetadata(before, BuildEmployeeAuditSnapshot(employee)));

            TempData["Success"] = "Employee deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        #region Helpers
        private static Dictionary<string, object?> BuildEmployeeAuditSnapshot(Employee employee)
        {
            return new Dictionary<string, object?>
            {
                ["FirstName"] = employee.User?.FirstName,
                ["LastName"] = employee.User?.LastName,
                ["EmployeeNumber"] = employee.EmployeeNumber,
                ["Status"] = employee.Status.ToString(),
                ["BasicSalary"] = employee.BasicSalary,
                ["HireDate"] = employee.HireDate.ToString("yyyy-MM-dd"),
                ["TerminationDate"] = employee.TerminationDate?.ToString("yyyy-MM-dd"),
                ["ShiftId"] = employee.ShiftId,
                ["Department"] = employee.Department,
                ["Position"] = employee.Position,
                ["EmploymentType"] = employee.EmploymentType?.ToString(),
                ["SalaryType"] = employee.SalaryType.ToString(),
                ["PayFrequency"] = employee.PayFrequency.ToString()
            };
        }

        private static string BuildChangeMetadata(Dictionary<string, object?> before, Dictionary<string, object?> after)
        {
            var changes = before
                .Where(item => !Equals(item.Value, after[item.Key]))
                .ToDictionary(item => item.Key, item => new { Before = item.Value, After = after[item.Key] });

            return JsonSerializer.Serialize(new { Before = before, After = after, Changes = changes });
        }

        private async Task<string> GenerateEmployeeNumber()
        {
            var lastEmployee = await _context.Employees
                .OrderByDescending(e => e.EmployeeId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastEmployee != null && lastEmployee.EmployeeNumber.StartsWith("EMP-"))
            {
                var numberPart = lastEmployee.EmployeeNumber.Substring(4);
                if (int.TryParse(numberPart, out int lastNum))
                    nextNumber = lastNum + 1;
            }
            return $"EMP-{nextNumber:D4}";
        }

        /// <summary>
        /// Builds a safe email local-part segment from a name. Spaces and other characters
        /// invalid in email addresses are removed so compound first names (e.g. "John Andrew") work.
        /// </summary>
        private static string ToEmailLocalSegment(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "user";

            var trimmed = name.Trim().ToLowerInvariant();
            // No spaces (or other whitespace) in the local part of an email
            var collapsed = Regex.Replace(trimmed, @"\s+", "");
            // Allow only typical safe local-part characters
            var cleaned = Regex.Replace(collapsed, @"[^a-z0-9._-]", "");
            return string.IsNullOrEmpty(cleaned) ? "user" : cleaned;
        }

        private async Task<string> GenerateUniqueEmail(string firstName, string lastName, string domain)
        {
            var first = ToEmailLocalSegment(firstName);
            var last = ToEmailLocalSegment(lastName);
            var email = $"{first}.{last}@{domain}";
            var counter = 0;

            while (await _userManager.FindByEmailAsync(email) != null)
            {
                counter++;
                email = $"{first}.{last}{counter}@{domain}";
            }

            return email;
        }
        #endregion
    }
}
