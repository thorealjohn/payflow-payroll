using itpayroll.Areas.Identity.Data;
using itpayroll.Data;
using itpayroll.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace itpayroll.Services
{
    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            AuditAction action,
            string? entity = null,
            LogType logType = LogType.System,
            string? resource = null,
            string? targetId = null,
            string? metadata = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User == null ? null : await _userManager.GetUserAsync(httpContext.User);
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

            var audit = new AuditLog
            {
                UserId = user?.Id ?? "SYSTEM",
                UserEmail = user?.Email,
                Action = action,
                Entity = entity,
                Resource = resource,
                TargetId = targetId,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = userAgent,
                Browser = UserAgentParser.GetBrowser(userAgent),
                OperatingSystem = UserAgentParser.GetOperatingSystem(userAgent),
                RequestId = httpContext?.TraceIdentifier,
                SessionId = httpContext?.Session?.Id,
                Metadata = metadata,
                Timestamp = DateTime.UtcNow,
                LogType = logType
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task PruneAsync(int retentionDays)
        {
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var oldLogs = _context.AuditLogs.Where(log => log.Timestamp < cutoff);
            _context.AuditLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();
        }
    }
}
