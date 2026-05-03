using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Models
{
    public class Overtime
    {
        [Key]
        public int OvertimeId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public double Hours { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required]
        public OvertimeStatus Status { get; set; } = OvertimeStatus.Pending;

        [StringLength(450)]
        public string? ApprovedBy { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum OvertimeStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
