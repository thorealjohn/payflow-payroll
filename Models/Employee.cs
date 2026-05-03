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
}