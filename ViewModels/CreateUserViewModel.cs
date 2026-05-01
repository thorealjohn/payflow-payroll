using itpayroll.Constant;
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

    public static class RoleHierarchy
    {
        private static readonly Dictionary<string, string[]> _allowedRoles = new()
        {
            { Roles.SuperAdmin, new[] { Roles.Admin } },
            { Roles.Admin, new[] { Roles.HR } },
            { Roles.HR, new[] { Roles.Employee } }
        };

        public static string[] GetAllowedRoles(string creatorRole)
        {
            return _allowedRoles.TryGetValue(creatorRole, out var roles) ? roles : Array.Empty<string>();
        }

        public static bool CanAssignRole(string creatorRole, string targetRole)
        {
            return GetAllowedRoles(creatorRole).Contains(targetRole);
        }

        public static bool IsEmployeeCreation(string creatorRole)
        {
            return creatorRole == Roles.HR;
        }
    }
}
