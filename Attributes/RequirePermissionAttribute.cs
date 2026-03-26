using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AdminMembers.Models;

namespace AdminMembers.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly Permission _requiredPermission;

        public RequirePermissionAttribute(Permission permission)
        {
            _requiredPermission = permission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated (simplified - check for user ID in headers)
            if (!context.HttpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdValue) ||
                !int.TryParse(userIdValue, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Authentication required" });
                return;
            }

            // Get user permissions from headers (set by middleware)
            if (!context.HttpContext.Request.Headers.TryGetValue("X-User-Permissions", out var permissionsValue))
            {
                context.Result = new ForbiddenResult();
                return;
            }

            var permissions = permissionsValue.ToString().Split(',');

            // Check if user has required permission
            bool hasPermission = _requiredPermission switch
            {
                Permission.Read => permissions.Contains("Read") || permissions.Contains("ReadWrite"),
                Permission.Write => permissions.Contains("Write") || permissions.Contains("ReadWrite"),
                Permission.ReadWrite => permissions.Contains("ReadWrite"),
                _ => false
            };

            if (!hasPermission)
            {
                context.Result = new ObjectResult(new { message = "Insufficient permissions" })
                {
                    StatusCode = 403
                };
            }
        }
    }

    public class ForbiddenResult : ObjectResult
    {
        public ForbiddenResult() : base(new { message = "Forbidden" })
        {
            StatusCode = 403;
        }
    }
}
