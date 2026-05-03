using itpayroll.Models;
using System.ComponentModel.DataAnnotations;

namespace itpayroll.ViewModels
{
    public class LeaveRequestViewModel
    {
        public int LeaveRequestId { get; set; }

        [Display(Name = "Employee")]
        public string EmployeeName { get; set; } = string.Empty;

        public int EmployeeId { get; set; }

        [Display(Name = "Leave Type")]
        public LeaveType LeaveType { get; set; }

        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Days Requested")]
        public double DaysRequested { get; set; }

        public string? Reason { get; set; }

        [Display(Name = "Status")]
        public LeaveStatus Status { get; set; }

        [Display(Name = "Approved By")]
        public string? ApprovedByName { get; set; }

        [Display(Name = "Approved Date")]
        public DateTime? ApprovedDate { get; set; }

        public string? RejectionReason { get; set; }

        [Display(Name = "Requested On")]
        public DateTime CreatedDate { get; set; }
    }
}
