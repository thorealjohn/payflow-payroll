using itpayroll;
using itpayroll.Areas.Identity.Data;
using itpayroll.Data;
using itpayroll.Filters;
using itpayroll.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure();
    }));
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure();
    }), ServiceLifetime.Scoped);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;
});

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 10;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    // Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User
    options.User.RequireUniqueEmail = true;

    // Sign-in
    options.SignIn.RequireConfirmedEmail = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SensitiveActionPasswordFilter>();
    options.Filters.Add<AuditLoggingActionFilter>();
    options.Filters.Add<RequirePasswordChangeAttribute>();
    options.Filters.Add<RequireTwoFactorAttribute>();
});

// Add authorization policies for role-based access
builder.Services.AddAuthorization(options =>
{
    // SuperAdmin - Full system access
    options.AddPolicy("RequireSuperAdmin", 
        policy => policy.RequireRole("SuperAdmin"));
    
    // Admin and above - Can manage HR users, view reports
    options.AddPolicy("RequireAdminOrAbove", 
        policy => policy.RequireRole("SuperAdmin", "Admin"));
    
    // HR and above - Can manage employees, attendance, basic payroll
    options.AddPolicy("RequireHROrAbove", 
        policy => policy.RequireRole("SuperAdmin", "Admin", "HR"));
});

builder.Services.AddScoped<GovernmentService>();
builder.Services.AddScoped<TaxService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<PayrollService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromHours(8);
});
builder.Services.AddScoped<AuditService>();
builder.Services.AddSingleton<IPdfService, PdfService>();

builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));
builder.Services.PostConfigure<BrandingOptions>(o =>
{
    if (string.IsNullOrWhiteSpace(o.LogoPath))
        o.LogoPath = "~/images/logo.jpg";
});

var cultureInfo = new CultureInfo("en-PH");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        await SeedData.InitializeAsync(userManager, roleManager, builder.Configuration, context, app.Environment);

        // Migrate existing Admin/HR users without Employee records
        try
        {
            var adminHrRoleNames = new[] { "Admin", "HR" };
            var usersNeedingEmployees = new List<(ApplicationUser User, string RoleName)>();

            foreach (var roleName in adminHrRoleNames)
            {
                var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
                foreach (var user in usersInRole)
                {
                    var hasEmployee = await context.Employees.AnyAsync(e => e.UserId == user.Id);
                    if (!hasEmployee)
                        usersNeedingEmployees.Add((user, roleName));
                }
            }

            if (usersNeedingEmployees.Count > 0)
            {
                var adminDeptId = (await context.Departments.FirstOrDefaultAsync(d => d.Name == "Administration"))?.DepartmentId;
                var hrDeptId = (await context.Departments.FirstOrDefaultAsync(d => d.Name == "Human Resources"))?.DepartmentId;
                var hrPositionId = hrDeptId.HasValue
                    ? (await context.Positions.FirstOrDefaultAsync(p => p.Name == "HR Officer" && p.DepartmentId == hrDeptId.Value))?.PositionId
                    : null;
                var adminPositionId = adminDeptId.HasValue
                    ? (await context.Positions.FirstOrDefaultAsync(p => p.Name == "Admin Officer" && p.DepartmentId == adminDeptId.Value))?.PositionId
                    : null;

                var accessNumberCounters = new Dictionary<string, int>();
                async Task<string> GenerateAccessEmployeeNumber(string roleName)
                {
                    var employeeNumberPrefix = roleName == "HR" ? "HR" : "ADM";
                    var prefix = $"{employeeNumberPrefix}-{DateTime.UtcNow.Year}-";
                    if (accessNumberCounters.TryGetValue(prefix, out var cachedNextNumber))
                    {
                        accessNumberCounters[prefix] = cachedNextNumber + 1;
                        return $"{prefix}{cachedNextNumber:D3}";
                    }

                    var existingNumbers = await context.Employees
                        .Where(e => e.EmployeeNumber.StartsWith(prefix))
                        .Select(e => e.EmployeeNumber)
                        .ToListAsync();

                    var nextNumber = 1;
                    foreach (var employeeNumber in existingNumbers)
                    {
                        var numberPart = employeeNumber[prefix.Length..];
                        if (int.TryParse(numberPart, out var number) && number >= nextNumber)
                            nextNumber = number + 1;
                    }

                    accessNumberCounters[prefix] = nextNumber + 1;
                    return $"{prefix}{nextNumber:D3}";
                }

                foreach (var (user, roleName) in usersNeedingEmployees)
                {
                    context.Employees.Add(new itpayroll.Models.Employee
                    {
                        UserId = user.Id,
                        EmployeeNumber = await GenerateAccessEmployeeNumber(roleName),
                        BasicSalary = 0,
                        HireDate = user.CreatedDate != default ? user.CreatedDate : DateTime.UtcNow,
                        Status = itpayroll.Models.EmploymentStatus.Active,
                        DepartmentId = roleName == "HR" ? hrDeptId : (roleName == "Admin" ? adminDeptId : null),
                        PositionId = roleName == "HR" ? hrPositionId : (roleName == "Admin" ? adminPositionId : null),
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Created {Count} missing Employee records for existing Admin/HR users.", usersNeedingEmployees.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to migrate existing Admin/HR users to Employee records. Non-fatal, continuing startup.");
        }

        var auditRetentionDays = builder.Configuration.GetValue<int?>("Audit:RetentionDays") ?? 90;
        var auditService = services.GetRequiredService<AuditService>();
        await auditService.PruneAsync(auditRetentionDays);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during application startup. Database migration, seeding, or audit pruning failed.");
        throw;
    }
}

// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}*/

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
