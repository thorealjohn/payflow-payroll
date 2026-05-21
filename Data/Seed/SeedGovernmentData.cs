using itpayroll.Models;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Data.Seed
{
    public static class SeedGovernmentData
    {
        public static async Task RunAsync(ApplicationDbContext context)
        {
            if (!await context.SSSContributions.AnyAsync())
            {
                var contributions = new List<SSSContribution>();

                for (int msc = 5000; msc <= 35000; msc += 500)
                {
                    var minSalary = msc == 5000 ? 0m : msc - 250;
                    var maxSalary = msc == 35000 ? 9999999.99m : msc + 249.99m;
                    var ecc = msc >= 15000 ? 30m : 10m;

                    contributions.Add(new SSSContribution
                    {
                        MinSalary = minSalary,
                        MaxSalary = maxSalary,
                        MSC = msc,
                        EmployeeShare = msc * 0.05m,
                        EmployerShare = msc * 0.10m,
                        ECC = ecc,
                        TotalContribution = msc * 0.15m + ecc,
                        Year = 2025
                    });
                }

                await context.SSSContributions.AddRangeAsync(contributions);
                await context.SaveChangesAsync();
            }
        }
    }
}
