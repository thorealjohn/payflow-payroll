using itpayroll.Areas.Identity.Data;
using itpayroll.Constant;
using Microsoft.AspNetCore.Identity;

namespace itpayroll.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            
            // ROLES
     
            string[] roles = {
            Roles.SuperAdmin,
            Roles.Admin,
            Roles.HR,
            Roles.Employee
        };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }


            // SUPERADMIN

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

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
                }
            }
            else
            {
                // Ensure the existing user has the SuperAdmin role
                if (!await userManager.IsInRoleAsync(existingUser, Roles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(existingUser, Roles.SuperAdmin);
                }
            }
        }
    }
}
