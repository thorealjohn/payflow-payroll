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
        public string? Resource { get; set; }

        [MaxLength(100)]
        public string? TargetId { get; set; }

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(100)]
        public string? Browser { get; set; }

        [MaxLength(100)]
        public string? OperatingSystem { get; set; }

        [MaxLength(100)]
        public string? RequestId { get; set; }

        [MaxLength(100)]
        public string? SessionId { get; set; }

        [MaxLength(2000)]
        public string? Metadata { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public LogType LogType { get; set; } = LogType.System;
    }

    public enum LogType
    {
        Security,
        System
    }

    public enum AuditAction
    {
        Login,
        Logout,
        Create,
        Update,
        Delete,
        PayrollProcess,
        FailedLogin,
        PasswordReset
    }
}

// Add LogType property to AuditLog class
// I'll add it in the next edit
