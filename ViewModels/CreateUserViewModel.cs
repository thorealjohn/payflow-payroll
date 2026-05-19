using System.ComponentModel.DataAnnotations;

namespace itpayroll.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(10, ErrorMessage = "Password must be at least 10 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Employee Number")]
        public string? EmployeeNumber { get; set; }

        [Display(Name = "Basic Salary")]
        [DataType(DataType.Currency)]
        [Range(0, 1000000, ErrorMessage = "Invalid salary amount.")]
        public decimal? BasicSalary { get; set; }
    }
}
