using itpayroll.Data;
using itpayroll.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace itpayroll.Filters
{
    public class AuditLoggingActionFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public AuditLoggingActionFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var result = await next();

            if (result.Exception != null || result.Result == null)
                return;

            var userId = context.HttpContext.User?.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return;

            var action = GetAuditAction(context.HttpContext.Request.Method);
            var controller = context.RouteData.Values["controller"]?.ToString();
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Entity = controller,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow,
                LogType = GetLogType(action)
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        private AuditAction GetAuditAction(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "POST" => AuditAction.Create,
                "PUT" => AuditAction.Update,
                "DELETE" => AuditAction.Delete,
                _ => AuditAction.Update
            };
        }

        private LogType GetLogType(AuditAction action)
        {
            return action switch
            {
                AuditAction.Login => LogType.Security,
                AuditAction.Logout => LogType.Security,
                AuditAction.FailedLogin => LogType.Security,
                _ => LogType.System
            };
        }
    }
}
