using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using itpayroll.Data;
using itpayroll.Models;
using Microsoft.EntityFrameworkCore;

namespace itpayroll.Services
{
    public class PayrollService
    {
        private readonly ApplicationDbContext _context;
        private readonly AttendanceService _attendance;
        private readonly GovernmentService _gov;
        private readonly TaxService _tax;

        public PayrollService(
            ApplicationDbContext context,
            AttendanceService attendance,
            GovernmentService gov,
            TaxService tax)
        {
            _context = context;
            _attendance = attendance;
            _gov = gov;
            _tax = tax;
        }

        // =========================
        // CORE CALCULATIONS
        // =========================

        private decimal HourlyRate(decimal monthly)
            => monthly / 22 / 8;

        private decimal ComputeBasic(decimal salary, double hours)
            => HourlyRate(salary) * (decimal)hours;

        private decimal ComputeOT(decimal salary, double ot, DayType dayType)
        {
            var multiplier = dayType switch
            {
                DayType.Regular => 1.25m,
                DayType.RestDay or DayType.Holiday => 1.30m,
                DayType.RestDayHoliday => 1.50m,
                _ => 1.25m
            };
            return HourlyRate(salary) * (decimal)ot * multiplier;
        }

        private decimal ComputeNightShiftDifferential(decimal salary, double nightShiftHours)
            => HourlyRate(salary) * (decimal)nightShiftHours * 0.10m;

        /// <summary>
        /// When statutory totals exceed period gross (e.g. MSC floors), scale deductions
        /// proportionally so net pay is not negative; tax absorbs rounding remainder.
        /// </summary>
        private static void CapDeductionsToGross(ref decimal sss, ref decimal phil, ref decimal pagibig, ref decimal tax, decimal gross)
        {
            if (gross <= 0)
            {
                sss = 0;
                phil = 0;
                pagibig = 0;
                tax = 0;
                return;
            }

            var raw = sss + phil + pagibig + tax;
            if (raw <= gross)
                return;

            var factor = gross / raw;
            sss = decimal.Round(sss * factor, 2, MidpointRounding.AwayFromZero);
            phil = decimal.Round(phil * factor, 2, MidpointRounding.AwayFromZero);
            pagibig = decimal.Round(pagibig * factor, 2, MidpointRounding.AwayFromZero);
            tax = gross - sss - phil - pagibig;
            if (tax >= 0)
                return;

            pagibig += tax;
            tax = 0;
            if (pagibig < 0)
            {
                phil += pagibig;
                pagibig = 0;
            }
            if (phil < 0)
            {
                sss += phil;
                phil = 0;
            }
            sss = Math.Max(0, sss);

            var total = sss + phil + pagibig + tax;
            if (total > gross)
            {
                var cut = total - gross;
                if (tax >= cut)
                    tax -= cut;
                else
                {
                    cut -= tax;
                    tax = 0;
                    if (pagibig >= cut)
                        pagibig -= cut;
                    else
                    {
                        cut -= pagibig;
                        pagibig = 0;
                        if (phil >= cut)
                            phil -= cut;
                        else
                        {
                            cut -= phil;
                            phil = 0;
                            sss = Math.Max(0, sss - cut);
                        }
                    }
                }
            }
        }

        // =========================
        // MAIN PROCESS
        // =========================

        public async Task<Payroll> ProcessPayrollAsync(
            int employeeId,
            DateTime start,
            DateTime end)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                throw new Exception("Employee not found");

            // 🔹 Get attendance records with details
            var attendanceRecords = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.Date >= start && a.Date <= end)
                .ToListAsync();

            // 🔹 Get approved overtime records
            var approvedOvertimes = await _context.Overtimes
                .Where(o => o.EmployeeId == employeeId && o.Date >= start && o.Date <= end && o.Status == OvertimeStatus.Approved)
                .ToListAsync();

            // 🔹 Aggregate totals
            double totalHours = attendanceRecords.Sum(a => a.TotalHours);
            double totalOT = approvedOvertimes.Sum(o => o.Hours); // Use approved overtime only
            double totalNightShiftHours = attendanceRecords.Sum(a => a.NightShiftHours);

            // 🔹 Calculate weighted overtime (simplified: use most common day type or Regular)
            var mostCommonDayType = attendanceRecords
                .GroupBy(a => a.DayType)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? DayType.Regular;

            // 🔹 Earnings
            var basic = ComputeBasic(employee.BasicSalary, totalHours);
            var overtime = ComputeOT(employee.BasicSalary, totalOT, mostCommonDayType);
            var nightShiftDiff = ComputeNightShiftDifferential(employee.BasicSalary, totalNightShiftHours);

            var gross = basic + overtime + nightShiftDiff;

            // Statutory contributions and withholding use the same base as period gross
            // (earnings from this run), so deductions stay aligned with amounts earned.
            decimal contributionBase = gross;

            var sss = contributionBase > 0
                ? await _gov.ComputeSSS(contributionBase)
                : 0;
            var phil = _gov.ComputePhilHealth(contributionBase);
            var pagibig = _gov.ComputePagIBIG(contributionBase);

            var govTotal = sss + phil + pagibig;

            var taxable = gross - govTotal;
            var tax = _tax.ComputeTax(taxable);

            CapDeductionsToGross(ref sss, ref phil, ref pagibig, ref tax, gross);

            var totalDed = sss + phil + pagibig + tax;
            var net = gross - totalDed;

            // 🔹 Save Payroll
            var payroll = new Payroll
            {
                EmployeeId = employeeId,
                PeriodStart = start,
                PeriodEnd = end,
                GrossPay = gross,
                TotalDeductions = totalDed,
                NetPay = net,
                Status = PayrollStatus.Processed
            };

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(payroll, new ValidationContext(payroll), validationResults, validateAllProperties: true))
            {
                throw new InvalidOperationException(string.Join("; ", validationResults.Select(r => r.ErrorMessage)));
            }

            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync();

            // 🔹 Save Earnings
            var earnings = new List<Earning>
            {
                new Earning { PayrollId = payroll.PayrollId, Type = EarningType.BasicPay, Amount = basic },
                new Earning { PayrollId = payroll.PayrollId, Type = EarningType.Overtime, Amount = overtime }
            };

            if (nightShiftDiff > 0)
            {
                earnings.Add(new Earning { PayrollId = payroll.PayrollId, Type = EarningType.NightShiftDifferential, Amount = nightShiftDiff });
            }

            _context.Earnings.AddRange(earnings);

            // 🔹 Save Deductions
            _context.Deductions.AddRange(
                new Deduction { PayrollId = payroll.PayrollId, Type = DeductionType.SSS, Amount = sss },
                new Deduction { PayrollId = payroll.PayrollId, Type = DeductionType.PhilHealth, Amount = phil },
                new Deduction { PayrollId = payroll.PayrollId, Type = DeductionType.PagIBIG, Amount = pagibig },
                new Deduction { PayrollId = payroll.PayrollId, Type = DeductionType.Tax, Amount = tax }
            );

            await _context.SaveChangesAsync();

            return payroll;
        }
    }
}