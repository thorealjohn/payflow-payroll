using itpayroll.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace itpayroll.ViewModels
{
    public class OvertimeViewModel
    {
        public int? OvertimeId { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public double Hours { get; set; }

        [StringLength(500)]
        [Display(Name = "Reason")]
        public string? Reason { get; set; }

        [Display(Name = "Status")]
        public OvertimeStatus Status { get; set; }

        [Display(Name = "Approved By")]
        public string? ApprovedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }
    }
}
