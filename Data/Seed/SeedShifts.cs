using itpayroll.Models;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedShifts
    {
        public static async Task RunAsync(ApplicationDbContext context)
        {
            if (!await context.Shifts.AnyAsync())
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
