using AdminMembers.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AdminMembers.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext, IMemoryCache cache)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/swagger") || path.Contains("/.well-known") ||
                context.Request.Method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var token = authHeader.ToString().Replace("Bearer ", "");

                try
                {
                    var tokenBytes = Convert.FromBase64String(token);
                    var tokenData  = System.Text.Encoding.UTF8.GetString(tokenBytes);
                    var parts      = tokenData.Split(':');

                    if (parts.Length >= 2 && int.TryParse(parts[0], out var userId))
                    {
                        var cacheKey = $"auth_user_{userId}";

                        if (!cache.TryGetValue(cacheKey, out (string username, string permissions) cached))
                        {
                            var user = await dbContext.Users
                                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                            if (user != null)
                            {
                                var perms = string.Join(",", user.UserRoles
                                    .Select(ur => ur.Role.Permission)
                                    .Distinct()
                                    .Select(p => p.ToString()));

                                cached = (user.Username, perms);
                                cache.Set(cacheKey, cached, TimeSpan.FromMinutes(5));
                            }
                        }

                        if (!string.IsNullOrEmpty(cached.username))
                        {
                            context.Request.Headers["X-User-Id"]          = userId.ToString();
                            context.Request.Headers["X-Username"]          = cached.username;
                            context.Request.Headers["X-User-Permissions"]  = cached.permissions;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid token provided");
                }
            }

            await _next(context);
        }
    }

    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<AuthenticationMiddleware>();
    }
}
