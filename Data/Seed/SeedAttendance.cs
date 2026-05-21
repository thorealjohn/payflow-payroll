using itpayroll.Models;
using itpayroll.Services;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedAttendance
    {
        public static async Task SeedShiftAssignmentsAsync(
            ApplicationDbContext context,
            List<Employee> employees)
        {
            foreach (var emp in employees)
            {
                if (emp.ShiftId is int sid &&
                    !await context.EmployeeShiftAssignments.AnyAsync(a => a.EmployeeId == emp.EmployeeId))
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
        }

        public static async Task RunAsync(
            ApplicationDbContext context,
            List<Employee> employees,
            HashSet<(int EmployeeId, DateTime Date)> leaveExclusions,
            DateTime attendanceStart,
            DateTime attendanceEnd)
        {
            if (await context.Attendances.AnyAsync())
                return;

            var attendanceService = new AttendanceService(context);

            foreach (var emp in employees)
            {
                for (var d = attendanceStart; d <= attendanceEnd; d = d.AddDays(1))
                {
                    if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                        continue;
                    if (d < emp.HireDate.Date)
                        continue;

                    if (emp.Status != EmploymentStatus.Active && emp.TerminationDate.HasValue && d > emp.TerminationDate.Value)
                        continue;

                    if (leaveExclusions.Contains((emp.EmployeeId, d)))
                        continue;

                    var daySeed = d.Day * 17 + emp.EmployeeId;
                    var empIndex = employees.FindIndex(e => e.EmployeeId == emp.EmployeeId);

                    var row = BuildSeededAttendance(emp.EmployeeId, emp.ShiftId, d, emp.Shift, daySeed, empIndex, attendanceService);
                    context.Attendances.Add(row);
                }
            }
        }

        private static Attendance BuildSeededAttendance(
            int employeeId,
            int? shiftId,
            DateTime date,
            Shift? shift,
            int daySeed,
            int employeeIndex,
            AttendanceService attendanceService)
        {
            TimeSpan timeIn;
            TimeSpan timeOut;
            double totalHours;
            var otHours = 0.0;

            if (shift?.IsNightShift == true && shift.EndTime < shift.StartTime)
            {
                timeIn = shift.StartTime;
                timeOut = shift.EndTime;

                if (employeeIndex != 0)
                {
                    if (daySeed % 9 == 0)
                        timeIn = timeIn.Add(TimeSpan.FromMinutes(12));
                    if (daySeed % 7 == 0)
                    {
                        timeOut = timeOut.Add(TimeSpan.FromMinutes(45));
                        otHours = 0.75;
                    }
                }

                totalHours = (TimeSpan.FromHours(24) - timeIn + timeOut).TotalHours;
            }
            else if (shift != null)
            {
                timeIn = shift.StartTime;
                timeOut = shift.EndTime;

                switch (employeeIndex)
                {
                    case 0:
                        break;
                    case 1:
                        if (daySeed % 3 == 0)
                            timeIn = timeIn.Add(TimeSpan.FromMinutes(25));
                        if (daySeed % 10 == 0)
                        {
                            timeOut = timeOut.Add(TimeSpan.FromMinutes(45));
                            otHours = 0.75;
                        }
                        break;
                    case 5:
                        if (daySeed % 8 == 0)
                            timeIn = timeIn.Add(TimeSpan.FromMinutes(10));
                        if (daySeed % 4 == 0)
                        {
                            timeOut = timeOut.Add(TimeSpan.FromMinutes(150));
                            otHours = 2.5;
                        }
                        break;
                    default:
                        if (daySeed % 8 == 0)
                            timeIn = timeIn.Add(TimeSpan.FromMinutes(18));
                        if (daySeed % 6 == 0)
                        {
                            timeOut = timeOut.Add(TimeSpan.FromMinutes(60));
                            otHours = 1;
                        }
                        break;
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
