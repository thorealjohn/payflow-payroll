using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Models
{
    public enum DayType
    {
        Regular,
        RestDay,
        Holiday,
        RestDayHoliday
    }

    public class Attendance : IValidatableObject
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee Employee { get; set; } = null!;

        public int? ShiftId { get; set; }

        [ForeignKey(nameof(ShiftId))]
        public Shift? Shift { get; set; }

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

        [Range(0, 1440)]
        public int LateMinutes { get; set; }

        [Range(0, 1440)]
        public int UndertimeMinutes { get; set; }

        [Range(0, 24)]
        public double NightShiftHours { get; set; }

        public DayType DayType { get; set; } = DayType.Regular;

        // AUDIT
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

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