using itpayroll.Models;

namespace itpayroll.ViewModels
{
    public class AuditLogViewModel
    {
        public int Id { get; set; }
        public string? UserEmail { get; set; }
        public AuditAction Action { get; set; }
        public string? Entity { get; set; }
        public string? Resource { get; set; }
        public string? TargetId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Browser { get; set; }
        public string? OperatingSystem { get; set; }
        public string? RequestId { get; set; }
        public string? SessionId { get; set; }
        public string? Metadata { get; set; }
        public DateTime Timestamp { get; set; }
        public LogType LogType { get; set; }
    }
}
