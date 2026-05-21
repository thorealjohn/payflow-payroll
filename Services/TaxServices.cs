using itpayroll.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace itpayroll.Services
{
    public class TaxService
    {
        private readonly ApplicationDbContext _context;

        public TaxService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> ComputeTax(decimal taxableIncome)
        {
            if (taxableIncome < 0)
                taxableIncome = 0;

            var brackets = await _context.TaxBrackets
                .OrderBy(t => t.SortOrder)
                .ToListAsync();

            foreach (var b in brackets)
            {
                if (taxableIncome >= b.MinAmount)
                {
                    if (!b.MaxAmount.HasValue || taxableIncome <= b.MaxAmount.Value)
                    {
                        return b.BaseTax + (taxableIncome - b.MinAmount) * b.TaxRate;
                    }
                }
            }

            return 0;
        }
    }
}
