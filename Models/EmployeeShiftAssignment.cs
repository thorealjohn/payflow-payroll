using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Models
{
    public class EmployeeShiftAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int ShiftId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime DateFrom { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? DateTo { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [ForeignKey("ShiftId")]
        public Shift? Shift { get; set; }
    }
}
