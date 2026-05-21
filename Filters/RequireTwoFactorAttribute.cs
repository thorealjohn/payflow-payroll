using itpayroll.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace itpayroll.Filters
{
    public class RequireTwoFactorAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var path = context.HttpContext.Request.Path.Value?.ToLower();

            if (string.IsNullOrEmpty(path) ||
                path.Contains("login") ||
                path.Contains("logout") ||
                path.Contains("changepassword") ||
                path.Contains("enableauthenticator") ||
                path.Contains("twofactorauthentication") ||
                path.Contains("disable2fa") ||
                path.Contains("resetauthenticator") ||
                path.Contains("showrecoverycodes") ||
                path.Contains("generaterecoverycodes") ||
                path.Contains("loginwith2fa") ||
                path.Contains("loginwithrecoverycode") ||
                path.Contains("/lib/") ||
                path.Contains("/css/") ||
                path.Contains("/js/") ||
                path.Contains("/images/") ||
                path.StartsWith("/error"))
            {
                await next();
                return;
            }

            var userManager = context.HttpContext.RequestServices
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.GetUserAsync(context.HttpContext.User);

            if (user != null && !await userManager.GetTwoFactorEnabledAsync(user))
            {
                if (context.Controller is Controller controller)
                {
                    controller.TempData["Warning"] = "Two-factor authentication is required. Please set up your authenticator app to continue.";
                }

                context.Result = new RedirectToPageResult("/Account/Manage/EnableAuthenticator", new { area = "Identity" });
                return;
            }

            await next();
        }
    }
}
