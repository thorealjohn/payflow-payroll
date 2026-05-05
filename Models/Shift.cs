using System.ComponentModel.DataAnnotations;

namespace itpayroll.Models
{
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ShiftName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Grace Period (minutes)")]
        [Range(0, 60)]
        public int GracePeriodMinutes { get; set; } = 10;

        [Display(Name = "Is Night Shift")]
        public bool IsNightShift { get; set; }

        [Display(Name = "Is Overnight Shift")]
        public bool IsOvernight { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Description")]
        [MaxLength(200)]
        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
