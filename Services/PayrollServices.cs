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

        private decimal ComputeOT(decimal salary, double ot)
            => HourlyRate(salary) * (decimal)ot * 1.25m;

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

            // 🔹 Get attendance
            var (hours, ot) = await _attendance.GetHoursAsync(employeeId, start, end);

            // 🔹 Earnings
            var basic = ComputeBasic(employee.BasicSalary, hours);
            var overtime = ComputeOT(employee.BasicSalary, ot);

            var gross = basic + overtime;

            // 🔹 Government deductions
            var sss = _gov.ComputeSSS(employee.BasicSalary);
            var phil = _gov.ComputePhilHealth(employee.BasicSalary);
            var pagibig = _gov.ComputePagIBIG(employee.BasicSalary);

            var govTotal = sss + phil + pagibig;

            // 🔹 Tax
            var taxable = gross - govTotal;
            var tax = _tax.ComputeTax(taxable);

            var totalDed = govTotal + tax;
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

            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync();

            // 🔹 Save Earnings
            _context.Earnings.AddRange(
                new Earning { PayrollId = payroll.PayrollId, Type = EarningType.BasicPay, Amount = basic },
                new Earning { PayrollId = payroll.PayrollId, Type = EarningType.Overtime, Amount = overtime }
            );

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