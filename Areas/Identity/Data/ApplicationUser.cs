using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Areas.Identity.Data
{
    public class ApplicationUser : IdentityUser
    {
        
        // BASIC INFO
        

        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z\s\-']+$")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z\s\-']+$")]
        public string LastName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        
        // ACCOUNT STATUS
        

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false; // soft delete

        
        // AUDIT FIELDS
        

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(450)]
        public string ModifiedBy { get; set; } = string.Empty;

        
        // SECURITY / LOGIN TRACKING
        

        public DateTime? LastLoginDate { get; set; }

        
        // CONCURRENCY CONTROL
        

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}


   