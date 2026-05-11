using itpayroll.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace itpayroll.Services
{
    public class GovernmentService
    {
        private readonly ApplicationDbContext _context;

        public GovernmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> ComputeSSS(decimal salary)
        {
            var bracket = await _context.SSSContributions
                .Where(s => salary >= s.MinSalary && salary <= s.MaxSalary)
                .FirstOrDefaultAsync();

            return bracket?.EmployeeShare ?? 0;
        }

        public decimal ComputePhilHealth(decimal salary)
        {
            if (salary <= 0)
                return 0;

            decimal min = 10000;
            decimal max = 80000;
            decimal rate = 0.04m;

            var baseSalary = Math.Min(Math.Max(salary, min), max);
            return (baseSalary * rate) / 2;
        }

        public decimal ComputePagIBIG(decimal salary)
        {
            if (salary <= 1500)
                return salary * 0.01m;

            return Math.Min(salary * 0.02m, 100);
        }
    }
}