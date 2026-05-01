using itpayroll.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itpayroll.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string? UserEmail { get; set; }

        [Required]
        public AuditAction Action { get; set; }

        [MaxLength(100)]
        public string? Entity { get; set; }

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public enum AuditAction
    {
        Login,
        Logout,
        Create,
        Update,
        Delete,
        PayrollProcess,
        FailedLogin
    }
}