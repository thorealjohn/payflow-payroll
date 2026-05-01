using itpayroll.Models;
using System.ComponentModel.DataAnnotations;

namespace itpayroll.ViewModels
{
    public class PayrollProcessViewModel
    {
        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Period Start")]
        public DateTime PeriodStart { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Period End")]
        public DateTime PeriodEnd { get; set; }
    }

    public class PayrollDetailViewModel
    {
        public Payroll Payroll { get; set; } = null!;
        public List<Earning> Earnings { get; set; } = new();
        public List<Deduction> Deductions { get; set; } = new();
        public Employee Employee { get; set; } = null!;
    }
}
