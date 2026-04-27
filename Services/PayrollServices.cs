using itpayroll.Data;
using itpayroll.Models;

namespace itpayroll.Services
{
    public class PayrollService
    {
        private readonly ApplicationDbContext _context;
        private readonly GovernmentService _gov;
        private readonly TaxService _tax;

        public PayrollService(
            ApplicationDbContext context,
            GovernmentService gov,
            TaxService tax)
        {
            _context = context;
            _gov = gov;
            _tax = tax;
        }

        // =========================
        // CORE CALCULATIONS
        // =========================

        public decimal ComputeHourlyRate(decimal monthlySalary)
        {
            return monthlySalary / 22 / 8;
        }

        public decimal ComputeBasicPay(decimal monthlySalary, double hoursWorked)
        {
            var rate = ComputeHourlyRate(monthlySalary);
            return rate * (decimal)hoursWorked;
        }

        public decimal ComputeOvertimePay(decimal monthlySalary, double overtimeHours)
        {
            var rate = ComputeHourlyRate(monthlySalary);
            return rate * (decimal)overtimeHours * 1.25m;
        }

        // =========================
        // MAIN PAYROLL LOGIC
        // =========================

        public (decimal gross, decimal deductions, decimal net) ComputePayroll(
            decimal monthlySalary,
            double hoursWorked,
            double overtimeHours)
        {
            var basicPay = ComputeBasicPay(monthlySalary, hoursWorked);
            var overtimePay = ComputeOvertimePay(monthlySalary, overtimeHours);

            var gross = basicPay + overtimePay;

            var gov = _gov.ComputeTotalGovernment(monthlySalary);

            var taxable = gross - gov;

            var tax = _tax.ComputeTax(taxable);

            var totalDeductions = gov + tax;

            var net = gross - totalDeductions;

            return (gross, totalDeductions, net);
        }

        // =========================
        // DATABASE PROCESSING
        // =========================

        public async Task<Payroll> ProcessPayrollAsync(
            int employeeId,
            double hoursWorked,
            double overtimeHours)
        {
            var employee = await _context.Employees.FindAsync(employeeId);

            if (employee == null)
                throw new Exception("Employee not found");

            var result = ComputePayroll(
                employee.BasicSalary,
                hoursWorked,
                overtimeHours);

            var payroll = new Payroll
            {
                EmployeeId = employeeId,
                PeriodStart = DateTime.UtcNow.AddDays(-15),
                PeriodEnd = DateTime.UtcNow,
                GrossPay = result.gross,
                TotalDeductions = result.deductions,
                NetPay = result.net,
                Status = PayrollStatus.Processed
            };

            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync();

            return payroll;
        }
    }
}