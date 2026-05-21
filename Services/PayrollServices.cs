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

        private decimal HourlyRate(Employee employee)
            => employee.SalaryType switch
            {
                SalaryType.Daily => employee.BasicSalary / 8,
                SalaryType.Hourly => employee.BasicSalary,
                _ => employee.BasicSalary / 22 / 8
            };

        private decimal ComputeOT(Employee employee, double ot, DayType dayType)
        {
            var premiumRate = dayType switch
            {
                DayType.Regular => 0.25m,
                DayType.RestDay or DayType.Holiday => 0.30m,
                DayType.RestDayHoliday => 0.50m,
                _ => 0.25m
            };
            return HourlyRate(employee) * (decimal)ot * premiumRate;
        }

        private decimal ComputeNightShiftDifferential(Employee employee, double nightShiftHours)
            => HourlyRate(employee) * (decimal)nightShiftHours * 0.10m;

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
            DateTime end,
            string processedByUserId = "")
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

            // 🔹 Build OT hours lookup per date
            var otHoursByDate = approvedOvertimes
                .GroupBy(o => o.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.Hours));

            // 🔹 Per-record earnings
            decimal basic = 0;
            decimal overtime = 0;
            double totalNightShiftHours = 0;

            foreach (var record in attendanceRecords)
            {
                var hourly = HourlyRate(employee);
                var date = record.Date.Date;
                totalNightShiftHours += record.NightShiftHours;

                if (record.DayType is DayType.RestDay or DayType.RestDayHoliday)
                {
                    // Rest day: all hours at 130% (no late/undertime deduction)
                    basic += hourly * (decimal)record.TotalHours * 1.30m;
                }
                else
                {
                    // Regular day: scheduled 480 min minus late/undertime
                    var paidMin = Math.Max(0, 480 - record.LateMinutes - record.UndertimeMinutes);
                    basic += hourly * (decimal)paidMin / 60m;
                    // Base rate for approved OT hours on this day
                    basic += hourly * (decimal)otHoursByDate.GetValueOrDefault(date, 0);
                }
            }

            foreach (var ot in approvedOvertimes)
            {
                var attendanceOnDate = attendanceRecords.FirstOrDefault(a => a.Date == ot.Date);
                var dayType = attendanceOnDate?.DayType ?? DayType.Regular;
                overtime += ComputeOT(employee, ot.Hours, dayType);
            }

            var nightShiftDiff = ComputeNightShiftDifferential(employee, totalNightShiftHours);

            var gross = basic + overtime + nightShiftDiff;

            // Statutory contributions and withholding use the same base as period gross
            // (earnings from this run), so deductions stay aligned with amounts earned.
            decimal contributionBase = gross;

            var sss = contributionBase > 0
                ? await _gov.ComputeSSS(contributionBase)
                : 0;
            var phil = contributionBase > 0
                ? await _gov.ComputePhilHealth(contributionBase)
                : 0;
            var pagibig = contributionBase > 0
                ? await _gov.ComputePagIBIG(contributionBase)
                : 0;

            var govTotal = sss + phil + pagibig;

            var taxable = gross - govTotal;
            var tax = await _tax.ComputeTax(taxable);

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
                Status = PayrollStatus.PendingApproval,
                ProcessedById = processedByUserId,
                CreatedBy = processedByUserId
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