using itpayroll.Areas.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;

namespace itpayroll.Filters
{
    public class RequirePasswordChangeAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var path = context.HttpContext.Request.Path.Value?.ToLower();

            // Skip for auth pages, static files, and error pages
            if (string.IsNullOrEmpty(path) ||
                path.Contains("changepassword") ||
                path.Contains("login") ||
                path.Contains("logout") ||
                path.Contains("register") ||
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

             if (user != null && user.MustChangePassword)
             {
                 context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
                 return;
             }

            await next();
        }
    }
}
