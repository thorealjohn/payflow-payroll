using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Models
{
    public class Earning
    {
        [Key]
        public int EarningId { get; set; }

        [Required]
        public int PayrollId { get; set; }

        [ForeignKey(nameof(PayrollId))]
        public Payroll Payroll { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public EarningType Type { get; set; }

        [Range(0, 1000000)]
        public decimal Amount { get; set; }

        // AUDIT
        public DateTime CreatedAt{ get; set; } = DateTime.UtcNow;
    }

    public enum EarningType
    {
        BasicPay,
        Overtime,
        Bonus,
        Allowance
    }
}