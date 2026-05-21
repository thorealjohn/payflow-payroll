using itpayroll.Models;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedPayrollSettings
    {
        public static async Task RunAsync(ApplicationDbContext context)
        {
            if (!await context.PayrollSettings.AnyAsync())
            {
                context.PayrollSettings.Add(new PayrollSetting
                {
                    Frequency = PayrollFrequency.SemiMonthly,
                    Cutoff1StartDay = 1,
                    Cutoff1EndDay = 15,
                    Cutoff2StartDay = 16,
                    Cutoff2EndDay = 30
                });
            }

            if (!await context.TaxBrackets.AnyAsync())
            {
                context.TaxBrackets.AddRange(
                    new TaxBracket { MinAmount = 0, MaxAmount = 250000, BaseTax = 0, TaxRate = 0, SortOrder = 1, Year = 2025 },
                    new TaxBracket { MinAmount = 250000, MaxAmount = 400000, BaseTax = 0, TaxRate = 0.15m, SortOrder = 2, Year = 2025 },
                    new TaxBracket { MinAmount = 400000, MaxAmount = 800000, BaseTax = 22500, TaxRate = 0.20m, SortOrder = 3, Year = 2025 },
                    new TaxBracket { MinAmount = 800000, MaxAmount = 2000000, BaseTax = 102500, TaxRate = 0.25m, SortOrder = 4, Year = 2025 },
                    new TaxBracket { MinAmount = 2000000, MaxAmount = 8000000, BaseTax = 402500, TaxRate = 0.30m, SortOrder = 5, Year = 2025 },
                    new TaxBracket { MinAmount = 8000000, MaxAmount = null, BaseTax = 2202500, TaxRate = 0.35m, SortOrder = 6, Year = 2025 }
                );
            }

            if (!await context.PhilHealthRates.AnyAsync())
            {
                context.PhilHealthRates.Add(new PhilHealthRate
                {
                    MinSalary = 10000,
                    MaxSalary = 100000,
                    Rate = 0.05m,
                    EmployeeSharePercentage = 0.50m,
                    Year = 2025
                });
            }

            if (!await context.PagIBIGRates.AnyAsync())
            {
                context.PagIBIGRates.AddRange(
                    new PagIBIGRate { MinSalary = 0, MaxSalary = 1500, EmployeeRate = 0.01m, EmployerRate = 0.02m, MaxContribution = 100, Year = 2025 },
                    new PagIBIGRate { MinSalary = 1500.01m, MaxSalary = null, EmployeeRate = 0.02m, EmployerRate = 0.02m, MaxContribution = 100, Year = 2025 }
                );
            }

            await context.SaveChangesAsync();
        }
    }
}
