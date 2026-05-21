using itpayroll.Models;
using itpayroll.Services;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedPayroll
    {
        public static async Task RunAsync(
            ApplicationDbContext context,
            List<Employee> employees,
            DateTime payrollMonthStart,
            DateTime payrollMonthEnd)
        {
            var attendanceService = new AttendanceService(context);
            var payrollService = new PayrollService(
                context,
                attendanceService,
                new GovernmentService(context),
                new TaxService());

            foreach (var emp in employees)
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
                }
            }
        }
    }
}
