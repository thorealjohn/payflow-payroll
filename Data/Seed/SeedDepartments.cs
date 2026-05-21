using itpayroll.Models;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedDepartments
    {
        public static readonly Dictionary<string, string[]> DepartmentsWithPositions = new()
        {
            ["Administration"] = new[] { "System Administrator", "System Owner", "Operations Manager", "Office Administrator", "Branch Manager", "Admin Officer", "Admin Assistant" },
            ["Human Resources"] = new[] { "HR Manager", "HR Officer", "HR Staff", "Recruitment Officer", "Payroll Officer", "HR Specialist" },
            ["Information Technology"] = new[] { "Senior Software Engineer", "Junior Software Developer", "Software Developer", "System Administrator", "IT Support", "Technician" },
            ["Finance"] = new[] { "Accounting Supervisor", "Finance Clerk", "Accountant", "Finance Manager" },
            ["Operations"] = new[] { "Logistics Coordinator", "Operations Lead", "Operations Manager" },
            ["Sales"] = new[] { "Sales Associate", "Sales Staff", "Sales Manager" },
            ["Marketing"] = new[] { "Marketing Specialist", "Marketing Manager" },
            ["Payroll"] = new[] { "Payroll Officer", "Payroll Manager" },
            ["Customer Service"] = new[] { "Customer Service Representative", "Support Staff" },
            ["Warehouse"] = new[] { "Warehouse Staff", "Driver", "Warehouse Supervisor" }
        };

        public static async Task RunAsync(ApplicationDbContext context)
        {
            if (await context.Departments.AnyAsync())
                return;

            foreach (var (deptName, positions) in DepartmentsWithPositions)
            {
                var dept = new Department { Name = deptName };
                context.Departments.Add(dept);
                await context.SaveChangesAsync();

                foreach (var posName in positions)
                {
                    context.Positions.Add(new Position
                    {
                        Name = posName,
                        DepartmentId = dept.DepartmentId
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
