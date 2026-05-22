using itpayroll.Areas.Identity.Data;
using itpayroll.Constant;
using itpayroll.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public class EmployeeSeedSpec
    {
        public string FirstName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string LastName { get; init; } = string.Empty;
        public string EmailLocal { get; init; } = string.Empty;
        public string EmployeeNumber { get; init; } = string.Empty;
        public decimal BasicSalary { get; init; }
        public string DepartmentName { get; init; } = string.Empty;
        public string PositionName { get; init; } = string.Empty;
        public EmploymentType EmploymentType { get; init; }
        public string BankName { get; init; } = string.Empty;
        public string BankAccountNumber { get; init; } = string.Empty;
        public string Tin { get; init; } = string.Empty;
        public string Sss { get; init; } = string.Empty;
        public string PhilHealth { get; init; } = string.Empty;
        public string PagIbig { get; init; } = string.Empty;
        public Gender Gender { get; init; }
        public CivilStatus CivilStatus { get; init; }
        public DateTime DateOfBirth { get; init; }
        public int HireMonthsAgo { get; init; }
        public int ShiftIndex { get; init; }
        public string Street { get; init; } = string.Empty;
        public string Barangay { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Province { get; init; } = string.Empty;
        public string Zip { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string EmergencyName { get; init; } = string.Empty;
        public string EmergencyRelation { get; init; } = string.Empty;
        public string EmergencyPhone { get; init; } = string.Empty;
        public EmploymentStatus Status { get; init; } = EmploymentStatus.Active;
        public DateTime? TerminationDate { get; init; }
    }

    public static class SeedEmployees
    {

        public static readonly EmployeeSeedSpec[] Specs =
        {
            new()
            {
                FirstName = "Maria",
                MiddleName = "Isabel",
                LastName = "Santos",
                EmailLocal = "maria.santos",
                EmployeeNumber = "EMP-2025-001",
                BasicSalary = 48_000m,
                DepartmentName = "Information Technology",
                PositionName = "Senior Software Engineer",
                EmploymentType = EmploymentType.Regular,
                BankName = "BPI",
                BankAccountNumber = "1234-5678-9012",
                Tin = "123-456-789-000",
                Sss = "33-1111111-1",
                PhilHealth = "12-345678901-2",
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
                EmergencyPhone = "09182233445",
                Status = EmploymentStatus.Active
            },
            new()
            {
                FirstName = "Juan",
                MiddleName = "Miguel",
                LastName = "Dela Cruz",
                EmailLocal = "juan.delacruz",
                EmployeeNumber = "EMP-2025-002",
                BasicSalary = 35_000m,
                DepartmentName = "Finance",
                PositionName = "Accounting Supervisor",
                EmploymentType = EmploymentType.Regular,
                BankName = "BDO Unibank",
                BankAccountNumber = "007123456789",
                Tin = "234-567-890-000",
                Sss = "34-2222222-2",
                PhilHealth = "23-456789012-3",
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
                EmergencyPhone = "09193344556",
                Status = EmploymentStatus.Active
            },
            new()
            {
                FirstName = "Ana",
                MiddleName = "Patricia",
                LastName = "Reyes",
                EmailLocal = "ana.reyes",
                EmployeeNumber = "EMP-2025-003",
                BasicSalary = 28_500m,
                DepartmentName = "Human Resources",
                PositionName = "HR Specialist",
                EmploymentType = EmploymentType.Regular,
                BankName = "Metrobank",
                BankAccountNumber = "9876543210987",
                Tin = "345-678-901-000",
                Sss = "35-3333333-3",
                PhilHealth = "34-567890123-4",
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
                EmergencyPhone = "09204455667",
                Status = EmploymentStatus.Active
            },
            new()
            {
                FirstName = "Jose",
                MiddleName = "Ramon",
                LastName = "Bautista",
                EmailLocal = "jose.bautista",
                EmployeeNumber = "EMP-2025-004",
                BasicSalary = 22_000m,
                DepartmentName = "Sales",
                PositionName = "Sales Associate",
                EmploymentType = EmploymentType.Regular,
                BankName = "Union Bank of the Philippines",
                BankAccountNumber = "1098765432109",
                Tin = "456-789-012-000",
                Sss = "36-4444444-4",
                PhilHealth = "45-678901234-5",
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
                EmergencyPhone = "09215566778",
                Status = EmploymentStatus.Active
            },
            new()
            {
                FirstName = "Liza",
                MiddleName = "Mae",
                LastName = "Gonzales",
                EmailLocal = "liza.gonzales",
                EmployeeNumber = "EMP-2025-005",
                BasicSalary = 18_000m,
                DepartmentName = "Operations",
                PositionName = "Logistics Coordinator",
                EmploymentType = EmploymentType.Contractual,
                BankName = "RCBC",
                BankAccountNumber = "5566778899001",
                Tin = "567-890-123-000",
                Sss = "37-5555555-5",
                PhilHealth = "56-789012345-6",
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
                EmergencyPhone = "09226677889",
                Status = EmploymentStatus.Active
            },
            new()
            {
                FirstName = "Pedro",
                MiddleName = "Ricardo",
                LastName = "Gutierrez",
                EmailLocal = "pedro.gutierrez",
                EmployeeNumber = "EMP-2025-006",
                BasicSalary = 32_000m,
                DepartmentName = "Operations",
                PositionName = "Operations Lead",
                EmploymentType = EmploymentType.Regular,
                BankName = "Landbank",
                BankAccountNumber = "010987654321",
                Tin = "678-901-234-000",
                Sss = "38-6666666-6",
                PhilHealth = "67-890123456-7",
                PagIbig = "6789-0123-4567",
                Gender = Gender.Male,
                CivilStatus = CivilStatus.Married,
                DateOfBirth = new DateTime(1990, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                HireMonthsAgo = 16,
                ShiftIndex = 0,
                Street = "56 Katipunan Avenue",
                Barangay = "Loyola Heights",
                City = "Quezon City",
                Province = "Metro Manila",
                Zip = "1108",
                Phone = "09176789012",
                EmergencyName = "Lucia Gutierrez",
                EmergencyRelation = "Spouse",
                EmergencyPhone = "09237788990",
                Status = EmploymentStatus.Active
            },
            new()
            {
                FirstName = "Rosario",
                MiddleName = "Diana",
                LastName = "Mendoza",
                EmailLocal = "rosario.mendoza",
                EmployeeNumber = "EMP-2025-007",
                BasicSalary = 25_000m,
                DepartmentName = "Finance",
                PositionName = "Finance Clerk",
                EmploymentType = EmploymentType.Regular,
                BankName = "BPI",
                BankAccountNumber = "113355779922",
                Tin = "789-012-345-000",
                Sss = "39-7777777-7",
                PhilHealth = "78-901234567-8",
                PagIbig = "7890-1234-5678",
                Gender = Gender.Female,
                CivilStatus = CivilStatus.Single,
                DateOfBirth = new DateTime(1997, 3, 22, 0, 0, 0, DateTimeKind.Utc),
                HireMonthsAgo = 8,
                ShiftIndex = 0,
                Street = "22 Shaw Boulevard",
                Barangay = "Wack-Wack",
                City = "Mandaluyong City",
                Province = "Metro Manila",
                Zip = "1552",
                Phone = "09227890123",
                EmergencyName = "Elena Mendoza",
                EmergencyRelation = "Mother",
                EmergencyPhone = "09248899001",
                Status = EmploymentStatus.Suspended,
                TerminationDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                FirstName = "Carlos",
                MiddleName = "Eduardo",
                LastName = "Villanueva",
                EmailLocal = "carlos.villanueva",
                EmployeeNumber = "EMP-2025-008",
                BasicSalary = 20_000m,
                DepartmentName = "Administration",
                PositionName = "Admin Assistant",
                EmploymentType = EmploymentType.Regular,
                BankName = "Metrobank",
                BankAccountNumber = "224466880011",
                Tin = "890-123-456-000",
                Sss = "40-8888888-8",
                PhilHealth = "89-012345678-9",
                PagIbig = "8901-2345-6789",
                Gender = Gender.Male,
                CivilStatus = CivilStatus.Single,
                DateOfBirth = new DateTime(1999, 11, 8, 0, 0, 0, DateTimeKind.Utc),
                HireMonthsAgo = 4,
                ShiftIndex = 0,
                Street = "77 Commonwealth Avenue",
                Barangay = "Holy Spirit",
                City = "Quezon City",
                Province = "Metro Manila",
                Zip = "1127",
                Phone = "09238901234",
                EmergencyName = "Roberto Villanueva",
                EmergencyRelation = "Father",
                EmergencyPhone = "09259911223",
                Status = EmploymentStatus.Inactive,
                TerminationDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                FirstName = "Grace",
                MiddleName = "Marie",
                LastName = "Torres",
                EmailLocal = "grace.torres",
                EmployeeNumber = "EMP-2025-009",
                BasicSalary = 30_000m,
                DepartmentName = "Marketing",
                PositionName = "Marketing Specialist",
                EmploymentType = EmploymentType.Regular,
                BankName = "BDO Unibank",
                BankAccountNumber = "998877665544",
                Tin = "901-234-567-000",
                Sss = "41-9999999-9",
                PhilHealth = "90-123456789-0",
                PagIbig = "9012-3456-7890",
                Gender = Gender.Female,
                CivilStatus = CivilStatus.Single,
                DateOfBirth = new DateTime(1994, 7, 14, 0, 0, 0, DateTimeKind.Utc),
                HireMonthsAgo = 12,
                ShiftIndex = 0,
                Street = "15 Esteban Street",
                Barangay = "Legazpi Village",
                City = "Makati City",
                Province = "Metro Manila",
                Zip = "1229",
                Phone = "09249012345",
                EmergencyName = "Ana Torres",
                EmergencyRelation = "Sister",
                EmergencyPhone = "09260022334",
                Status = EmploymentStatus.Active
            },
            new()
            {
                FirstName = "Mark",
                MiddleName = "Andrei",
                LastName = "Navarro",
                EmailLocal = "mark.navarro",
                EmployeeNumber = "EMP-2025-010",
                BasicSalary = 26_000m,
                DepartmentName = "Information Technology",
                PositionName = "Junior Software Developer",
                EmploymentType = EmploymentType.Regular,
                BankName = "UnionBank",
                BankAccountNumber = "335577991122",
                Tin = "012-345-678-000",
                Sss = "42-0000000-0",
                PhilHealth = "01-234567890-1",
                PagIbig = "0123-4567-8901",
                Gender = Gender.Male,
                CivilStatus = CivilStatus.Single,
                DateOfBirth = new DateTime(2000, 1, 30, 0, 0, 0, DateTimeKind.Utc),
                HireMonthsAgo = 7,
                ShiftIndex = 0,
                Street = "89 East Avenue",
                Barangay = "Diliman",
                City = "Quezon City",
                Province = "Metro Manila",
                Zip = "1101",
                Phone = "09250123456",
                EmergencyName = "Susan Navarro",
                EmergencyRelation = "Mother",
                EmergencyPhone = "09271133445",
                Status = EmploymentStatus.Active
            }
        };

        public static async Task<List<Employee>> RunAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            string? seedEmployeePassword = configuration["SeedEmployees:Password"];
            if (string.IsNullOrWhiteSpace(seedEmployeePassword))
            {
                throw new InvalidOperationException("SeedEmployees:Password is not configured. Set it in appsettings.{Environment}.json.");
            }

            var companyDomain = configuration["CompanySettings:Domain"] ?? "payflow.com";
            var shifts = await context.Shifts.OrderBy(s => s.ShiftId).ToListAsync();
            var departments = await context.Departments.ToDictionaryAsync(d => d.Name, d => d.DepartmentId);
            var positions = await context.Positions.ToDictionaryAsync(p => (p.Name, p.DepartmentId), p => p.PositionId);

            if (shifts.Count == 0)
            {
                throw new InvalidOperationException("Cannot seed employees: no shifts exist.");
            }

            int ShiftIdAt(int index) => shifts[index % shifts.Count].ShiftId;

            var createdEmployees = new List<Employee>(Specs.Length);

            foreach (var s in Specs)
            {
                var email = $"{s.EmailLocal}@{companyDomain}";
                var hireDate = DateTime.UtcNow.AddMonths(-s.HireMonthsAgo).Date;
                var deptId = departments.GetValueOrDefault(s.DepartmentName);
                var posId = deptId > 0 ? positions.GetValueOrDefault((s.PositionName, deptId)) : (int?)null;
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
                            var employee = new Employee
                            {
                                UserId = demoUser.Id,
                                EmployeeNumber = s.EmployeeNumber,
                                Status = s.Status,
                                BasicSalary = s.BasicSalary,
                                HireDate = hireDate,
                                TerminationDate = s.TerminationDate,
                                ShiftId = ShiftIdAt(s.ShiftIndex),
                                DepartmentId = deptId > 0 ? deptId : null,
                                PositionId = posId,
                                EmploymentType = s.EmploymentType,
                                SalaryType = SalaryType.Monthly,

                                BankName = s.BankName,
                                BankAccountNumber = s.BankAccountNumber,
                                TIN = s.Tin,
                                SSSNumber = s.Sss,
                                PhilHealthNumber = s.PhilHealth,
                                PagIBIGNumber = s.PagIbig,
                                CreatedBy = "SeedData",
                                CreatedDate = DateTime.UtcNow
                            };
                            context.Employees.Add(employee);
                            createdEmployees.Add(employee);
                        }
                    }
                    else
                    {
                        var existing = await context.Employees.FirstAsync(e => e.UserId == demoUser.Id);
                        createdEmployees.Add(existing);
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
                    IsActive = s.Status == EmploymentStatus.Active,
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

                var newEmployee = new Employee
                {
                    UserId = user.Id,
                    EmployeeNumber = s.EmployeeNumber,
                    Status = s.Status,
                    BasicSalary = s.BasicSalary,
                    HireDate = hireDate,
                    TerminationDate = s.TerminationDate,
                    ShiftId = ShiftIdAt(s.ShiftIndex),
                    DepartmentId = deptId > 0 ? deptId : null,
                    PositionId = posId,
                    EmploymentType = s.EmploymentType,
                    SalaryType = SalaryType.Monthly,
                    BankName = s.BankName,
                    BankAccountNumber = s.BankAccountNumber,
                    TIN = s.Tin,
                    SSSNumber = s.Sss,
                    PhilHealthNumber = s.PhilHealth,
                    PagIBIGNumber = s.PagIbig,
                    CreatedBy = "SeedData",
                    CreatedDate = DateTime.UtcNow
                };
                context.Employees.Add(newEmployee);
                createdEmployees.Add(newEmployee);
            }

            await context.SaveChangesAsync();

            return await context.Employees
                .Include(e => e.Shift)
                .Where(e => e.CreatedBy == "SeedData")
                .OrderBy(e => e.EmployeeNumber)
                .ToListAsync();
        }
    }
}
