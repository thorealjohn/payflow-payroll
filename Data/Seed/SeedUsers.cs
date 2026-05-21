using itpayroll.Areas.Identity.Data;
using itpayroll.Constant;
using Microsoft.AspNetCore.Identity;

namespace itpayroll.Data.Seed
{
    public static class SeedUsers
    {
        public static async Task RunAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {

            string? superAdminEmail = configuration["SuperAdmin:Email"];
            string? superAdminPassword = configuration["SuperAdmin:Password"];

            if (string.IsNullOrEmpty(superAdminEmail) || string.IsNullOrEmpty(superAdminPassword))
            {
                throw new Exception("SuperAdmin credentials are not configured properly.");
            }

            var existingUser = await userManager.FindByEmailAsync(superAdminEmail);

            if (existingUser == null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(superAdmin, superAdminPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create SuperAdmin user: {errors}");
                }

                await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
            }
            else
            {
                if (!await userManager.IsInRoleAsync(existingUser, Roles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(existingUser, Roles.SuperAdmin);
                }

                if (!await userManager.CheckPasswordAsync(existingUser, superAdminPassword))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                    var resetResult = await userManager.ResetPasswordAsync(existingUser, token, superAdminPassword);
                    if (!resetResult.Succeeded)
                    {
                        var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to update SuperAdmin password: {errors}");
                    }
                }
            }
        }
    }
}
