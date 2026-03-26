using AdminMembers.Data;
using Microsoft.EntityFrameworkCore;

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

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            // Skip authentication for certain endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/swagger") || path.Contains("/.well-known") ||
                context.Request.Method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            // Check for token in Authorization header
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var token = authHeader.ToString().Replace("Bearer ", "");
                
                try
                {
                    // Decode token (simplified - in production use JWT)
                    var tokenBytes = Convert.FromBase64String(token);
                    var tokenData = System.Text.Encoding.UTF8.GetString(tokenBytes);
                    var parts = tokenData.Split(':');
                    
                    if (parts.Length >= 2 && int.TryParse(parts[0], out var userId))
                    {
                        // Load user with roles
                        var user = await dbContext.Users
                            .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                        if (user != null)
                        {
                            // Set user information in headers for downstream use
                            context.Request.Headers["X-User-Id"] = user.Id.ToString();
                            context.Request.Headers["X-Username"] = user.Username;
                            
                            // Aggregate permissions
                            var permissions = user.UserRoles
                                .Select(ur => ur.Role.Permission)
                                .Distinct()
                                .Select(p => p.ToString())
                                .ToList();
                            
                            context.Request.Headers["X-User-Permissions"] = string.Join(",", permissions);
                            
                            _logger.LogInformation("User {Username} authenticated with permissions: {Permissions}", 
                                user.Username, string.Join(",", permissions));
                        }
                        else
                        {
                            _logger.LogWarning("Invalid or inactive user ID: {UserId}", userId);
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
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}
