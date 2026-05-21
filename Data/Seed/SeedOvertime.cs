using itpayroll.Areas.Identity.Data;
using itpayroll.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedOvertime
    {
        public static async Task RunAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            List<Employee> employees,
            DateTime payrollMonthStart)
        {
            if (await context.Overtimes.AnyAsync())
                return;

            string? superEmail = configuration["SuperAdmin:Email"];
            var approver = !string.IsNullOrEmpty(superEmail)
                ? await userManager.FindByEmailAsync(superEmail)
                : null;

            var otSpecs = new (int Index, DateTime Date, double Hours, string Reason)[]
            {
                (0, payrollMonthStart.AddDays(17), 2,   "Release support"),
                (1, payrollMonthStart.AddDays(10), 1.5, "Month-end closing"),
                (3, payrollMonthStart.AddDays(18), 3,   "Sales event prep"),
                (4, payrollMonthStart.AddDays(11), 1,   "Warehouse inventory"),
                (5, payrollMonthStart.AddDays(6),  2.5, "Process optimization"),
                (5, payrollMonthStart.AddDays(14), 3,   "Shift coverage"),
                (5, payrollMonthStart.AddDays(22), 2,   "System migration"),
                (8, payrollMonthStart.AddDays(9),  1.5, "Campaign launch prep"),
                (9, payrollMonthStart.AddDays(12), 1,   "Deploy support")
            };

            foreach (var (index, date, hours, reason) in otSpecs)
            {
                if (index < 0 || index >= employees.Count)
                    continue;

                context.Overtimes.Add(new Overtime
                {
                    EmployeeId = employees[index].EmployeeId,
                    Date = date.Date,
                    Hours = hours,
                    Reason = reason,
                    Status = OvertimeStatus.Approved,
                    ApprovedBy = approver?.Email ?? "SeedData",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                });
            }
        }
    }
}
