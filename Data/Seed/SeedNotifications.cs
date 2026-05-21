using itpayroll.Models;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedNotifications
    {
        public static async Task RunAsync(
            ApplicationDbContext context,
            List<Employee> employees)
        {
            if (await context.Notifications.AnyAsync())
                return;

            foreach (var emp in employees)
            {

                context.Notifications.Add(new Notification
                {
                    UserId = emp.UserId,
                    Title = "Welcome to Payflow",
                    Message = "Your demo employee account is set up. Review attendance, leave balances, and payroll in the portal.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                });

                context.Notifications.Add(new Notification
                {
                    UserId = emp.UserId,
                    Title = "Payroll Processed",
                    Message = "Your payroll for the previous month has been processed. Check your payslip in the portal.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow.AddDays(-3)
                });

                if (emp.Status != EmploymentStatus.Active)
                {
                    context.Notifications.Add(new Notification
                    {
                        UserId = emp.UserId,
                        Title = "Account Status Update",
                        Message = $"Your account is currently {(emp.Status == EmploymentStatus.Suspended ? "suspended" : "inactive")}. Please contact HR for details.",
                        IsRead = false,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    });
                }
            }
        }
    }
}
