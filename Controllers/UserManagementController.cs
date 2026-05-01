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

namespace itpayroll.Controllers
{
    [Authorize]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            AuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
        }

        private string GetCurrentUserRole()
        {
            if (User.IsInRole(Roles.SuperAdmin)) return Roles.SuperAdmin;
            if (User.IsInRole(Roles.Admin)) return Roles.Admin;
            if (User.IsInRole(Roles.HR)) return Roles.HR;
            return Roles.Employee;
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = string.Join(", ", roles);
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.CurrentUserRole = GetCurrentUserRole();
            return View(users);
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Create()
        {
            var currentRole = GetCurrentUserRole();
            var allowedRoles = RoleHierarchy.GetAllowedRoles(currentRole);
            var requiresEmployeeCreation = RoleHierarchy.IsEmployeeCreation(currentRole);

            ViewBag.AllowedRoles = allowedRoles.Select(r => new SelectListItem
            {
                Value = r,
                Text = r
            }).ToList();

            ViewBag.RequiresEmployeeCreation = requiresEmployeeCreation;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            var currentRole = GetCurrentUserRole();
            var allowedRoles = RoleHierarchy.GetAllowedRoles(currentRole);
            var requiresEmployeeCreation = RoleHierarchy.IsEmployeeCreation(currentRole);

            ViewBag.AllowedRoles = allowedRoles.Select(r => new SelectListItem
            {
                Value = r,
                Text = r
            }).ToList();

            ViewBag.RequiresEmployeeCreation = requiresEmployeeCreation;

            if (requiresEmployeeCreation)
            {
                if (string.IsNullOrWhiteSpace(model.EmployeeNumber))
                {
                    ModelState.AddModelError(nameof(model.EmployeeNumber), "Employee number is required.");
                }

                if (!model.BasicSalary.HasValue || model.BasicSalary.Value <= 0)
                {
                    ModelState.AddModelError(nameof(model.BasicSalary), "Basic salary is required and must be greater than zero.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            if (!RoleHierarchy.CanAssignRole(currentRole, model.Role))
            {
                ModelState.AddModelError(string.Empty, "You are not authorized to assign this role.");
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already in use.");
                return View(model);
            }

            var existingEmployeeNumber = !string.IsNullOrWhiteSpace(model.EmployeeNumber)
                && await _context.Employees.AnyAsync(e => e.EmployeeNumber == model.EmployeeNumber);

            if (existingEmployeeNumber)
            {
                ModelState.AddModelError(nameof(model.EmployeeNumber), "Employee number already exists.");
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
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            if (requiresEmployeeCreation)
            {
                var employee = new Employee
                {
                    UserId = user.Id,
                    EmployeeNumber = model.EmployeeNumber!,
                    BasicSalary = model.BasicSalary!.Value,
                    HireDate = DateTime.UtcNow,
                    Status = EmploymentStatus.Active,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
            }

            await _auditService.LogAsync(AuditAction.Create, $"User: {user.Email}");
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentUserRole = GetCurrentUserRole();

            var allowedRoles = RoleHierarchy.GetAllowedRoles(currentUserRole);
            var currentRole = currentRoles.FirstOrDefault();

            if (!string.IsNullOrEmpty(currentRole) && !allowedRoles.Contains(currentRole))
            {
                allowedRoles = allowedRoles.Concat(new[] { currentRole }).ToArray();
            }

            var model = new EditUserViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CurrentRole = currentRole ?? Roles.Employee
            };

            ViewBag.AvailableRoles = allowedRoles.Select(r => new SelectListItem
            {
                Value = r,
                Text = r,
                Selected = r == currentRole
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            var currentUserRole = GetCurrentUserRole();

            if (!RoleHierarchy.CanAssignRole(currentUserRole, model.CurrentRole))
            {
                ModelState.AddModelError(string.Empty, "You are not authorized to assign this role.");
                return View(model);
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.ModifiedDate = DateTime.UtcNow;
            user.ModifiedBy = User.Identity?.Name ?? "System";

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.CurrentRole);

            await _auditService.LogAsync(AuditAction.Update, $"User: {user.Email}");
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == id);

            ViewBag.Employee = employee;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.HR}")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var targetUserRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            var currentUserRole = GetCurrentUserRole();
            if (targetUserRole == Roles.SuperAdmin && currentUserRole != Roles.SuperAdmin)
            {
                TempData["Error"] = "You cannot modify a SuperAdmin account.";
                return RedirectToAction(nameof(Index));
            }

            if (targetUserRole == Roles.Admin && currentUserRole == Roles.HR)
            {
                TempData["Error"] = "HR cannot modify Admin accounts.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            user.ModifiedDate = DateTime.UtcNow;
            user.ModifiedBy = User.Identity?.Name ?? "System";

            await _userManager.UpdateAsync(user);
            await _auditService.LogAsync(AuditAction.Update, "UserStatus");

            var action = user.IsActive ? "activated" : "deactivated";
            TempData["Success"] = $"User {action} successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> SoftDelete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var targetUserRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            var currentUserRole = GetCurrentUserRole();

            if (targetUserRole == Roles.SuperAdmin)
            {
                TempData["Error"] = "SuperAdmin accounts cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            if (targetUserRole == Roles.Admin && currentUserRole == Roles.Admin)
            {
                TempData["Error"] = "Admins cannot delete other Admin accounts.";
                return RedirectToAction(nameof(Index));
            }

            user.IsDeleted = true;
            user.IsActive = false;
            user.ModifiedDate = DateTime.UtcNow;
            user.ModifiedBy = User.Identity?.Name ?? "System";

            await _userManager.UpdateAsync(user);

            
            await _auditService.LogAsync(AuditAction.Delete, $"User: {user.Email}");
            TempData["Success"] = "User deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
