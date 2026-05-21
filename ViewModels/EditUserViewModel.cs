using System.ComponentModel.DataAnnotations;

namespace itpayroll.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Only letters, spaces, hyphens, and apostrophes allowed")]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Only letters, spaces, hyphens, and apostrophes allowed")]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string CurrentRole { get; set; } = string.Empty;
    }
}
