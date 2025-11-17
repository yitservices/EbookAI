using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace EBookDashboard.Middleware
{
    public class SessionManagementMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionManagementMiddleware> _logger;

        public SessionManagementMiddleware(RequestDelegate next, ILogger<SessionManagementMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Track user session activity
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var lastActivity = context.Session.GetString("LastActivity");
                var currentTime = DateTime.UtcNow;

                if (lastActivity != null)
                {
                    var lastActivityTime = DateTime.Parse(lastActivity);
                    var inactivityPeriod = currentTime - lastActivityTime;

                    // Auto-logout after 30 minutes of inactivity
                    if (inactivityPeriod.TotalMinutes > 30)
                    {
                        _logger.LogInformation("User session expired due to inactivity. User: {User}", 
                            context.User.Identity.Name);
                        
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        context.Session.Clear();
                        
                        if (IsAjaxRequest(context))
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("Session expired");
                            return;
                        }
                        
                        context.Response.Redirect("/Account/Login?sessionExpired=true");
                        return;
                    }
                }

                // Update last activity
                context.Session.SetString("LastActivity", currentTime.ToString());
                
                // Track page views for analytics
                var currentPage = context.Request.Path.Value;
                if (!string.IsNullOrEmpty(currentPage) && !currentPage.Contains("/api/"))
                {
                    IncrementPageView(context, currentPage);
                }
            }

            await _next(context);
        }

        private static bool IsAjaxRequest(HttpContext context)
        {
            return context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   context.Request.Headers["Content-Type"].ToString().Contains("application/json");
        }

        private static void IncrementPageView(HttpContext context, string page)
        {
            var pageViewsKey = $"PageViews_{page}";
            var currentViews = context.Session.GetInt32(pageViewsKey) ?? 0;
            context.Session.SetInt32(pageViewsKey, currentViews + 1);
        }
    }

    // Extension method for easy registration
    public static class SessionManagementMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionManagement(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionManagementMiddleware>();
        }
    }
}