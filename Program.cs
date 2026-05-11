using itpayroll;
using itpayroll.Areas.Identity.Data;
using itpayroll.Data;
using itpayroll.Filters;
using itpayroll.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
    }));
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
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SensitiveActionPasswordFilter>();
    options.Filters.Add<AuditLoggingActionFilter>();
    options.Filters.Add<RequirePasswordChangeAttribute>();
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
builder.Services.AddSession();
builder.Services.AddScoped<AuditService>();

builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));
builder.Services.PostConfigure<BrandingOptions>(o =>
{
    if (string.IsNullOrWhiteSpace(o.LogoPath))
        o.LogoPath = "~/images/logo.jpg";
});

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

app.UseDeveloperExceptionPage();
app.UseMigrationsEndPoint();

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
