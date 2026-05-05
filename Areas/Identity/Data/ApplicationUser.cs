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

        [MaxLength(50)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z\s\-']+$")]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Suffix { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Gender? Gender { get; set; }

        public CivilStatus? CivilStatus { get; set; }

        [MaxLength(50)]
        public string Nationality { get; set; } = "Filipino";

        [MaxLength(500)]
        public string? ProfilePicturePath { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {MiddleName} {LastName} {Suffix}".Replace("  ", " ").Trim();

        // CONTACT INFORMATION

        [MaxLength(500)]
        public string? AddressStreet { get; set; }

        [MaxLength(100)]
        public string? AddressBarangay { get; set; }

        [MaxLength(100)]
        public string? AddressCity { get; set; }

        [MaxLength(100)]
        public string? AddressProvince { get; set; }

        [MaxLength(20)]
        public string? AddressZipCode { get; set; }

        [MaxLength(20)]
        public string? AlternatePhone { get; set; }

        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelationship { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        // ACCOUNT STATUS

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false; // soft delete

        // SECURITY / LOGIN TRACKING

        public bool MustChangePassword { get; set; } = true;

        public DateTime? LastLoginDate { get; set; }

        public DateTime? PasswordLastChanged { get; set; }

        // AUDIT FIELDS

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(450)]
        public string ModifiedBy { get; set; } = string.Empty;

        // CONCURRENCY CONTROL

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }

    public enum Gender
    {
        Male,
        Female,
        Other
    }

    public enum CivilStatus
    {
        Single,
        Married,
        Divorced,
        Widowed
    }
}
