using Microsoft.AspNetCore.Mvc;
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
            if (!context.HttpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdValue) ||
                !int.TryParse(userIdValue, out _))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Authentication required" });
                return;
            }

            var roles = context.HttpContext.Request.Headers
                .TryGetValue("X-User-Roles", out var rolesValue)
                ? rolesValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            // Super Admin and Admin bypass all permission checks
            if (roles.Contains("Super Admin") || roles.Contains("Admin"))
                return;

            bool hasPermission = _requiredPermission switch
            {
                Permission.Read =>
                    roles.Contains("Member Editor") || roles.Contains("Member Viewer") ||
                    roles.Contains("Stock Editor")  || roles.Contains("Stock Viewer"),
                Permission.Write =>
                    roles.Contains("Member Editor") || roles.Contains("Stock Editor"),
                Permission.ReadWrite =>
                    roles.Contains("Member Editor") || roles.Contains("Stock Editor"),
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
        public ForbiddenResult() : base(new { message = "Forbidden" }) { StatusCode = 403; }
    }
}
