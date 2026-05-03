using System.ComponentModel.DataAnnotations;

namespace itpayroll.Models
{
    public class SSSContribution
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Min Salary")]
        public decimal MinSalary { get; set; }

        [Required]
        [Display(Name = "Max Salary")]
        public decimal MaxSalary { get; set; }

        [Required]
        [Display(Name = "Employee Share")]
        public decimal EmployeeShare { get; set; }

        [Required]
        [Display(Name = "Employer Share")]
        public decimal EmployerShare { get; set; }

        [Display(Name = "Year")]
        public int Year { get; set; } = 2024;
    }
}
