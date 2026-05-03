using itpayroll.Areas.Identity.Data;
using itpayroll.Constant;
using itpayroll.Models;
using Microsoft.AspNetCore.Identity;

namespace itpayroll.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context)
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

            // SSS CONTRIBUTION TABLE (2024 - Republic Act 11199)
            if (!context.SSSContributions.Any())
            {
                var contributions = new[]
                {
                    // MSC Range (Monthly) | Employee Share | Employer Share
                    new SSSContribution { MinSalary = 5000, MaxSalary = 5249.99m, EmployeeShare = 225, EmployerShare = 425, Year = 2024 },
                    new SSSContribution { MinSalary = 5250, MaxSalary = 5749.99m, EmployeeShare = 236.25m, EmployerShare = 446.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 5750, MaxSalary = 6249.99m, EmployeeShare = 247.50m, EmployerShare = 467.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 6250, MaxSalary = 6749.99m, EmployeeShare = 258.75m, EmployerShare = 488.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 6750, MaxSalary = 7249.99m, EmployeeShare = 270, EmployerShare = 510, Year = 2024 },
                    new SSSContribution { MinSalary = 7250, MaxSalary = 7749.99m, EmployeeShare = 281.25m, EmployerShare = 531.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 7750, MaxSalary = 8249.99m, EmployeeShare = 292.50m, EmployerShare = 552.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 8250, MaxSalary = 8749.99m, EmployeeShare = 303.75m, EmployerShare = 573.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 8750, MaxSalary = 9249.99m, EmployeeShare = 315, EmployerShare = 595, Year = 2024 },
                    new SSSContribution { MinSalary = 9250, MaxSalary = 9749.99m, EmployeeShare = 326.25m, EmployerShare = 616.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 9750, MaxSalary = 10249.99m, EmployeeShare = 337.50m, EmployerShare = 637.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 10250, MaxSalary = 10749.99m, EmployeeShare = 348.75m, EmployerShare = 658.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 10750, MaxSalary = 11249.99m, EmployeeShare = 360, EmployerShare = 680, Year = 2024 },
                    new SSSContribution { MinSalary = 11250, MaxSalary = 11749.99m, EmployeeShare = 371.25m, EmployerShare = 701.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 11750, MaxSalary = 12249.99m, EmployeeShare = 382.50m, EmployerShare = 722.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 12250, MaxSalary = 12749.99m, EmployeeShare = 393.75m, EmployerShare = 743.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 12750, MaxSalary = 13249.99m, EmployeeShare = 405, EmployerShare = 765, Year = 2024 },
                    new SSSContribution { MinSalary = 13250, MaxSalary = 13749.99m, EmployeeShare = 416.25m, EmployerShare = 786.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 13750, MaxSalary = 14249.99m, EmployeeShare = 427.50m, EmployerShare = 807.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 14250, MaxSalary = 14749.99m, EmployeeShare = 438.75m, EmployerShare = 828.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 14750, MaxSalary = 15249.99m, EmployeeShare = 450, EmployerShare = 850, Year = 2024 },
                    new SSSContribution { MinSalary = 15250, MaxSalary = 15749.99m, EmployeeShare = 461.25m, EmployerShare = 871.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 15750, MaxSalary = 16249.99m, EmployeeShare = 472.50m, EmployerShare = 892.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 16250, MaxSalary = 16749.99m, EmployeeShare = 483.75m, EmployerShare = 913.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 16750, MaxSalary = 17249.99m, EmployeeShare = 495, EmployerShare = 935, Year = 2024 },
                    new SSSContribution { MinSalary = 17250, MaxSalary = 17749.99m, EmployeeShare = 506.25m, EmployerShare = 956.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 17750, MaxSalary = 18249.99m, EmployeeShare = 517.50m, EmployerShare = 977.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 18250, MaxSalary = 18749.99m, EmployeeShare = 528.75m, EmployerShare = 998.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 18750, MaxSalary = 19249.99m, EmployeeShare = 540, EmployerShare = 1020, Year = 2024 },
                    new SSSContribution { MinSalary = 19250, MaxSalary = 19749.99m, EmployeeShare = 551.25m, EmployerShare = 1041.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 19750, MaxSalary = 20249.99m, EmployeeShare = 562.50m, EmployerShare = 1062.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 20250, MaxSalary = 20749.99m, EmployeeShare = 573.75m, EmployerShare = 1083.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 20750, MaxSalary = 21249.99m, EmployeeShare = 585, EmployerShare = 1105, Year = 2024 },
                    new SSSContribution { MinSalary = 21250, MaxSalary = 21749.99m, EmployeeShare = 596.25m, EmployerShare = 1126.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 21750, MaxSalary = 22249.99m, EmployeeShare = 607.50m, EmployerShare = 1147.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 22250, MaxSalary = 22749.99m, EmployeeShare = 618.75m, EmployerShare = 1168.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 22750, MaxSalary = 23249.99m, EmployeeShare = 630, EmployerShare = 1190, Year = 2024 },
                    new SSSContribution { MinSalary = 23250, MaxSalary = 23749.99m, EmployeeShare = 641.25m, EmployerShare = 1211.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 23750, MaxSalary = 24249.99m, EmployeeShare = 652.50m, EmployerShare = 1232.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 24250, MaxSalary = 24749.99m, EmployeeShare = 663.75m, EmployerShare = 1253.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 24750, MaxSalary = 25249.99m, EmployeeShare = 675, EmployerShare = 1275, Year = 2024 },
                    new SSSContribution { MinSalary = 25250, MaxSalary = 25749.99m, EmployeeShare = 686.25m, EmployerShare = 1296.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 25750, MaxSalary = 26249.99m, EmployeeShare = 697.50m, EmployerShare = 1317.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 26250, MaxSalary = 26749.99m, EmployeeShare = 708.75m, EmployerShare = 1338.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 26750, MaxSalary = 27249.99m, EmployeeShare = 720, EmployerShare = 1360, Year = 2024 },
                    new SSSContribution { MinSalary = 27250, MaxSalary = 27749.99m, EmployeeShare = 731.25m, EmployerShare = 1381.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 27750, MaxSalary = 28249.99m, EmployeeShare = 742.50m, EmployerShare = 1402.50m, Year = 2024 },
                    new SSSContribution { MinSalary = 28250, MaxSalary = 28749.99m, EmployeeShare = 753.75m, EmployerShare = 1423.75m, Year = 2024 },
                    new SSSContribution { MinSalary = 28750, MaxSalary = 29249.99m, EmployeeShare = 765, EmployerShare = 1445, Year = 2024 },
                    new SSSContribution { MinSalary = 29250, MaxSalary = 29749.99m, EmployeeShare = 776.25m, EmployerShare = 1466.25m, Year = 2024 },
                    new SSSContribution { MinSalary = 29750, MaxSalary = 30000, EmployeeShare = 787.50m, EmployerShare = 1487.50m, Year = 2024 },
                };

                await context.SSSContributions.AddRangeAsync(contributions);
                await context.SaveChangesAsync();
            }

            // DEFAULT SHIFTS
            if (!context.Shifts.Any())
            {
                var shifts = new[]
                {
                    new Shift
                    {
                        ShiftName = "Day Shift (8AM-5PM)",
                        StartTime = new TimeSpan(8, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        GracePeriodMinutes = 10,
                        IsNightShift = false,
                        IsActive = true,
                        Description = "Standard day shift (8AM to 5PM with 1-hour lunch break)"
                    },
                    new Shift
                    {
                        ShiftName = "Morning Shift (7AM-4PM)",
                        StartTime = new TimeSpan(7, 0, 0),
                        EndTime = new TimeSpan(16, 0, 0),
                        GracePeriodMinutes = 10,
                        IsNightShift = false,
                        IsActive = true,
                        Description = "Early morning shift (7AM to 4PM)"
                    },
                    new Shift
                    {
                        ShiftName = "Night Shift (9PM-6AM)",
                        StartTime = new TimeSpan(21, 0, 0),
                        EndTime = new TimeSpan(6, 0, 0),
                        GracePeriodMinutes = 15,
                        IsNightShift = true,
                        IsActive = true,
                        Description = "Night shift (9PM to 6AM next day)"
                    },
                    new Shift
                    {
                        ShiftName = "Graveyard Shift (10PM-7AM)",
                        StartTime = new TimeSpan(22, 0, 0),
                        EndTime = new TimeSpan(7, 0, 0),
                        GracePeriodMinutes = 15,
                        IsNightShift = true,
                        IsActive = true,
                        Description = "Graveyard shift (10PM to 7AM next day)"
                    }
                };

                await context.Shifts.AddRangeAsync(shifts);
                await context.SaveChangesAsync();
            }
        }
    }
}
