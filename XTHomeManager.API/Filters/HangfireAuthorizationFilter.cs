// Filters/HangfireAuthorizationFilter.cs
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace XTHomeManager.API.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext() ?? (context as Microsoft.AspNetCore.Http.IHttpContextAccessor)?.HttpContext;
            return httpContext.User.Identity?.IsAuthenticated == true
                   && httpContext.User.IsInRole("Admin");
        }
    }
}