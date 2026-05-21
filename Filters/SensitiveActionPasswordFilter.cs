using itpayroll.Areas.Identity.Data;
using itpayroll.Constant;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace itpayroll.Filters
{
    public class SensitiveActionPasswordFilter : IAsyncActionFilter
    {
        private static readonly string[] SensitiveActionWords =
        {
            "Edit",
            "Delete",
            "SoftDelete",
            "Release",
            "Void",
            "Approve",
            "Reject",
            "Process",
            "ResetPassword"
        };

        private readonly UserManager<ApplicationUser> _userManager;

        public SensitiveActionPasswordFilter(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            if (!HttpMethods.IsPost(request.Method))
            {
                await next();
                return;
            }

            var actionName = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
            if (!SensitiveActionWords.Any(word => actionName.Contains(word, StringComparison.OrdinalIgnoreCase)))
            {
                await next();
                return;
            }

            var userPrincipal = context.HttpContext.User;
            if (!(userPrincipal.IsInRole(Roles.SuperAdmin) || userPrincipal.IsInRole(Roles.Admin) || userPrincipal.IsInRole(Roles.HR)))
            {
                await next();
                return;
            }

            var user = await _userManager.GetUserAsync(userPrincipal);
            var password = request.HasFormContentType ? request.Form["SecurityPassword"].ToString() : null;

            if (user == null || string.IsNullOrWhiteSpace(password) || !await _userManager.CheckPasswordAsync(user, password))
            {
                if (context.Controller is Controller controller)
                {
                    controller.TempData["Error"] = "Password validation failed. Please re-enter your password before continuing.";
                }

                var referer = request.Headers.Referer.ToString();
                context.Result = !string.IsNullOrWhiteSpace(referer)
                    ? new RedirectResult(referer)
                    : new RedirectToActionResult("Index", "Dashboard", null);
                return;
            }

            await next();
        }
    }
}
