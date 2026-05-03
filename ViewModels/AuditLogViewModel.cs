using itpayroll.Models;

namespace itpayroll.ViewModels
{
    public class AuditLogViewModel
    {
        public int Id { get; set; }
        public string? UserEmail { get; set; }
        public AuditAction Action { get; set; }
        public string? Entity { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public LogType LogType { get; set; }
    }
}
