using itpayroll.Models;
using System.ComponentModel.DataAnnotations;

namespace itpayroll.ViewModels
{
    public class EmployeeViewModel
    {
        public int? EmployeeId { get; set; }

        [Required]
        [Display(Name = "First Name")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Only letters, spaces, hyphens, and apostrophes allowed")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Only letters, spaces, hyphens, and apostrophes allowed")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Email (auto-generated)")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Employee Number")]
        [MaxLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Status")]
        public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;

        [Required]
        [Range(0, 1000000)]
        [DataType(DataType.Currency)]
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Hire Date")]
        public DateTime HireDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Termination Date")]
        public DateTime? TerminationDate { get; set; }

        [Display(Name = "Shift")]
        public int? ShiftId { get; set; }

        // Employment Details
        [Display(Name = "Department")]
        [MaxLength(100)]
        public string? Department { get; set; }

        [Display(Name = "Position")]
        [MaxLength(100)]
        public string? Position { get; set; }

        [Display(Name = "Employment Type")]
        public EmploymentType? EmploymentType { get; set; }

        // Payroll Information
        [Display(Name = "Salary Type")]
        public SalaryType SalaryType { get; set; } = SalaryType.Monthly;

        [Display(Name = "Pay Frequency")]
        public PayFrequency PayFrequency { get; set; } = PayFrequency.Monthly;

        [Display(Name = "Bank Name")]
        [MaxLength(100)]
        public string? BankName { get; set; }

        [Display(Name = "Bank Account Number")]
        [MaxLength(50)]
        public string? BankAccountNumber { get; set; }

        [Display(Name = "TIN")]
        [MaxLength(50)]
        public string? TIN { get; set; }

        [Display(Name = "SSS Number")]
        [MaxLength(50)]
        public string? SSSNumber { get; set; }

        [Display(Name = "PhilHealth Number")]
        [MaxLength(50)]
        public string? PhilHealthNumber { get; set; }

        [Display(Name = "Pag-IBIG Number")]
        [MaxLength(50)]
        public string? PagIBIGNumber { get; set; }
    }
}
