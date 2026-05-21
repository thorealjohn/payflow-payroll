using System.ComponentModel.DataAnnotations;

namespace itpayroll.Models
{
    public enum PayrollFrequency
    {
        Weekly,
        SemiMonthly,
        Monthly
    }

    public class PayrollSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Payroll Frequency")]
        public PayrollFrequency Frequency { get; set; } = PayrollFrequency.SemiMonthly;

        [Required]
        [Display(Name = "Cutoff 1 Start Day")]
        [Range(1, 28)]
        public int Cutoff1StartDay { get; set; } = 1;

        [Required]
        [Display(Name = "Cutoff 1 End Day")]
        [Range(1, 28)]
        public int Cutoff1EndDay { get; set; } = 15;

        [Required]
        [Display(Name = "Cutoff 2 Start Day")]
        [Range(1, 28)]
        public int Cutoff2StartDay { get; set; } = 16;

        [Required]
        [Display(Name = "Cutoff 2 End Day")]
        [Range(1, 31)]
        public int Cutoff2EndDay { get; set; } = 30;

        public DateTime? UpdatedAt { get; set; }

        [StringLength(450)]
        public string? UpdatedBy { get; set; }
    }
}
