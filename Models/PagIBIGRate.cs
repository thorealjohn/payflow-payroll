using System.ComponentModel.DataAnnotations;

namespace itpayroll.Models
{
    public class PagIBIGRate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Min Salary")]
        public decimal MinSalary { get; set; }

        [Display(Name = "Max Salary")]
        public decimal? MaxSalary { get; set; }

        [Required]
        [Display(Name = "Employee Rate")]
        [Range(0, 1)]
        public decimal EmployeeRate { get; set; } = 0.02m;

        [Display(Name = "Employer Rate")]
        [Range(0, 1)]
        public decimal EmployerRate { get; set; } = 0.02m;

        [Required]
        [Display(Name = "Max Contribution")]
        public decimal MaxContribution { get; set; } = 100;

        [Display(Name = "Year")]
        public int Year { get; set; } = DateTime.UtcNow.Year;
    }
}
