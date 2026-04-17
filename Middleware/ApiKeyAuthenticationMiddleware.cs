using AdminMembers.Services;

namespace AdminMembers.Middleware
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

        public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ApiKeyService apiKeyService)
        {
            // Skip if Bearer token is already present
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                await _next(context);
                return;
            }

            string? rawKey = null;

            if (context.Request.Headers.TryGetValue("X-Api-Key", out var headerKey))
            {
                rawKey = headerKey.ToString();
            }
            else if (context.Request.Query.TryGetValue("apikey", out var queryKey))
            {
                rawKey = queryKey.ToString();
            }

            if (!string.IsNullOrEmpty(rawKey))
            {
                try
                {
                    var apiKey = await apiKeyService.ValidateKeyAsync(rawKey);
                    if (apiKey != null)
                    {
                        var permission = apiKey.Permission == Models.ApiKeyPermission.ReadWrite
                            ? "ReadWrite" : "Read";

                        // Sanitize name: strip control chars and newlines to prevent header injection
                        var safeName = SanitizeHeaderValue(apiKey.Name);

                        context.Request.Headers["X-User-Id"] = "0";
                        context.Request.Headers["X-Username"] = $"ApiKey:{safeName}";
                        context.Request.Headers["X-User-Permissions"] = permission;
                        context.Request.Headers["X-User-Roles"] = "ApiKeyUser";

                        // Prevent caching of API-key authenticated responses (key may appear in query string)
                        context.Response.Headers["Cache-Control"] = "no-store";
                    }
                    else
                    {
                        _logger.LogWarning("Invalid or expired API key attempted");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error validating API key");
                }
            }

            await _next(context);
        }

        private static string SanitizeHeaderValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Unknown";
            // Remove any characters that could be used for header injection
            var sanitized = new string(value.Where(c => c >= 0x20 && c != 0x7F).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
        }
    }

    public static class ApiKeyAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuthenticationMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
