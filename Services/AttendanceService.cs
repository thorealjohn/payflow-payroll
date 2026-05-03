using itpayroll.Data;
using itpayroll.Models;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Services
{
    public class AttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(double hours, double overtime, double nightShiftHours)> GetHoursAsync(int employeeId, DateTime start, DateTime end)
        {
            var records = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId &&
                            a.Date >= start &&
                            a.Date <= end)
                .ToListAsync();

            double totalHours = records.Sum(a => a.TotalHours);
            double overtime = records.Sum(a => a.OvertimeHours);
            double nightShiftHours = records.Sum(a => a.NightShiftHours);

            return (totalHours, overtime, nightShiftHours);
        }

        public (int lateMinutes, int undertimeMinutes) CalculateLateAndUndertime(
            TimeSpan timeIn, TimeSpan timeOut, Shift shift, DateTime date)
        {
            var lateMinutes = 0;
            var undertimeMinutes = 0;

            if (shift == null)
                return (lateMinutes, undertimeMinutes);

            // Combine date with times
            var shiftStart = date.Date.Add(shift.StartTime);
            var shiftEnd = date.Date.Add(shift.EndTime);
            var timeInDateTime = date.Date.Add(timeIn);
            var timeOutDateTime = date.Date.Add(timeOut);

            // Handle night shift
            if (shift.IsNightShift)
            {
                // If end time is less than start time, it's next day
                if (shift.EndTime < shift.StartTime)
                {
                    shiftEnd = shiftEnd.AddDays(1);
                }

                // If time out is less than time in, it's next day
                if (timeOut < timeIn)
                {
                    timeOutDateTime = timeOutDateTime.AddDays(1);
                }
            }

            // Calculate late minutes
            var graceTime = shiftStart.AddMinutes(shift.GracePeriodMinutes);
            if (timeInDateTime > graceTime)
            {
                lateMinutes = (int)(timeInDateTime - graceTime).TotalMinutes;
            }

            // Calculate undertime minutes
            if (timeOutDateTime < shiftEnd)
            {
                undertimeMinutes = (int)(shiftEnd - timeOutDateTime).TotalMinutes;
            }

            return (lateMinutes, undertimeMinutes);
        }

        public double CalculateNightShiftHours(TimeSpan timeIn, TimeSpan timeOut, DateTime date)
        {
            // Night shift window: 10:00 PM - 6:00 AM
            var nightStart = date.Date.AddHours(22); // 10 PM
            var nightEnd = date.Date.AddDays(1).AddHours(6); // 6 AM next day

            var timeInDateTime = date.Date.Add(timeIn);
            var timeOutDateTime = date.Date.Add(timeOut);

            // Handle time out past midnight
            if (timeOut < timeIn)
            {
                timeOutDateTime = timeOutDateTime.AddDays(1);
            }

            // Calculate overlap with night shift window
            var overlapStart = Max(timeInDateTime, nightStart);
            var overlapEnd = Min(timeOutDateTime, nightEnd);

            if (overlapEnd > overlapStart)
            {
                return (overlapEnd - overlapStart).TotalHours;
            }

            return 0;
        }

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        public async Task<(int lateMinutes, int undertimeMinutes)> CalculateLateAndUndertime(int attendanceId)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Shift)
                .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

            if (attendance?.Employee?.Shift == null)
                return (0, 0);

            return CalculateLateAndUndertime(
                attendance.TimeIn,
                attendance.TimeOut,
                attendance.Employee.Shift,
                attendance.Date);
        }
    }
}