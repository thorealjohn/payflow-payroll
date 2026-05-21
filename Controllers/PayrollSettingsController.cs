using itpayroll.Constant;
using itpayroll.Data;
using itpayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Controllers
{
    [Authorize(Roles = Roles.SuperAdmin)]
    public class PayrollSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.PayrollSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new PayrollSetting();
                _context.PayrollSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            var taxBrackets = await _context.TaxBrackets.OrderBy(t => t.SortOrder).ToListAsync();
            var sssContributions = await _context.SSSContributions.OrderBy(s => s.MSC).ToListAsync();
            var philHealthRates = await _context.PhilHealthRates.OrderBy(p => p.MinSalary).ToListAsync();
            var pagIBIGRates = await _context.PagIBIGRates.OrderBy(p => p.MinSalary).ToListAsync();

            ViewBag.TaxBrackets = taxBrackets;
            ViewBag.SSSContributions = sssContributions;
            ViewBag.PhilHealthRates = philHealthRates;
            ViewBag.PagIBIGRates = pagIBIGRates;

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(PayrollSetting model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index));

            var settings = await _context.PayrollSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new PayrollSetting();
                _context.PayrollSettings.Add(settings);
            }

            settings.Frequency = model.Frequency;
            settings.Cutoff1StartDay = model.Cutoff1StartDay;
            settings.Cutoff1EndDay = model.Cutoff1EndDay;
            settings.Cutoff2StartDay = model.Cutoff2StartDay;
            settings.Cutoff2EndDay = model.Cutoff2EndDay;
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Payroll settings updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTaxBracket(TaxBracket model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid tax bracket data.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Id > 0)
            {
                var existing = await _context.TaxBrackets.FindAsync(model.Id);
                if (existing != null)
                {
                    existing.MinAmount = model.MinAmount;
                    existing.MaxAmount = model.MaxAmount;
                    existing.BaseTax = model.BaseTax;
                    existing.TaxRate = model.TaxRate;
                    existing.SortOrder = model.SortOrder;
                }
            }
            else
            {
                _context.TaxBrackets.Add(new TaxBracket
                {
                    MinAmount = model.MinAmount,
                    MaxAmount = model.MaxAmount,
                    BaseTax = model.BaseTax,
                    TaxRate = model.TaxRate,
                    SortOrder = model.SortOrder,
                    Year = model.Year
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Tax bracket saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTaxBracket(int id)
        {
            var bracket = await _context.TaxBrackets.FindAsync(id);
            if (bracket != null)
            {
                _context.TaxBrackets.Remove(bracket);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tax bracket deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSSSBracket(SSSContribution model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid SSS bracket data.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Id > 0)
            {
                var existing = await _context.SSSContributions.FindAsync(model.Id);
                if (existing != null)
                {
                    existing.MinSalary = model.MinSalary;
                    existing.MaxSalary = model.MaxSalary;
                    existing.EmployeeShare = model.EmployeeShare;
                    existing.EmployerShare = model.EmployerShare;
                    existing.MSC = model.MSC;
                    existing.ECC = model.ECC;
                    existing.TotalContribution = model.TotalContribution;
                }
            }
            else
            {
                _context.SSSContributions.Add(new SSSContribution
                {
                    MinSalary = model.MinSalary,
                    MaxSalary = model.MaxSalary,
                    EmployeeShare = model.EmployeeShare,
                    EmployerShare = model.EmployerShare,
                    MSC = model.MSC,
                    ECC = model.ECC,
                    TotalContribution = model.TotalContribution,
                    Year = model.Year
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "SSS bracket saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSSSBracket(int id)
        {
            var bracket = await _context.SSSContributions.FindAsync(id);
            if (bracket != null)
            {
                _context.SSSContributions.Remove(bracket);
                await _context.SaveChangesAsync();
                TempData["Success"] = "SSS bracket deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePhilHealthRate(PhilHealthRate model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid PhilHealth rate data.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Id > 0)
            {
                var existing = await _context.PhilHealthRates.FindAsync(model.Id);
                if (existing != null)
                {
                    existing.MinSalary = model.MinSalary;
                    existing.MaxSalary = model.MaxSalary;
                    existing.Rate = model.Rate;
                    existing.EmployeeSharePercentage = model.EmployeeSharePercentage;
                }
            }
            else
            {
                _context.PhilHealthRates.Add(new PhilHealthRate
                {
                    MinSalary = model.MinSalary,
                    MaxSalary = model.MaxSalary,
                    Rate = model.Rate,
                    EmployeeSharePercentage = model.EmployeeSharePercentage,
                    Year = model.Year
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "PhilHealth rate saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhilHealthRate(int id)
        {
            var rate = await _context.PhilHealthRates.FindAsync(id);
            if (rate != null)
            {
                _context.PhilHealthRates.Remove(rate);
                await _context.SaveChangesAsync();
                TempData["Success"] = "PhilHealth rate deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePagIBIGRate(PagIBIGRate model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid Pag-IBIG rate data.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Id > 0)
            {
                var existing = await _context.PagIBIGRates.FindAsync(model.Id);
                if (existing != null)
                {
                    existing.MinSalary = model.MinSalary;
                    existing.MaxSalary = model.MaxSalary;
                    existing.EmployeeRate = model.EmployeeRate;
                    existing.EmployerRate = model.EmployerRate;
                    existing.MaxContribution = model.MaxContribution;
                }
            }
            else
            {
                _context.PagIBIGRates.Add(new PagIBIGRate
                {
                    MinSalary = model.MinSalary,
                    MaxSalary = model.MaxSalary,
                    EmployeeRate = model.EmployeeRate,
                    EmployerRate = model.EmployerRate,
                    MaxContribution = model.MaxContribution,
                    Year = model.Year
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Pag-IBIG rate saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePagIBIGRate(int id)
        {
            var rate = await _context.PagIBIGRates.FindAsync(id);
            if (rate != null)
            {
                _context.PagIBIGRates.Remove(rate);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Pag-IBIG rate deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
