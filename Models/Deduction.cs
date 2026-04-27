using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Models
{
    public class Deduction
    {
        [Key]
        public int DeductionId { get; set; }

        [Required]
        public int PayrollId { get; set; }

        [ForeignKey(nameof(PayrollId))]
        public Payroll Payroll { get; set; } = null!;

        [Required]
        public DeductionType Type { get; set; }

        [Range(0, 1000000)]
        public decimal Amount { get; set; }

        // AUDIT
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public enum DeductionType
    {
        Tax,
        SSS,
        PhilHealth,
        PagIBIG,
        Loan,
        Other
    }
}