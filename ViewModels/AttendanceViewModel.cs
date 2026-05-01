using System.ComponentModel.DataAnnotations;

namespace itpayroll.ViewModels
{
    public class AttendanceViewModel
    {
        public int? AttendanceId { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Time In")]
        [DataType(DataType.Time)]
        public TimeSpan TimeIn { get; set; }

        [Required]
        [Display(Name = "Time Out")]
        [DataType(DataType.Time)]
        public TimeSpan TimeOut { get; set; }

        [Display(Name = "Overtime Hours")]
        [Range(0, 24)]
        public double OvertimeHours { get; set; }
    }
}
