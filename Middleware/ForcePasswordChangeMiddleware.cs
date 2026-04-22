using Microsoft.AspNetCore.Identity;
using Sedziowanie.Models;

namespace Sedziowanie.Middleware;

public class ForcePasswordChangeMiddleware
{
    private readonly RequestDelegate _next;

    public ForcePasswordChangeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value ?? "";
            var isAllowedPath =
                path.StartsWith("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase);

            if (!isAllowedPath)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user?.MustChangePassword == true)
                {
                    context.Response.Redirect("/Account/ChangePassword?forced=true");
                    return;
                }
            }
        }

        await _next(context);
    }
}
