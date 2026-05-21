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

        private async Task PopulateDepartmentDropdown(int? selectedDepartmentId)
        {
            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new { d.DepartmentId, d.Name })
                .ToListAsync();
            ViewBag.Departments = new SelectList(departments, "DepartmentId", "Name", selectedDepartmentId);
        }

        private async Task PopulatePositionDropdown(int? selectedPositionId, int? departmentId)
        {
            var positions = _context.Positions.AsQueryable();
            if (departmentId.HasValue)
            {
                positions = positions.Where(p => p.DepartmentId == departmentId.Value);
            }
            var positionList = await positions
                .OrderBy(p => p.Name)
                .Select(p => new { p.PositionId, p.Name })
                .ToListAsync();
            ViewBag.Positions = new SelectList(positionList, "PositionId", "Name", selectedPositionId);
        }

        public async Task<IActionResult> Index(string? searchString = null, int page = 1)
        {
            int pageSize = 10;

            IQueryable<Employee> employees = _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.Status == EmploymentStatus.Active);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                employees = employees.Where(e =>
                    e.EmployeeNumber.Contains(searchString) ||
                    (e.User != null && (e.User.FirstName.Contains(searchString) || e.User.LastName.Contains(searchString))) ||
                    (e.Shift != null && e.Shift.ShiftName.Contains(searchString)));
            }

            int totalEmployees = await employees.CountAsync();
            var model = await employees
                .OrderBy(e => e.EmployeeNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Populate role badges
            var employeeRoles = new Dictionary<int, string>();
            foreach (var emp in model.Where(e => e.User != null))
            {
                var roles = await _userManager.GetRolesAsync(emp.User!);
                employeeRoles[emp.EmployeeId] = roles.FirstOrDefault() ?? "-";
            }
            ViewBag.UserRoles = employeeRoles;
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalEmployees / pageSize);
            ViewBag.CurrentUserRole = GetCurrentUserRole();

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            var currentRole = GetCurrentUserRole();
            var allowedRoles = RoleHierarchy.GetAllowedRoles(currentRole);
            var model = new EmployeeViewModel
            {
                Role = allowedRoles.Contains(Roles.Employee)
                    ? Roles.Employee
                    : allowedRoles.FirstOrDefault() ?? Roles.Employee
            };

            await PopulateShiftDropdown(null);
            await PopulateDepartmentDropdown(null);
            await PopulatePositionDropdown(null, null);
            ViewBag.AllowedRoles = new SelectList(allowedRoles, model.Role);
            await PopulateRoleDefaults();
            return View(model);
        }

        private async Task PopulateRoleDefaults()
        {
            var roleDefaults = new Dictionary<string, object?>();
            var adminDept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == "Administration");
            var hrDept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == "Human Resources");

            if (adminDept != null)
            {
                roleDefaults["SuperAdmin"] = new
                {
                    departmentId = adminDept.DepartmentId,
                    positionId = (await _context.Positions.FirstOrDefaultAsync(p => p.Name == "System Administrator" && p.DepartmentId == adminDept.DepartmentId))?.PositionId
                };
                roleDefaults["Admin"] = new
                {
                    departmentId = adminDept.DepartmentId,
                    positionId = (await _context.Positions.FirstOrDefaultAsync(p => p.Name == "Admin Officer" && p.DepartmentId == adminDept.DepartmentId))?.PositionId
                };
            }

            if (hrDept != null)
            {
                roleDefaults["HR"] = new
                {
                    departmentId = hrDept.DepartmentId,
                    positionId = (await _context.Positions.FirstOrDefaultAsync(p => p.Name == "HR Officer" && p.DepartmentId == hrDept.DepartmentId))?.PositionId
                };
            }

            ViewBag.RoleDefaults = System.Text.Json.JsonSerializer.Serialize(roleDefaults);
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

            ModelState.Remove(nameof(model.EmployeeNumber));
            ModelState.Remove(nameof(model.Email));

            if (!RoleHierarchy.CanAssignRole(currentRole, model.Role))
            {
                ModelState.AddModelError(string.Empty, "You are not authorized to assign this role.");
            }

            if (string.IsNullOrEmpty(model.EmployeeNumber))
                model.EmployeeNumber = await GenerateEmployeeNumber();

            if (string.IsNullOrEmpty(model.Email))
            {
                var domain = _configuration["CompanySettings:Domain"] ?? "payflow.com";
                model.Email = await GenerateUniqueEmail(model.FirstName, model.LastName, domain);
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateDropdowns(model);
                return View(model);
            }

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                var createResult = await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    Suffix = model.Suffix,
                    DateOfBirth = model.DateOfBirth?.Date,
                    Gender = model.Gender,
                    CivilStatus = model.CivilStatus,
                    Nationality = model.Nationality,
                    PhoneNumber = model.PhoneNumber,
                    AlternatePhone = model.AlternatePhone,
                    AddressStreet = model.AddressStreet,
                    AddressBarangay = model.AddressBarangay,
                    AddressCity = model.AddressCity,
                    AddressProvince = model.AddressProvince,
                    AddressZipCode = model.AddressZipCode,
                    EmergencyContactName = model.EmergencyContactName,
                    EmergencyContactRelationship = model.EmergencyContactRelationship,
                    EmergencyContactPhone = model.EmergencyContactPhone,
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                        MustChangePassword = true,
                        PasswordLastChanged = DateTime.UtcNow
                    };

                    var tempPassword = "Temp@" + Guid.NewGuid().ToString("N").Substring(0, 6);
                    var userResult = await _userManager.CreateAsync(user, tempPassword);
                    if (!userResult.Succeeded)
                    {
                        return (Succeeded: false, Result: userResult, CreatedEmail: (string?)null, TempPassword: (string?)null);
                    }

                    var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                    if (!roleResult.Succeeded)
                    {
                        return (Succeeded: false, Result: roleResult, CreatedEmail: (string?)null, TempPassword: (string?)null);
                    }

                    var employee = new Employee
                    {
                    UserId = user.Id,
                    EmployeeNumber = model.EmployeeNumber,
                    Status = model.Status,
                    BasicSalary = model.BasicSalary,
                    HireDate = model.HireDate == default ? DateTime.UtcNow : model.HireDate,
                    TerminationDate = model.TerminationDate,
                    ShiftId = model.ShiftId,
                    DepartmentId = model.DepartmentId,
                    PositionId = model.PositionId,
                    EmploymentType = model.EmploymentType,
                    SalaryType = model.SalaryType,
                    BankName = model.BankName,
                    BankAccountNumber = model.BankAccountNumber,
                    TIN = model.TIN,
                    SSSNumber = model.SSSNumber,
                    PhilHealthNumber = model.PhilHealthNumber,
                    PagIBIGNumber = model.PagIBIGNumber,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                    _context.Employees.Add(employee);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (Succeeded: true, Result: IdentityResult.Success, CreatedEmail: user.Email, TempPassword: tempPassword);
                });

                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Result.Errors)
                        ModelState.AddModelError("", error.Description);
                    await PopulateCreateDropdowns(model);
                    return View(model);
                }

                ViewBag.CreatedEmail = createResult.CreatedEmail;
                ViewBag.TempPassword = createResult.TempPassword;
                return View("CreationSuccess", model);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Employee could not be created because the employee number or linked account already exists.");
                await PopulateCreateDropdowns(model);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (employee == null)
                return NotFound();

            var roles = employee.User != null
                ? await _userManager.GetRolesAsync(employee.User)
                : new List<string>();
            var currentRole = roles.FirstOrDefault() ?? Roles.Employee;

            var currentUserRole = GetCurrentUserRole();

            if (!await CanManageEmployee(employee.User))
            {
                TempData["Error"] = "You are not authorized to edit this account.";
                return RedirectToAction(nameof(Index));
            }

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
                MiddleName = employee.User?.MiddleName,
                LastName = employee.User?.LastName ?? string.Empty,
                Suffix = employee.User?.Suffix,
                DateOfBirth = employee.User?.DateOfBirth,
                Gender = employee.User?.Gender,
                CivilStatus = employee.User?.CivilStatus,
                Nationality = employee.User?.Nationality ?? "Filipino",
                PhoneNumber = employee.User?.PhoneNumber,
                AlternatePhone = employee.User?.AlternatePhone,
                AddressStreet = employee.User?.AddressStreet,
                AddressBarangay = employee.User?.AddressBarangay,
                AddressCity = employee.User?.AddressCity,
                AddressProvince = employee.User?.AddressProvince,
                AddressZipCode = employee.User?.AddressZipCode,
                EmergencyContactName = employee.User?.EmergencyContactName,
                EmergencyContactRelationship = employee.User?.EmergencyContactRelationship,
                EmergencyContactPhone = employee.User?.EmergencyContactPhone,
                EmployeeNumber = employee.EmployeeNumber,
                Status = employee.Status,
                BasicSalary = employee.BasicSalary,
                HireDate = employee.HireDate,
                TerminationDate = employee.TerminationDate,
                ShiftId = employee.ShiftId,
                DepartmentId = employee.DepartmentId,
                PositionId = employee.PositionId,
                EmploymentType = employee.EmploymentType,
                SalaryType = employee.SalaryType,
                BankName = employee.BankName,
                BankAccountNumber = employee.BankAccountNumber,
                TIN = employee.TIN,
                SSSNumber = employee.SSSNumber,
                PhilHealthNumber = employee.PhilHealthNumber,
                PagIBIGNumber = employee.PagIBIGNumber
            };

            await PopulateShiftDropdown(employee.ShiftId);
            await PopulateDepartmentDropdown(employee.DepartmentId);
            await PopulatePositionDropdown(employee.PositionId, employee.DepartmentId);
            await PopulateRoleDefaults();
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

            if (!ModelState.IsValid)
            {
                await PopulateShiftDropdown(model.ShiftId);
                await PopulateDepartmentDropdown(model.DepartmentId);
                await PopulatePositionDropdown(model.PositionId, model.DepartmentId);
                await PopulateRoleDefaults();
                return View(model);
            }

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == model.EmployeeId);
            if (employee == null)
                return NotFound();

            if (!await CanManageEmployee(employee.User))
            {
                TempData["Error"] = "You are not authorized to edit this account.";
                return RedirectToAction(nameof(Index));
            }

            var before = BuildEmployeeAuditSnapshot(employee);

            if (employee.User != null)
            {
                employee.User.FirstName = model.FirstName;
                employee.User.MiddleName = model.MiddleName;
                employee.User.LastName = model.LastName;
                employee.User.Suffix = model.Suffix;
                employee.User.DateOfBirth = model.DateOfBirth?.Date;
                employee.User.Gender = model.Gender;
                employee.User.CivilStatus = model.CivilStatus;
                employee.User.Nationality = model.Nationality;
                employee.User.PhoneNumber = model.PhoneNumber;
                employee.User.AlternatePhone = model.AlternatePhone;
                employee.User.AddressStreet = model.AddressStreet;
                employee.User.AddressBarangay = model.AddressBarangay;
                employee.User.AddressCity = model.AddressCity;
                employee.User.AddressProvince = model.AddressProvince;
                employee.User.AddressZipCode = model.AddressZipCode;
                employee.User.EmergencyContactName = model.EmergencyContactName;
                employee.User.EmergencyContactRelationship = model.EmergencyContactRelationship;
                employee.User.EmergencyContactPhone = model.EmergencyContactPhone;
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
            employee.BasicSalary = model.BasicSalary;
            employee.HireDate = model.HireDate == default ? DateTime.UtcNow : model.HireDate;
            employee.TerminationDate = model.TerminationDate;
            employee.ShiftId = model.ShiftId;
            employee.DepartmentId = model.DepartmentId;
            employee.PositionId = model.PositionId;
            employee.EmploymentType = model.EmploymentType;
            employee.SalaryType = model.SalaryType;
            employee.BankName = model.BankName;
            employee.BankAccountNumber = model.BankAccountNumber;
            employee.TIN = model.TIN;
            employee.SSSNumber = model.SSSNumber;
            employee.PhilHealthNumber = model.PhilHealthNumber;
            employee.PagIBIGNumber = model.PagIBIGNumber;
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
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId.Value);
            if (employee?.User == null) return null;
            var roles = await _userManager.GetRolesAsync(employee.User);
            return roles.FirstOrDefault();
        }

        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Position)
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

            if (!await CanManageEmployee(employee.User))
            {
                TempData["Error"] = "You are not authorized to deactivate this account.";
                return RedirectToAction(nameof(Index));
            }

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

        [HttpGet]
        public async Task<JsonResult> GetPositionsByDepartment(int departmentId)
        {
            var positions = await _context.Positions
                .Where(p => p.DepartmentId == departmentId)
                .OrderBy(p => p.Name)
                .Select(p => new { p.PositionId, p.Name })
                .ToListAsync();
            return Json(positions);
        }

        #region Helpers
        private async Task<bool> CanManageEmployee(ApplicationUser? targetUser)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || targetUser == null)
                return false;

            if (User.IsInRole(Roles.SuperAdmin))
                return true;

            if (targetUser.Id == currentUser.Id)
                return true;

            var currentRoles = await _userManager.GetRolesAsync(currentUser);
            var targetRoles = await _userManager.GetRolesAsync(targetUser);
            var currentRole = currentRoles.FirstOrDefault() ?? "";
            var targetRole = targetRoles.FirstOrDefault() ?? "";

            return RoleHierarchy.CanApprove(currentRole, targetRole);
        }

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
                ["DepartmentId"] = employee.DepartmentId,
                ["PositionId"] = employee.PositionId,
                ["Department"] = employee.Department?.Name,
                ["Position"] = employee.Position?.Name,
                ["EmploymentType"] = employee.EmploymentType?.ToString(),
                ["SalaryType"] = employee.SalaryType.ToString()
            };
        }

        private static string BuildChangeMetadata(Dictionary<string, object?> before, Dictionary<string, object?> after)
        {
            var changes = before
                .Where(item => !Equals(item.Value, after[item.Key]))
                .ToDictionary(item => item.Key, item => new { Before = item.Value, After = after[item.Key] });

            return JsonSerializer.Serialize(new { Before = before, After = after, Changes = changes });
        }

        private async Task PopulateCreateDropdowns(EmployeeViewModel model)
        {
            await PopulateShiftDropdown(model.ShiftId);
            await PopulateDepartmentDropdown(model.DepartmentId);
            await PopulatePositionDropdown(model.PositionId, model.DepartmentId);
            await PopulateRoleDefaults();
        }

        private async Task<string> GenerateEmployeeNumber()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"EMP-{year}-";
            var employeeNumbers = await _context.Employees
                .Where(e => e.EmployeeNumber.StartsWith(prefix))
                .Select(e => e.EmployeeNumber)
                .ToListAsync();

            var nextNumber = 1;
            foreach (var employeeNumber in employeeNumbers)
            {
                var numberPart = employeeNumber[prefix.Length..];
                if (int.TryParse(numberPart, out var number) && number >= nextNumber)
                    nextNumber = number + 1;
            }

            string candidate;
            do
            {
                candidate = $"{prefix}{nextNumber:D3}";
                nextNumber++;
            }
            while (await _context.Employees.AnyAsync(e => e.EmployeeNumber == candidate));

            return candidate;
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
