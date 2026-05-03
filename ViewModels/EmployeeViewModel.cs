using itpayroll.Models;
using System.ComponentModel.DataAnnotations;

namespace itpayroll.ViewModels
{
    public class EmployeeViewModel
    {
        public int? EmployeeId { get; set; }

        [Required]
        [Display(Name = "User")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Employee Number")]
        [MaxLength(20)]
        [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Only uppercase letters, numbers, and hyphens allowed")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;

        [Required]
        [Range(0, 1000000)]
        [DataType(DataType.Currency)]
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Hire Date")]
        public DateTime HireDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Termination Date")]
        public DateTime? TerminationDate { get; set; }

        [Display(Name = "Shift")]
        public int? ShiftId { get; set; }
    }
}
