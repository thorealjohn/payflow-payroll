using System.ComponentModel.DataAnnotations;
using itpayroll.Models;

namespace itpayroll.ViewModels
{
    public class CreateLeaveRequestViewModel
    {
        [Required]
        [Display(Name = "Leave Type")]
        public LeaveType LeaveType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Required]
        [Range(0.5, 100)]
        [Display(Name = "Days Requested")]
        public double DaysRequested { get; set; }

        [StringLength(500)]
        [Display(Name = "Reason")]
        public string? Reason { get; set; }
    }
}
