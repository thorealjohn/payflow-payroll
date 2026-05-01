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
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string CurrentRole { get; set; } = string.Empty;
    }
}
