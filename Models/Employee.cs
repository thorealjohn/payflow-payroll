using itpayroll.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Models
{
    public class Employee : IValidatableObject
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(20)]
        [RegularExpression(@"^[A-Z0-9\-]+$")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        public EmploymentStatus Status { get; set; }

        [Required]
        [Range(0, 1000000)]
        [DataType(DataType.Currency)]
        public decimal BasicSalary { get; set; }

        [Required]
        public DateTime HireDate { get; set; }

        public DateTime? TerminationDate { get; set; }

        [Display(Name = "Shift")]
        public int? ShiftId { get; set; }

        [ForeignKey(nameof(ShiftId))]
        public Shift? Shift { get; set; }

        // EMPLOYMENT DETAILS
        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        [Display(Name = "Position")]
        public int? PositionId { get; set; }

        [ForeignKey(nameof(PositionId))]
        public Position? Position { get; set; }

        public EmploymentType? EmploymentType { get; set; }

        // PAYROLL INFORMATION
        public SalaryType SalaryType { get; set; } = SalaryType.Monthly;

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? BankAccountNumber { get; set; }

        [MaxLength(15)]
        [RegularExpression(@"^\d{3}-\d{3}-\d{3}(-\d{3})?$", ErrorMessage = "TIN must be in format XXX-XXX-XXX or XXX-XXX-XXX-XXX")]
        public string? TIN { get; set; } // Tax Identification Number

        [MaxLength(12)]
        [RegularExpression(@"^\d{2}-\d{7}-\d$", ErrorMessage = "SSS must be in format XX-XXXXXXX-X")]
        public string? SSSNumber { get; set; }

        [MaxLength(14)]
        [RegularExpression(@"^\d{2}-\d{7,9}-\d$", ErrorMessage = "PhilHealth must be in format XX-XXXXXXX-X or XX-XXXXXXXXX-X")]
        public string? PhilHealthNumber { get; set; }

        [MaxLength(14)]
        [RegularExpression(@"^\d{4}-\d{4}-\d{4}$", ErrorMessage = "Pag-IBIG must be in format XXXX-XXXX-XXXX")]
        public string? PagIBIGNumber { get; set; }

        // AUDIT
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(450)]
        public string ModifiedBy { get; set; } = string.Empty;

        // CONCURRENCY
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // BUSINESS VALIDATION
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (TerminationDate.HasValue && TerminationDate < HireDate)
            {
                yield return new ValidationResult(
                    "Termination date cannot be earlier than hire date.",
                    new[] { nameof(TerminationDate) });
            }

            if (HireDate > DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "Hire date cannot be in the future.",
                    new[] { nameof(HireDate) });
            }
        }
    }

    public enum EmploymentStatus
    {
        Active,
        Inactive,
        Suspended
    }

    public enum EmploymentType
    {
        Regular,
        PartTime,
        Contractual,
        Probationary,
        Intern
    }

    public enum SalaryType
    {
        Monthly,
        Daily,
        Hourly
    }
}