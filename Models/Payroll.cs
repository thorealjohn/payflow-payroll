using itpayroll.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Models
{
    public class Payroll : IValidatableObject
    {
        [Key]
        public int PayrollId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee Employee { get; set; } = null!;

        [Required]
        public DateTime PeriodStart { get; set; }

        [Required]
        public DateTime PeriodEnd { get; set; }

        [Range(0, 1000000)]
        public decimal GrossPay { get; set; }

        [Range(0, 1000000)]
        public decimal TotalDeductions { get; set; }

        [Range(0, 1000000)]
        public decimal NetPay { get; set; }

        [Required]
        public PayrollStatus Status { get; set; }

        // AUDIT
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? ModifiedAt { get; set; }

        [StringLength(450)]
        public string ModifiedBy { get; set; } = string.Empty;

        // APPROVAL WORKFLOW
        [StringLength(450)]
        public string? ProcessedById { get; set; }

        [ForeignKey(nameof(ProcessedById))]
        public ApplicationUser? ProcessedBy { get; set; }

        [StringLength(450)]
        public string? ApprovedById { get; set; }

        [ForeignKey(nameof(ApprovedById))]
        public ApplicationUser? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        // CONCURRENCY
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // VALIDATION
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (PeriodEnd < PeriodStart)
            {
                yield return new ValidationResult(
                    "Invalid payroll period",
                    new[] { nameof(PeriodEnd) });
            }

            if (NetPay < 0)
            {
                yield return new ValidationResult(
                    "Net pay cannot be negative",
                    new[] { nameof(NetPay) });
            }

            if (GrossPay < TotalDeductions)
            {
                yield return new ValidationResult(
                    "Deductions cannot exceed gross pay",
                    new[] { nameof(TotalDeductions) });
            }
        }
    }

    public enum PayrollStatus
    {
        Draft,
        Processed,
        PendingApproval,
        Approved,
        Released
    }
}