using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using itpayroll.Areas.Identity.Data;

namespace itpayroll.Models
{
    public class LeaveRequest
    {
        [Key]
        public int LeaveRequestId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee Employee { get; set; } = null!;

        [Required]
        public LeaveType LeaveType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Range(0.5, 100)]
        public double DaysRequested { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required]
        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        public string? ApprovedById { get; set; }

        [ForeignKey(nameof(ApprovedById))]
        public ApplicationUser? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }
    }

    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public enum LeaveType
    {
        SickLeave,
        VacationLeave,
        EmergencyLeave,
        MaternityLeave,
        PaternityLeave,
        BereavementLeave,
        UnpaidLeave
    }
}
