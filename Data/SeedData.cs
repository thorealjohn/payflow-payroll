using itpayroll.Areas.Identity.Data;
using itpayroll.Constant;
using itpayroll.Models;
using itpayroll.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace itpayroll.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context,
            IHostEnvironment hostEnvironment)
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

            // DEMO EMPLOYEES (end users: Identity + Employee row). Idempotent: creates any missing demo users.
            // Password: SeedEmployees:Password, or in Development only defaults to EmployeeSeed123! if unset.
            // Password must meet Identity policy (length 10+, upper, lower, digit, symbol).
            string? seedEmployeePassword = configuration["SeedEmployees:Password"];
            if (string.IsNullOrWhiteSpace(seedEmployeePassword) && hostEnvironment.IsDevelopment())
                seedEmployeePassword = "EmployeeSeed123!";

            if (!string.IsNullOrWhiteSpace(seedEmployeePassword))
            {
                var companyDomain = configuration["CompanySettings:Domain"] ?? "payflow.com";
                var shifts = await context.Shifts.OrderBy(s => s.ShiftId).ToListAsync();
                if (shifts.Count == 0)
                {
                    throw new InvalidOperationException("Cannot seed employees: no shifts exist.");
                }

                int ShiftIdAt(int index) => shifts[index % shifts.Count].ShiftId;

                var specs = new[]
                {
                    new
                    {
                        FirstName = "Maria",
                        MiddleName = (string?)"Isabel",
                        LastName = "Santos",
                        EmailLocal = "maria.santos",
                        EmployeeNumber = "EMP-2025-001",
                        BasicSalary = 48_000m,
                        Department = "Information Technology",
                        Position = "Senior Software Engineer",
                        EmploymentType = EmploymentType.FullTime,
                        PayFrequency = PayFrequency.Monthly,
                        BankName = "BPI",
                        BankAccountNumber = "1234-5678-9012",
                        Tin = "123-456-789-000",
                        Sss = "33-1111111-1",
                        PhilHealth = "123456789012",
                        PagIbig = "1234-5678-9012",
                        Gender = Gender.Female,
                        CivilStatus = CivilStatus.Single,
                        DateOfBirth = new DateTime(1991, 4, 18, 0, 0, 0, DateTimeKind.Utc),
                        HireMonthsAgo = 22,
                        ShiftIndex = 0,
                        Street = "45 Kamias Road",
                        Barangay = "Kamuning",
                        City = "Quezon City",
                        Province = "Metro Manila",
                        Zip = "1102",
                        Phone = "09171234567",
                        EmergencyName = "Rosa Santos",
                        EmergencyRelation = "Mother",
                        EmergencyPhone = "09182233445"
                    },
                    new
                    {
                        FirstName = "Juan",
                        MiddleName = (string?)"Miguel",
                        LastName = "Dela Cruz",
                        EmailLocal = "juan.delacruz",
                        EmployeeNumber = "EMP-2025-002",
                        BasicSalary = 35_000m,
                        Department = "Finance",
                        Position = "Accounting Supervisor",
                        EmploymentType = EmploymentType.FullTime,
                        PayFrequency = PayFrequency.SemiMonthly,
                        BankName = "BDO Unibank",
                        BankAccountNumber = "007123456789",
                        Tin = "234-567-890-000",
                        Sss = "34-2222222-2",
                        PhilHealth = "234567890123",
                        PagIbig = "2345-6789-0123",
                        Gender = Gender.Male,
                        CivilStatus = CivilStatus.Married,
                        DateOfBirth = new DateTime(1988, 7, 9, 0, 0, 0, DateTimeKind.Utc),
                        HireMonthsAgo = 18,
                        ShiftIndex = 1,
                        Street = "12 Ayala Avenue",
                        Barangay = "Salcedo Village",
                        City = "Makati City",
                        Province = "Metro Manila",
                        Zip = "1227",
                        Phone = "09182345678",
                        EmergencyName = "Carmen Dela Cruz",
                        EmergencyRelation = "Spouse",
                        EmergencyPhone = "09193344556"
                    },
                    new
                    {
                        FirstName = "Ana",
                        MiddleName = (string?)"Patricia",
                        LastName = "Reyes",
                        EmailLocal = "ana.reyes",
                        EmployeeNumber = "EMP-2025-003",
                        BasicSalary = 28_500m,
                        Department = "Human Resources",
                        Position = "HR Specialist",
                        EmploymentType = EmploymentType.FullTime,
                        PayFrequency = PayFrequency.Monthly,
                        BankName = "Metrobank",
                        BankAccountNumber = "9876543210987",
                        Tin = "345-678-901-000",
                        Sss = "35-3333333-3",
                        PhilHealth = "345678901234",
                        PagIbig = "3456-7890-1234",
                        Gender = Gender.Female,
                        CivilStatus = CivilStatus.Married,
                        DateOfBirth = new DateTime(1993, 11, 2, 0, 0, 0, DateTimeKind.Utc),
                        HireMonthsAgo = 14,
                        ShiftIndex = 0,
                        Street = "88 Ortigas Avenue",
                        Barangay = "San Antonio",
                        City = "Pasig City",
                        Province = "Metro Manila",
                        Zip = "1605",
                        Phone = "09193456789",
                        EmergencyName = "Paolo Reyes",
                        EmergencyRelation = "Spouse",
                        EmergencyPhone = "09204455667"
                    },
                    new
                    {
                        FirstName = "Jose",
                        MiddleName = (string?)"Ramon",
                        LastName = "Bautista",
                        EmailLocal = "jose.bautista",
                        EmployeeNumber = "EMP-2025-004",
                        BasicSalary = 22_000m,
                        Department = "Sales",
                        Position = "Sales Associate",
                        EmploymentType = EmploymentType.FullTime,
                        PayFrequency = PayFrequency.SemiMonthly,
                        BankName = "Union Bank of the Philippines",
                        BankAccountNumber = "1098765432109",
                        Tin = "456-789-012-000",
                        Sss = "36-4444444-4",
                        PhilHealth = "456789012345",
                        PagIbig = "4567-8901-2345",
                        Gender = Gender.Male,
                        CivilStatus = CivilStatus.Single,
                        DateOfBirth = new DateTime(1996, 1, 25, 0, 0, 0, DateTimeKind.Utc),
                        HireMonthsAgo = 10,
                        ShiftIndex = 2,
                        Street = "210 Taft Avenue",
                        Barangay = "Malate",
                        City = "Manila",
                        Province = "Metro Manila",
                        Zip = "1004",
                        Phone = "09204567890",
                        EmergencyName = "Teresa Bautista",
                        EmergencyRelation = "Sister",
                        EmergencyPhone = "09215566778"
                    },
                    new
                    {
                        FirstName = "Liza",
                        MiddleName = (string?)"Mae",
                        LastName = "Gonzales",
                        EmailLocal = "liza.gonzales",
                        EmployeeNumber = "EMP-2025-005",
                        BasicSalary = 18_000m,
                        Department = "Operations",
                        Position = "Logistics Coordinator",
                        EmploymentType = EmploymentType.Contractual,
                        PayFrequency = PayFrequency.Monthly,
                        BankName = "RCBC",
                        BankAccountNumber = "5566778899001",
                        Tin = "567-890-123-000",
                        Sss = "37-5555555-5",
                        PhilHealth = "567890123456",
                        PagIbig = "5678-9012-3456",
                        Gender = Gender.Female,
                        CivilStatus = CivilStatus.Single,
                        DateOfBirth = new DateTime(1995, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                        HireMonthsAgo = 6,
                        ShiftIndex = 1,
                        Street = "33 Marcos Highway",
                        Barangay = "Masinag",
                        City = "Antipolo City",
                        Province = "Rizal",
                        Zip = "1870",
                        Phone = "09215678901",
                        EmergencyName = "Mark Gonzales",
                        EmergencyRelation = "Brother",
                        EmergencyPhone = "09226677889"
                    }
                };

                foreach (var s in specs)
                {
                    var email = $"{s.EmailLocal}@{companyDomain}";
                    var hireDate = DateTime.UtcNow.AddMonths(-s.HireMonthsAgo).Date;
                    var demoUser = await userManager.FindByEmailAsync(email);

                    if (demoUser != null)
                    {
                        if (!await userManager.IsInRoleAsync(demoUser, Roles.Employee))
                            await userManager.AddToRoleAsync(demoUser, Roles.Employee);

                        var hasEmployee = await context.Employees.AnyAsync(e => e.UserId == demoUser.Id);
                        if (!hasEmployee)
                        {
                            var numberTaken = await context.Employees.AnyAsync(e => e.EmployeeNumber == s.EmployeeNumber);
                            if (!numberTaken)
                            {
                                context.Employees.Add(new Employee
                                {
                                    UserId = demoUser.Id,
                                    EmployeeNumber = s.EmployeeNumber,
                                    Status = EmploymentStatus.Active,
                                    BasicSalary = s.BasicSalary,
                                    HireDate = hireDate,
                                    ShiftId = ShiftIdAt(s.ShiftIndex),
                                    Department = s.Department,
                                    Position = s.Position,
                                    EmploymentType = s.EmploymentType,
                                    SalaryType = SalaryType.Monthly,
                                    PayFrequency = s.PayFrequency,
                                    BankName = s.BankName,
                                    BankAccountNumber = s.BankAccountNumber,
                                    TIN = s.Tin,
                                    SSSNumber = s.Sss,
                                    PhilHealthNumber = s.PhilHealth,
                                    PagIBIGNumber = s.PagIbig,
                                    CreatedBy = "SeedData",
                                    CreatedDate = DateTime.UtcNow
                                });
                            }
                        }

                        continue;
                    }

                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = s.FirstName,
                        MiddleName = s.MiddleName,
                        LastName = s.LastName,
                        PhoneNumber = s.Phone,
                        EmailConfirmed = true,
                        MustChangePassword = false,
                        PasswordLastChanged = DateTime.UtcNow,
                        IsActive = true,
                        Gender = s.Gender,
                        CivilStatus = s.CivilStatus,
                        DateOfBirth = s.DateOfBirth,
                        Nationality = "Filipino",
                        AddressStreet = s.Street,
                        AddressBarangay = s.Barangay,
                        AddressCity = s.City,
                        AddressProvince = s.Province,
                        AddressZipCode = s.Zip,
                        EmergencyContactName = s.EmergencyName,
                        EmergencyContactRelationship = s.EmergencyRelation,
                        EmergencyContactPhone = s.EmergencyPhone,
                        CreatedBy = "SeedData",
                        CreatedDate = DateTime.UtcNow
                    };

                    var createResult = await userManager.CreateAsync(user, seedEmployeePassword);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Seed employee failed for {email}: {errors}");
                    }

                    await userManager.AddToRoleAsync(user, Roles.Employee);

                    context.Employees.Add(new Employee
                    {
                        UserId = user.Id,
                        EmployeeNumber = s.EmployeeNumber,
                        Status = EmploymentStatus.Active,
                        BasicSalary = s.BasicSalary,
                        HireDate = hireDate,
                        ShiftId = ShiftIdAt(s.ShiftIndex),
                        Department = s.Department,
                        Position = s.Position,
                        EmploymentType = s.EmploymentType,
                        SalaryType = SalaryType.Monthly,
                        PayFrequency = s.PayFrequency,
                        BankName = s.BankName,
                        BankAccountNumber = s.BankAccountNumber,
                        TIN = s.Tin,
                        SSSNumber = s.Sss,
                        PhilHealthNumber = s.PhilHealth,
                        PagIBIGNumber = s.PagIbig,
                        CreatedBy = "SeedData",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();
            }

            await SeedDemoEmployeeOperationalDataIfNeededAsync(context, userManager, configuration);
        }

        /// <summary>
        /// Adds shift assignments, attendance (weekdays), leave requests, overtime, notifications,
        /// and one processed payroll (previous calendar month) for employees created by SeedData.
        /// Runs once per database: skipped if the first seed employee already has attendance rows.
        /// </summary>
        private static async Task SeedDemoEmployeeOperationalDataIfNeededAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            var seeded = await context.Employees
                .Include(e => e.Shift)
                .Where(e => e.CreatedBy == "SeedData")
                .OrderBy(e => e.EmployeeNumber)
                .ToListAsync();

            if (seeded.Count == 0)
                return;

            var anchorEmployee = seeded.FirstOrDefault(e => e.EmployeeNumber == "EMP-2025-001") ?? seeded[0];
            if (await context.Attendances.AnyAsync(a => a.EmployeeId == anchorEmployee.EmployeeId))
                return;

            var attendanceService = new AttendanceService(context);
            var today = DateTime.UtcNow.Date;
            var payrollMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            var payrollMonthEnd = payrollMonthStart.AddMonths(1).AddDays(-1);
            var attendanceStart = payrollMonthStart;
            var attendanceEnd = today;

            string? superEmail = configuration["SuperAdmin:Email"];
            var approver = !string.IsNullOrEmpty(superEmail)
                ? await userManager.FindByEmailAsync(superEmail)
                : null;

            foreach (var emp in seeded)
            {
                if (emp.ShiftId is int sid)
                {
                    context.EmployeeShiftAssignments.Add(new EmployeeShiftAssignment
                    {
                        EmployeeId = emp.EmployeeId,
                        ShiftId = sid,
                        DateFrom = emp.HireDate.Date,
                        DateTo = null
                    });
                }
            }

            var leaveDayKeys = new HashSet<(int EmployeeId, DateTime Date)>();

            void AddWeekdayLeaveDays(int employeeId, DateTime from, DateTime to)
            {
                for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
                {
                    if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                        continue;
                    leaveDayKeys.Add((employeeId, d));
                }
            }

            var leaveSpecs = new List<(int Index, DateTime Start, DateTime End, LeaveType Type, LeaveStatus Status, string? Reason)>
            {
                (0, payrollMonthStart.AddDays(13), payrollMonthStart.AddDays(15), LeaveType.VacationLeave, LeaveStatus.Approved, "Family trip"),
                (1, payrollMonthStart.AddDays(8), payrollMonthStart.AddDays(8), LeaveType.SickLeave, LeaveStatus.Approved, "Medical appointment"),
                (2, today.AddDays(-10), today.AddDays(-9), LeaveType.EmergencyLeave, LeaveStatus.Pending, "Personal matter"),
                (3, payrollMonthStart.AddDays(21), payrollMonthStart.AddDays(22), LeaveType.VacationLeave, LeaveStatus.Approved, "Short break"),
                (4, payrollMonthStart.AddDays(16), payrollMonthStart.AddDays(17), LeaveType.VacationLeave, LeaveStatus.Approved, "Travel")
            };

            foreach (var spec in leaveSpecs)
            {
                if (spec.Index < 0 || spec.Index >= seeded.Count)
                    continue;

                var emp = seeded[spec.Index];
                if (spec.Status is LeaveStatus.Approved or LeaveStatus.Pending)
                    AddWeekdayLeaveDays(emp.EmployeeId, spec.Start, spec.End);

                var daysRequested = Math.Max(0.5, Math.Min(100, (spec.End.Date - spec.Start.Date).TotalDays + 1));

                context.LeaveRequests.Add(new LeaveRequest
                {
                    EmployeeId = emp.EmployeeId,
                    LeaveType = spec.Type,
                    StartDate = spec.Start.Date,
                    EndDate = spec.End.Date,
                    DaysRequested = daysRequested,
                    Reason = spec.Reason,
                    Status = spec.Status,
                    ApprovedById = spec.Status == LeaveStatus.Approved ? approver?.Id : null,
                    ApprovedDate = spec.Status == LeaveStatus.Approved ? DateTime.UtcNow.AddDays(-14) : null,
                    CreatedDate = DateTime.UtcNow.AddDays(-20)
                });
            }

            foreach (var emp in seeded)
            {
                for (var d = attendanceStart; d <= attendanceEnd; d = d.AddDays(1))
                {
                    if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                        continue;
                    if (d < emp.HireDate.Date)
                        continue;
                    if (leaveDayKeys.Contains((emp.EmployeeId, d)))
                        continue;

                    var daySeed = d.Day * 17 + emp.EmployeeId;
                    var row = BuildSeededAttendance(emp.EmployeeId, emp.ShiftId, d, emp.Shift, daySeed, attendanceService);
                    context.Attendances.Add(row);
                }
            }

            var otSpecs = new (int Index, DateTime Date, double Hours, string Reason)[]
            {
                (0, payrollMonthStart.AddDays(17), 2, "Release support"),
                (1, payrollMonthStart.AddDays(10), 1.5, "Month-end closing"),
                (3, payrollMonthStart.AddDays(18), 3, "Sales event"),
                (4, payrollMonthStart.AddDays(11), 1, "Warehouse inventory")
            };

            foreach (var (index, date, hours, reason) in otSpecs)
            {
                if (index < 0 || index >= seeded.Count)
                    continue;
                context.Overtimes.Add(new Overtime
                {
                    EmployeeId = seeded[index].EmployeeId,
                    Date = date.Date,
                    Hours = hours,
                    Reason = reason,
                    Status = OvertimeStatus.Approved,
                    ApprovedBy = approver?.Email ?? "SeedData",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                });
            }

            foreach (var emp in seeded)
            {
                context.Notifications.Add(new Notification
                {
                    UserId = emp.UserId,
                    Title = "Welcome to Payflow",
                    Message = "Your demo employee account is set up. Review attendance, leave balances, and payroll in the portal.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();

            var payrollService = new PayrollService(
                context,
                attendanceService,
                new GovernmentService(context),
                new TaxService());

            foreach (var emp in seeded)
            {
                var hasPayroll = await context.Payrolls.AnyAsync(p =>
                    p.EmployeeId == emp.EmployeeId &&
                    p.PeriodStart == payrollMonthStart &&
                    p.PeriodEnd == payrollMonthEnd);

                if (hasPayroll)
                    continue;

                try
                {
                    await payrollService.ProcessPayrollAsync(emp.EmployeeId, payrollMonthStart, payrollMonthEnd);
                }
                catch
                {
                    // Skip payroll if validation fails (e.g. zero-hour edge case); core demo data still usable
                }
            }
        }

        private static Attendance BuildSeededAttendance(
            int employeeId,
            int? shiftId,
            DateTime date,
            Shift? shift,
            int daySeed,
            AttendanceService attendanceService)
        {
            TimeSpan timeIn;
            TimeSpan timeOut;
            double totalHours;
            var otHours = 0.0;

            if (shift?.IsNightShift == true && shift.EndTime < shift.StartTime)
            {
                timeIn = shift.StartTime;
                if (daySeed % 9 == 0)
                    timeIn = timeIn.Add(TimeSpan.FromMinutes(12));

                timeOut = shift.EndTime;
                totalHours = (TimeSpan.FromHours(24) - timeIn + timeOut).TotalHours;
                if (daySeed % 7 == 0)
                {
                    timeOut = timeOut.Add(TimeSpan.FromMinutes(45));
                    otHours = 0.75;
                    totalHours = (TimeSpan.FromHours(24) - timeIn + timeOut).TotalHours;
                }
            }
            else if (shift != null)
            {
                timeIn = shift.StartTime;
                timeOut = shift.EndTime;
                if (daySeed % 8 == 0)
                    timeIn = timeIn.Add(TimeSpan.FromMinutes(18));
                if (daySeed % 6 == 0)
                {
                    timeOut = timeOut.Add(TimeSpan.FromMinutes(60));
                    otHours = 1;
                }

                totalHours = (timeOut - timeIn).TotalHours;
            }
            else
            {
                timeIn = new TimeSpan(8, 0, 0);
                timeOut = new TimeSpan(17, 0, 0);
                totalHours = 9;
            }

            var (late, undertime) = shift != null
                ? attendanceService.CalculateLateAndUndertime(timeIn, timeOut, shift, date.Date)
                : (0, 0);
            var nightH = shift != null
                ? attendanceService.CalculateNightShiftHours(timeIn, timeOut, shift, date.Date)
                : 0;

            return new Attendance
            {
                EmployeeId = employeeId,
                ShiftId = shiftId,
                Date = date.Date,
                TimeIn = timeIn,
                TimeOut = timeOut,
                TotalHours = totalHours,
                OvertimeHours = otHours,
                LateMinutes = late,
                UndertimeMinutes = undertime,
                NightShiftHours = nightH,
                DayType = DayType.Regular,
                CreatedDate = DateTime.UtcNow
            };
        }
    }
}
