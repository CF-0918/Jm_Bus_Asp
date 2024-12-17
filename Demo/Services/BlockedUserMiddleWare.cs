using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Demo.Services;

public class BlockedUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BlockedUserMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.Name)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<DB>();

                    // Check the user's status in the database
                    var user = db.Users.FirstOrDefault(u => u.Id == userId);
                    if (user != null && user.Status == "Blocked")
                    {
                        // Log out the user
                        await context.SignOutAsync();
                        // Redirect to the login page
                        context.Response.Redirect("/Account/Login");
                        return; // End the current request
                    }
                }
            }
        }

        // Call the next middleware in the pipeline
        await _next(context);
    }
}
