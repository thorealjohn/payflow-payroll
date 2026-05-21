using itpayroll.Areas.Identity.Data;
using itpayroll.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedLeaves
    {

        public static async Task<HashSet<(int EmployeeId, DateTime Date)>> RunAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            List<Employee> employees,
            DateTime payrollMonthStart,
            DateTime today)
        {
            if (await context.LeaveRequests.AnyAsync())
                return [];

            string? superEmail = configuration["SuperAdmin:Email"];
            var approver = !string.IsNullOrEmpty(superEmail)
                ? await userManager.FindByEmailAsync(superEmail)
                : null;

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

                (0, payrollMonthStart.AddDays(13), payrollMonthStart.AddDays(15), LeaveType.VacationLeave,   LeaveStatus.Approved, "Family trip"),
                (1, payrollMonthStart.AddDays(8),  payrollMonthStart.AddDays(8),  LeaveType.SickLeave,      LeaveStatus.Approved, "Medical appointment"),
                (2, today.AddDays(-10),            today.AddDays(-9),             LeaveType.EmergencyLeave,  LeaveStatus.Pending,  "Personal matter"),
                (2, payrollMonthStart.AddDays(3),  payrollMonthStart.AddDays(4),  LeaveType.SickLeave,      LeaveStatus.Rejected, "Not enough leave credits"),
                (3, payrollMonthStart.AddDays(21), payrollMonthStart.AddDays(22), LeaveType.VacationLeave,   LeaveStatus.Approved, "Short break"),
                (4, payrollMonthStart.AddDays(16), payrollMonthStart.AddDays(17), LeaveType.VacationLeave,   LeaveStatus.Approved, "Travel"),
                (5, today.AddDays(-5),             today.AddDays(-5),             LeaveType.SickLeave,      LeaveStatus.Pending,  "Under the weather"),
                (8, payrollMonthStart.AddDays(10), payrollMonthStart.AddDays(11), LeaveType.VacationLeave,   LeaveStatus.Approved, "Family event"),
                (9, payrollMonthStart.AddDays(5),  payrollMonthStart.AddDays(5),  LeaveType.SickLeave,      LeaveStatus.Approved, "Doctor checkup")
            };

            foreach (var spec in leaveSpecs)
            {
                if (spec.Index < 0 || spec.Index >= employees.Count)
                    continue;

                var emp = employees[spec.Index];
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

            return leaveDayKeys;
        }
    }
}
