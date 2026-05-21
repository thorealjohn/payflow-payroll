using itpayroll.Data;
using itpayroll.Models;
using itpayroll.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace itpayroll.Filters
{
    public class AuditLoggingActionFilter : IAsyncActionFilter
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public AuditLoggingActionFilter(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var method = context.HttpContext.Request.Method;
            if (!IsMutation(method))
            {
                await next();
                return;
            }

            var result = await next();

            if (result.Exception != null || result.Result == null)
                return;

            var userId = context.HttpContext.User?.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return;

            var controller = context.RouteData.Values["controller"]?.ToString();
            var routeAction = context.RouteData.Values["action"]?.ToString();
            var action = GetAuditAction(method, routeAction);
            var targetId = ResolveTargetId(context);
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.HttpContext.Request.Headers.UserAgent.ToString();
            var metadata = BuildMetadata(context);

            // Create ISOLATED context for audit (prevents concurrency bug)
            using (var auditContext = await _contextFactory.CreateDbContextAsync())
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Entity = controller,
                    Resource = routeAction,
                    TargetId = targetId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Browser = UserAgentParser.GetBrowser(userAgent),
                    OperatingSystem = UserAgentParser.GetOperatingSystem(userAgent),
                    RequestId = context.HttpContext.TraceIdentifier,
                    SessionId = context.HttpContext.Session?.Id,
                    Metadata = metadata,
                    Timestamp = DateTime.UtcNow,
                    LogType = GetLogType(action)
                };

                auditContext.AuditLogs.Add(auditLog);
                await auditContext.SaveChangesAsync(); // Safe: separate context
            }
        }

        private static string? BuildMetadata(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.HasFormContentType)
                return null;

            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "__RequestVerificationToken",
                "Password",
                "SecurityPassword",
                "OldPassword",
                "NewPassword",
                "ConfirmPassword",
                "FirstName",
                "LastName",
                "MiddleName",
                "PhoneNumber",
                "AlternatePhone",
                "BankName",
                "BankAccountNumber",
                "BasicSalary",
                "TIN",
                "SSSNumber",
                "PhilHealthNumber",
                "PagIBIGNumber",
                "AddressStreet",
                "AddressBarangay",
                "AddressCity",
                "AddressProvince",
                "AddressZipCode",
                "EmergencyContactName",
                "EmergencyContactRelationship",
                "EmergencyContactPhone"
            };

            var values = context.HttpContext.Request.Form
                .Where(item => !excluded.Contains(item.Key))
                .ToDictionary(item => item.Key, item => item.Value.ToString());

            if (values.Count == 0)
                return null;

            var json = JsonSerializer.Serialize(values);
            return json.Length > 2000 ? json[..2000] : json;
        }

        private static string? ResolveTargetId(ActionExecutingContext context)
        {
            var routeId = context.RouteData.Values["id"]?.ToString();
            if (!string.IsNullOrWhiteSpace(routeId))
                return routeId;

            if (!context.HttpContext.Request.HasFormContentType)
                return null;

            var idField = context.HttpContext.Request.Form
                .FirstOrDefault(item => item.Key.Equals("id", StringComparison.OrdinalIgnoreCase) || item.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(idField.Key) ? null : idField.Value.ToString();
        }

        private static bool IsMutation(string method)
        {
            return HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method);
        }

        private AuditAction GetAuditAction(string method, string? routeAction)
        {
            if (!string.IsNullOrWhiteSpace(routeAction))
            {
                if (routeAction.Contains("Delete", StringComparison.OrdinalIgnoreCase) || routeAction.Contains("Void", StringComparison.OrdinalIgnoreCase))
                    return AuditAction.Delete;
                if (routeAction.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
                    routeAction.Contains("Approve", StringComparison.OrdinalIgnoreCase) ||
                    routeAction.Contains("Reject", StringComparison.OrdinalIgnoreCase) ||
                    routeAction.Contains("Release", StringComparison.OrdinalIgnoreCase))
                    return AuditAction.Update;
                if (routeAction.Contains("Process", StringComparison.OrdinalIgnoreCase))
                    return AuditAction.PayrollProcess;
            }

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
