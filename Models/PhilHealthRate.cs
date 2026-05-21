using System.ComponentModel.DataAnnotations;

namespace itpayroll.Models
{
    public class PhilHealthRate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Min Salary")]
        public decimal MinSalary { get; set; }

        [Display(Name = "Max Salary")]
        public decimal? MaxSalary { get; set; }

        [Required]
        [Display(Name = "Premium Rate")]
        [Range(0, 1)]
        public decimal Rate { get; set; } = 0.05m;

        [Required]
        [Display(Name = "Employee Share (%)")]
        [Range(0, 1)]
        public decimal EmployeeSharePercentage { get; set; } = 0.50m;

        [Display(Name = "Year")]
        public int Year { get; set; } = DateTime.UtcNow.Year;
    }
}
