using System.ComponentModel.DataAnnotations;
using itpayroll.Models;

namespace itpayroll.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Suffix")]
        public string? Suffix { get; set; }

        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "Civil Status")]
        public string? CivilStatus { get; set; }

        [Display(Name = "Nationality")]
        public string Nationality { get; set; } = "Filipino";

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Alternate Phone")]
        public string? AlternatePhone { get; set; }

        [Display(Name = "Address Street")]
        public string? AddressStreet { get; set; }

        [Display(Name = "Barangay")]
        public string? AddressBarangay { get; set; }

        [Display(Name = "City")]
        public string? AddressCity { get; set; }

        [Display(Name = "Province")]
        public string? AddressProvince { get; set; }

        [Display(Name = "Zip Code")]
        public string? AddressZipCode { get; set; }

        [Display(Name = "Emergency Contact Name")]
        public string? EmergencyContactName { get; set; }

        [Display(Name = "Relationship")]
        public string? EmergencyContactRelationship { get; set; }

        [Display(Name = "Emergency Phone")]
        public string? EmergencyContactPhone { get; set; }

        [Display(Name = "Employee Number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Position")]
        public string? Position { get; set; }

        [Display(Name = "Employment Type")]
        public EmploymentType? EmploymentType { get; set; }

        [Display(Name = "Basic Salary")]
        [DataType(DataType.Currency)]
        public decimal BasicSalary { get; set; }

        [Display(Name = "Salary Type")]
        public SalaryType SalaryType { get; set; }

        [Display(Name = "Pay Frequency")]
        public PayFrequency PayFrequency { get; set; }

        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [Display(Name = "Bank Account Number")]
        public string? BankAccountNumber { get; set; }

        [Display(Name = "TIN")]
        public string? TIN { get; set; }

        [Display(Name = "SSS Number")]
        public string? SSSNumber { get; set; }

        [Display(Name = "PhilHealth Number")]
        public string? PhilHealthNumber { get; set; }

        [Display(Name = "Pag-IBIG Number")]
        public string? PagIBIGNumber { get; set; }

        [Display(Name = "Hire Date")]
        public DateTime HireDate { get; set; }

        [Display(Name = "Shift")]
        public string ShiftName { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Profile Picture")]
        public string? ProfilePicturePath { get; set; }

        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

        [Display(Name = "Two-Factor Authentication")]
        public bool IsTwoFactorEnabled { get; set; }

        [Display(Name = "Recovery Codes Left")]
        public int RecoveryCodesLeft { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLoginDate { get; set; }

        [Display(Name = "Password Last Changed")]
        public DateTime? PasswordLastChanged { get; set; }

        [Display(Name = "Last IP Address")]
        public string? LastIpAddress { get; set; }

        public IReadOnlyList<AuditLog> RecentSecurityLogs { get; set; } = Array.Empty<AuditLog>();
    }
}
