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

        public async Task<(double hours, double overtime)> GetHoursAsync(int employeeId, DateTime start, DateTime end)
        {
            var records = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId &&
                            a.Date >= start &&
                            a.Date <= end)
                .ToListAsync();

            double totalHours = records.Sum(a => a.TotalHours);
            double overtime = records.Sum(a => a.OvertimeHours);

            return (totalHours, overtime);
        }
    }
}