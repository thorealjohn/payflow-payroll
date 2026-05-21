using itpayroll.Areas.Identity.Data;
using itpayroll.Data.Seed;
using itpayroll.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            await SeedRoles.RunAsync(roleManager);
            await SeedUsers.RunAsync(userManager, roleManager, configuration);
            await SeedGovernmentData.RunAsync(context);
            await SeedShifts.RunAsync(context);
            await SeedDepartments.RunAsync(context);
            await SeedAccessUsers.RunAsync(userManager, configuration, context);
            var employees = await SeedEmployees.RunAsync(userManager, configuration, context);

            if (employees.Count == 0)
                return;

            var anchor = employees.FirstOrDefault(e => e.EmployeeNumber == "EMP-2025-001") ?? employees[0];
            if (await context.Attendances.AnyAsync(a => a.EmployeeId == anchor.EmployeeId))
                return;

            var today = DateTime.UtcNow.Date;
            var payrollMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            var payrollMonthEnd = payrollMonthStart.AddMonths(1).AddDays(-1);
            var attendanceStart = today.AddMonths(-3);
            var attendanceEnd = today;

            var leaveExclusions = await SeedLeaves.RunAsync(context, userManager, configuration, employees, payrollMonthStart, today);
            await SeedAttendance.SeedShiftAssignmentsAsync(context, employees);
            await SeedAttendance.RunAsync(context, employees, leaveExclusions, attendanceStart, attendanceEnd);
            await SeedOvertime.RunAsync(context, userManager, configuration, employees, payrollMonthStart);
            await SeedNotifications.RunAsync(context, employees);
            await context.SaveChangesAsync();
            await SeedPayroll.RunAsync(context, employees, payrollMonthStart, payrollMonthEnd);
        }
    }
}
