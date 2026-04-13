using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AdminMembers.Models;

namespace AdminMembers.Attributes
{
    public enum ResourceType { Member, Stock, Agenda, BoardReport }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly Permission _requiredPermission;
        private readonly ResourceType _resource;

        public RequirePermissionAttribute(Permission permission, ResourceType resource = ResourceType.Member)
        {
            _requiredPermission = permission;
            _resource = resource;
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

            // Super Admin, Admin and Secretaris bypass all permission checks (Secretaris has no admin pages)
            if (roles.Contains("Super Admin") || roles.Contains("Admin") || roles.Contains("Secretaris"))
                return;

            bool hasPermission = (_resource, _requiredPermission) switch
            {
                (ResourceType.Member, Permission.Read) =>
                    roles.Contains("Member Editor") || roles.Contains("Member Viewer"),
                (ResourceType.Member, Permission.Write) or (ResourceType.Member, Permission.ReadWrite) =>
                    roles.Contains("Member Editor"),
                (ResourceType.Stock, Permission.Read) =>
                    roles.Contains("Stock Editor") || roles.Contains("Stock Viewer"),
                (ResourceType.Stock, Permission.Write) or (ResourceType.Stock, Permission.ReadWrite) =>
                    roles.Contains("Stock Editor"),
                (ResourceType.Agenda, Permission.Read) => true,
                (ResourceType.Agenda, Permission.Write) or (ResourceType.Agenda, Permission.ReadWrite) =>
                    roles.Contains("Member Editor"),
                (ResourceType.BoardReport, Permission.Read) => false,
                (ResourceType.BoardReport, Permission.Write) or (ResourceType.BoardReport, Permission.ReadWrite) => false,
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
