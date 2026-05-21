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

        public async Task<decimal> ComputePhilHealth(decimal salary)
        {
            if (salary <= 0)
                return 0;

            var rate = await _context.PhilHealthRates.FirstOrDefaultAsync();
            if (rate == null)
                return 0;

            var baseSalary = Math.Min(Math.Max(salary, rate.MinSalary), rate.MaxSalary ?? salary);
            return (baseSalary * rate.Rate) * rate.EmployeeSharePercentage;
        }

        public async Task<decimal> ComputePagIBIG(decimal salary)
        {
            if (salary <= 0)
                return 0;

            var rate = await _context.PagIBIGRates
                .Where(r => salary >= r.MinSalary && (r.MaxSalary == null || salary <= r.MaxSalary))
                .OrderBy(r => r.MinSalary)
                .FirstOrDefaultAsync();

            if (rate == null)
                return 0;

            var contribution = salary * rate.EmployeeRate;
            return Math.Min(contribution, rate.MaxContribution);
        }
    }
}
