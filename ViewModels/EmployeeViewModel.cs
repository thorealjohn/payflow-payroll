using itpayroll.Areas.Identity.Data;
using itpayroll.Models;
using System.ComponentModel.DataAnnotations;

namespace itpayroll.ViewModels
{
    public class EmployeeViewModel : IValidatableObject
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

        [Display(Name = "Middle Name")]
        [MaxLength(50)]
        public string? MiddleName { get; set; }

        [Display(Name = "Suffix")]
        [MaxLength(10)]
        public string? Suffix { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public Gender? Gender { get; set; }

        [Display(Name = "Civil Status")]
        public CivilStatus? CivilStatus { get; set; }

        [Display(Name = "Nationality")]
        [MaxLength(50)]
        public string Nationality { get; set; } = "Filipino";

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Alternate Phone")]
        [MaxLength(20)]
        public string? AlternatePhone { get; set; }

        [Display(Name = "Address Street")]
        [MaxLength(500)]
        public string? AddressStreet { get; set; }

        [Display(Name = "Barangay")]
        [MaxLength(100)]
        public string? AddressBarangay { get; set; }

        [Display(Name = "City")]
        [MaxLength(100)]
        public string? AddressCity { get; set; }

        [Display(Name = "Province")]
        [MaxLength(100)]
        public string? AddressProvince { get; set; }

        [Display(Name = "Zip Code")]
        [MaxLength(20)]
        public string? AddressZipCode { get; set; }

        [Display(Name = "Emergency Contact Name")]
        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [Display(Name = "Relationship")]
        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        [Display(Name = "Emergency Phone")]
        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [Display(Name = "Email (auto-generated)")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Employee Number")]
        [MaxLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = Constant.Roles.Employee;

        [Required]
        [Display(Name = "Status")]
        public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;

        [DataType(DataType.Currency)]
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Hire Date")]
        public DateTime HireDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Termination Date")]
        public DateTime? TerminationDate { get; set; }

        [Display(Name = "Shift")]
        public int? ShiftId { get; set; }

        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [Display(Name = "Position")]
        public int? PositionId { get; set; }

        [Display(Name = "Employment Type")]
        public EmploymentType? EmploymentType { get; set; }

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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DepartmentId == null)
            {
                yield return new ValidationResult("Department is required.", new[] { nameof(DepartmentId) });
            }

            if (PositionId == null)
            {
                yield return new ValidationResult("Position is required.", new[] { nameof(PositionId) });
            }

            if (ShiftId == null)
            {
                yield return new ValidationResult("Shift is required.", new[] { nameof(ShiftId) });
            }

            if (BasicSalary <= 0)
            {
                yield return new ValidationResult(
                    "Basic salary is required and must be greater than zero.",
                    new[] { nameof(BasicSalary) });
            }

            if (EmploymentType == null)
            {
                yield return new ValidationResult("Employment type is required.", new[] { nameof(EmploymentType) });
            }

            if (TerminationDate.HasValue && TerminationDate < HireDate)
            {
                yield return new ValidationResult(
                    "Termination date cannot be earlier than hire date.",
                    new[] { nameof(TerminationDate) });
            }

            if (HireDate.Date > DateTime.Today)
            {
                yield return new ValidationResult(
                    "Hire date cannot be in the future.",
                    new[] { nameof(HireDate) });
            }
        }
    }
}
