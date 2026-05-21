using itpayroll.Constant;
using Microsoft.AspNetCore.Identity;

namespace itpayroll.Data.Seed
{
    public static class SeedRoles
    {
        public static async Task RunAsync(RoleManager<IdentityRole> roleManager)
        {
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
        }
    }
}
