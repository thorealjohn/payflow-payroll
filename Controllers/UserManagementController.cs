using itpayroll.Areas.Identity.Data;
using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Utilities;
using itpayroll.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itpayroll.Services;

namespace itpayroll.Controllers
{
    [Authorize(Policy = "RequireAdminOrAbove")]
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
        public async Task<IActionResult> Index(string? searchString, string? role, string? status, int? page)
        {
            var query = _userManager.Users.AsQueryable();

            // 🔍 Search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u =>
                    u.FirstName.Contains(searchString) ||
                    u.LastName.Contains(searchString) ||
                    u.Email.Contains(searchString));
            }

            // 🎯 Role filter
            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id);
                query = query.Where(u => userIds.Contains(u.Id));
            }

            // 📌 Status filter
            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status == "active";
                query = query.Where(u => u.IsActive == isActive);
            }

            int pageSize = 10;
            int pageNumber = page ?? 1;

            // 🔥 FIX HERE
            int totalUsers = await query.CountAsync();

            var users = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = string.Join(", ", roles);
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.CurrentUserRole = GetCurrentUserRole();
            ViewBag.CurrentPage = pageNumber;

            // 🔥 FIXED LINE
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

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
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
        public async Task<IActionResult> AuditLogs(
    string? logType = null,
    string? searchString = null,
    string period = "ThisMonth",
    string customDateFrom = null,
    string customDateTo = null,
    int page = 1
)
        {
            (DateTime? from, DateTime? to) = PeriodHelper.GetDateRange(period, customDateFrom, customDateTo);

            var logs = _context.AuditLogs.AsQueryable();

            // Date range filter
            if (from.HasValue)
            {
                logs = logs.Where(l => l.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                logs = logs.Where(l => l.Timestamp <= to.Value.AddDays(1));
            }

            // Filter by log type if specified
            if (!string.IsNullOrEmpty(logType) && Enum.TryParse<LogType>(logType, out var type))
            {
                logs = logs.Where(l => l.LogType == type);
            }

            // Search by user email or entity
            searchString = searchString?.Trim();

            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();

                if (Enum.TryParse<AuditAction>(searchString, true, out var actionEnum))
                {
                    logs = logs.Where(l =>
                        (l.UserEmail != null && l.UserEmail.ToLower().Contains(lowerSearch)) ||
                        (l.Entity != null && l.Entity.ToLower().Contains(lowerSearch)) ||
                        l.Action == actionEnum);
                }
                else
                {
                    logs = logs.Where(l =>
                        (l.UserEmail != null && l.UserEmail.ToLower().Contains(lowerSearch)) ||
                        (l.Entity != null && l.Entity.ToLower().Contains(lowerSearch)));
                }
            }

            int pageSize = 10;

            int totalLogs = await logs.CountAsync();

            var model = await logs
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new AuditLogViewModel
                {
                    Id = l.Id,
                    UserEmail = l.UserEmail,
                    Action = l.Action,
                    Entity = l.Entity,
                    IpAddress = l.IpAddress,
                    Timestamp = l.Timestamp,
                    LogType = l.LogType
                })
                .ToListAsync();

            ViewBag.SelectedLogType = logType;
            ViewBag.CurrentFilter = searchString;
            ViewBag.Period = period;
            ViewBag.CustomDateFrom = customDateFrom;
            ViewBag.CustomDateTo = customDateTo;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalLogs / pageSize);
            return View(model);
        }
    }
}
