using itpayroll.Areas.Identity.Data;
using itpayroll.Constant;
using itpayroll.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedAccessUsers
    {
        public static async Task RunAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            var password = configuration["SeedAccessUsers:Password"]
                ?? configuration["SeedEmployees:Password"];

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("SeedAccessUsers:Password or SeedEmployees:Password is required to seed Admin and HR users.");
            }

            var domain = configuration["CompanySettings:Domain"] ?? "payflow.com";
            await EnsureAccessUserAsync(
                userManager,
                context,
                email: configuration["Admin:Email"] ?? $"admin@{domain}",
                password,
                firstName: "Company",
                lastName: "Admin",
                role: Roles.Admin,
                employeeNumberPrefix: "ADM",
                departmentName: "Administration",
                positionName: "Admin Officer");

            await EnsureAccessUserAsync(
                userManager,
                context,
                email: configuration["HR:Email"] ?? $"hr@{domain}",
                password,
                firstName: "Human",
                lastName: "Resources",
                role: Roles.HR,
                employeeNumberPrefix: "HR",
                departmentName: "Human Resources",
                positionName: "HR Officer");
        }

        private static async Task EnsureAccessUserAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            string email,
            string password,
            string firstName,
            string lastName,
            string role,
            string employeeNumberPrefix,
            string departmentName,
            string positionName)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    MustChangePassword = false,
                    PasswordLastChanged = DateTime.UtcNow,
                    IsActive = true,
                    CreatedBy = "SeedData",
                    CreatedDate = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Seed access user failed for {email}: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            var employee = await context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee != null)
                return;

            var department = await context.Departments.FirstOrDefaultAsync(d => d.Name == departmentName);
            var position = department == null
                ? null
                : await context.Positions.FirstOrDefaultAsync(p => p.Name == positionName && p.DepartmentId == department.DepartmentId);

            context.Employees.Add(new Employee
            {
                UserId = user.Id,
                EmployeeNumber = await GenerateAccessEmployeeNumber(context, employeeNumberPrefix),
                Status = EmploymentStatus.Active,
                BasicSalary = 0,
                HireDate = DateTime.UtcNow.Date,
                DepartmentId = department?.DepartmentId,
                PositionId = position?.PositionId,
                SalaryType = SalaryType.Monthly,
                CreatedBy = "SeedData",
                CreatedDate = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }

        private static async Task<string> GenerateAccessEmployeeNumber(ApplicationDbContext context, string prefix)
        {
            var yearPrefix = $"{prefix}-{DateTime.UtcNow.Year}-";
            var existingNumbers = await context.Employees
                .Where(e => e.EmployeeNumber.StartsWith(yearPrefix))
                .Select(e => e.EmployeeNumber)
                .ToListAsync();

            var nextNumber = 1;
            foreach (var employeeNumber in existingNumbers)
            {
                var numberPart = employeeNumber[yearPrefix.Length..];
                if (int.TryParse(numberPart, out var number) && number >= nextNumber)
                    nextNumber = number + 1;
            }

            return $"{yearPrefix}{nextNumber:D3}";
        }
    }
}
