using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Models
{
    public class Attendance : IValidatableObject
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee Employee { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan TimeIn { get; set; }

        [Required]
        public TimeSpan TimeOut { get; set; }

        [Range(0, 24)]
        public double TotalHours { get; set; }

        [Range(0, 24)]
        public double OvertimeHours { get; set; }

        // AUDIT
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // VALIDATION
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (TimeOut <= TimeIn)
            {
                yield return new ValidationResult(
                    "TimeOut must be after TimeIn",
                    new[] { nameof(TimeOut) });
            }

            var computedHours = (TimeOut - TimeIn).TotalHours;

            if (TotalHours > 24 || TotalHours < 0)
            {
                yield return new ValidationResult(
                    "Invalid total hours",
                    new[] { nameof(TotalHours) });
            }

            if (computedHours < 0)
            {
                yield return new ValidationResult(
                    "Computed hours invalid",
                    new[] { nameof(TimeOut) });
            }
        }
    }
}