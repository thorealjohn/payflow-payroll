using itpayroll.Areas.Identity.Data;
using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Services;
using itpayroll.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace itpayroll.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly NotificationService _notificationService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            AuditService auditService,
            NotificationService notificationService,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            IWebHostEnvironment hostingEnvironment)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
            _logger = logger;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var captchaResponse = Request.Form["g-recaptcha-response"];
            var secretKey = _configuration["GoogleReCaptcha:SecretKey"];

            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={captchaResponse}",
                null);

            var jsonResponse = await response.Content.ReadAsStringAsync();

            dynamic? captchaResult = JsonConvert.DeserializeObject(jsonResponse);

            if (captchaResult?.success != true)
            {
                ModelState.AddModelError(string.Empty, "Captcha verification failed.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            if (!user.IsActive || user.IsDeleted)
            {
                ModelState.AddModelError(string.Empty, "This account is deactivated.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLoginDate = DateTime.UtcNow;

                var passwordExpiryDays = _configuration.GetValue<int>("PasswordExpiryDays");
                if (passwordExpiryDays > 0 && user.PasswordLastChanged.HasValue &&
                    DateTime.UtcNow - user.PasswordLastChanged.Value > TimeSpan.FromDays(passwordExpiryDays))
                {
                    user.MustChangePassword = true;
                }

                await _userManager.UpdateAsync(user);
                await _auditService.LogAsync(AuditAction.Login, "Account", LogType.Security);

                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl != "/")
                {
                    return LocalRedirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.RequiresTwoFactor)
            {
                var redirectUrl = QueryHelpers.AddQueryString("/Identity/Account/LoginWith2fa", new Dictionary<string, string?>
                {
                    ["returnUrl"] = model.ReturnUrl,
                    ["rememberMe"] = model.RememberMe.ToString()
                });
                return Redirect(redirectUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                await _notificationService.CreateNotificationForRole("Admin", "Account Locked",
                    $"User {user.Email} has been locked out due to too many failed login attempts.");
                ModelState.AddModelError(string.Empty, "This account is locked. Please try again later.");
                return View(model);
            }

            await _auditService.LogAsync(AuditAction.FailedLogin, "Account", LogType.Security);
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _auditService.LogAsync(AuditAction.Logout, "Account", LogType.Security);
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet("/Profile")]
        [HttpGet("/Account/Profile")]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var model = await BuildProfileViewModelAsync();
            if (model == null)
            {
                return Challenge();
            }

            return View(model);
        }

        [HttpPost("/Profile")]
        [HttpPost("/Account/Profile")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                var hydratedModel = await BuildProfileViewModelAsync(model);
                return View(hydratedModel ?? model);
            }

            user.FirstName = model.FirstName;
            user.MiddleName = model.MiddleName;
            user.LastName = model.LastName;
            user.Suffix = model.Suffix;
            user.DateOfBirth = model.DateOfBirth?.Date;
            user.Nationality = model.Nationality;
            user.PhoneNumber = model.PhoneNumber;
            user.AlternatePhone = model.AlternatePhone;
            user.AddressStreet = model.AddressStreet;
            user.AddressBarangay = model.AddressBarangay;
            user.AddressCity = model.AddressCity;
            user.AddressProvince = model.AddressProvince;
            user.AddressZipCode = model.AddressZipCode;
            user.EmergencyContactName = model.EmergencyContactName;
            user.EmergencyContactRelationship = model.EmergencyContactRelationship;
            user.EmergencyContactPhone = model.EmergencyContactPhone;
            user.ModifiedBy = User.Identity?.Name ?? "System";
            user.ModifiedDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                var hydratedModel = await BuildProfileViewModelAsync(model);
                return View(hydratedModel ?? model);
            }

            await _auditService.LogAsync(AuditAction.Update, "Account", LogType.Security, resource: "Profile");
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file.";
                return RedirectToAction(nameof(Profile));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only JPG, PNG, and GIF files are allowed.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var uploadsPath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{user.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.ProfilePicturePath = $"/uploads/profiles/{fileName}";
            user.ModifiedBy = User.Identity?.Name ?? "System";
            user.ModifiedDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _auditService.LogAsync(AuditAction.Update, "Account", LogType.Security, resource: "ProfilePicture");

            TempData["Success"] = "Profile picture updated.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            user.MustChangePassword = false;
            user.PasswordLastChanged = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Your password has been changed.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<ProfileViewModel?> BuildProfileViewModelAsync(ProfileViewModel? existing = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            var employee = await _context.Employees
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            var roles = await _userManager.GetRolesAsync(user);
            var recentSecurityLogs = await _context.AuditLogs
                .Where(log => log.UserId == user.Id && log.LogType == LogType.Security)
                .OrderByDescending(log => log.Timestamp)
                .Take(8)
                .ToListAsync();

            var model = existing ?? new ProfileViewModel();
            model.FirstName = existing?.FirstName ?? user.FirstName;
            model.MiddleName = existing?.MiddleName ?? user.MiddleName;
            model.LastName = existing?.LastName ?? user.LastName;
            model.Suffix = existing?.Suffix ?? user.Suffix;
            model.DateOfBirth = existing?.DateOfBirth ?? user.DateOfBirth;
            model.Gender = user.Gender?.ToString();
            model.CivilStatus = user.CivilStatus?.ToString();
            model.Nationality = existing?.Nationality ?? user.Nationality;
            model.Email = user.Email ?? string.Empty;
            model.PhoneNumber = existing?.PhoneNumber ?? user.PhoneNumber ?? string.Empty;
            model.AlternatePhone = existing?.AlternatePhone ?? user.AlternatePhone;
            model.AddressStreet = existing?.AddressStreet ?? user.AddressStreet;
            model.AddressBarangay = existing?.AddressBarangay ?? user.AddressBarangay;
            model.AddressCity = existing?.AddressCity ?? user.AddressCity;
            model.AddressProvince = existing?.AddressProvince ?? user.AddressProvince;
            model.AddressZipCode = existing?.AddressZipCode ?? user.AddressZipCode;
            model.EmergencyContactName = existing?.EmergencyContactName ?? user.EmergencyContactName;
            model.EmergencyContactRelationship = existing?.EmergencyContactRelationship ?? user.EmergencyContactRelationship;
            model.EmergencyContactPhone = existing?.EmergencyContactPhone ?? user.EmergencyContactPhone;
            model.ProfilePicturePath = user.ProfilePicturePath;
            model.EmployeeNumber = employee?.EmployeeNumber ?? string.Empty;
            model.Department = employee?.Department?.Name;
            model.Position = employee?.Position?.Name;
            model.EmploymentType = employee?.EmploymentType;
            model.BasicSalary = employee?.BasicSalary ?? 0;
            model.SalaryType = employee?.SalaryType ?? SalaryType.Monthly;
            model.PayFrequency = employee?.PayFrequency ?? PayFrequency.Monthly;
            model.BankName = employee?.BankName;
            model.BankAccountNumber = employee?.BankAccountNumber;
            model.TIN = employee?.TIN;
            model.SSSNumber = employee?.SSSNumber;
            model.PhilHealthNumber = employee?.PhilHealthNumber;
            model.PagIBIGNumber = employee?.PagIBIGNumber;
            model.HireDate = employee?.HireDate ?? DateTime.MinValue;
            model.ShiftName = employee?.Shift?.ShiftName ?? "Not Assigned";
            model.Status = employee?.Status.ToString() ?? (user.IsActive ? "Active" : "Inactive");
            model.Roles = roles.ToList();
            model.IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            model.RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
            model.LastLoginDate = user.LastLoginDate;
            model.PasswordLastChanged = user.PasswordLastChanged;
            model.LastIpAddress = recentSecurityLogs.FirstOrDefault(log => log.Action == AuditAction.Login)?.IpAddress;
            model.RecentSecurityLogs = recentSecurityLogs;

            return model;
        }
    }
}
