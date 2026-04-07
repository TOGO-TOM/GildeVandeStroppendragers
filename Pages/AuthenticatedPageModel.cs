using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace AdminMembers.Pages
{
    public class AuthenticatedPageModel : PageModel
    {
        public class UserInfo
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public List<string> Roles { get; set; } = new();
        }

        public UserInfo? CurrentUser { get; set; }
        public bool IsAuthenticated { get; set; }

        protected bool CheckAuthentication(string? requiredPermission = null)
        {
            var token = HttpContext.Session.GetString("AuthToken");
            var userJson = HttpContext.Session.GetString("CurrentUser");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userJson))
            {
                IsAuthenticated = false;
                return false;
            }

            // Sliding session — refresh LoginTime on every valid request
            var loginTimeStr = HttpContext.Session.GetString("LoginTime");
            if (!string.IsNullOrEmpty(loginTimeStr) && DateTime.TryParse(loginTimeStr, out DateTime loginTime))
            {
                if ((DateTime.UtcNow - loginTime).TotalMinutes > 60)
                {
                    HttpContext.Session.Clear();
                    IsAuthenticated = false;
                    return false;
                }
                // Slide the expiry window
                HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString("O"));
            }

            try
            {
                CurrentUser = JsonSerializer.Deserialize<UserInfo>(userJson);
                IsAuthenticated = true;

                // Check permission if required
                if (!string.IsNullOrEmpty(requiredPermission))
                {
                    return HasPermission(requiredPermission);
                }

                return true;
            }
            catch
            {
                IsAuthenticated = false;
                return false;
            }
        }

        protected internal bool HasPermission(string permission)
        {
            if (CurrentUser == null || CurrentUser.Roles == null)
                return false;

            var roles = CurrentUser.Roles;

            // Super Admin & Admin have full access to everything (except audit logs, handled separately)
            if (roles.Contains("Super Admin") || roles.Contains("Admin"))
                return true;

            return permission switch
            {
                "Read"      => roles.Contains("Member Editor") || roles.Contains("Member Viewer") ||
                               roles.Contains("Stock Editor")  || roles.Contains("Stock Viewer"),
                "Write"     => roles.Contains("Member Editor") || roles.Contains("Stock Editor"),
                "ReadWrite" => roles.Contains("Member Editor") || roles.Contains("Stock Editor"),
                "MemberRead"  => roles.Contains("Member Editor") || roles.Contains("Member Viewer"),
                "MemberWrite" => roles.Contains("Member Editor"),
                "StockRead"   => roles.Contains("Stock Editor")  || roles.Contains("Stock Viewer"),
                "StockWrite"  => roles.Contains("Stock Editor"),
                "AuditLogs"   => roles.Contains("Super Admin"),
                "AgendaRead"  => true,
                "AgendaWrite" => roles.Contains("Member Editor"),
                _             => false
            };
        }

        protected internal bool CanManageAgenda()
            => IsAdmin() || HasPermission("AgendaWrite");

        protected internal bool CanViewAgenda()
            => IsAuthenticated;

        protected internal bool IsSuperAdmin()
            => CurrentUser?.Roles?.Contains("Super Admin") == true;

        protected internal bool IsAdmin()
            => CurrentUser?.Roles?.Contains("Super Admin") == true ||
               CurrentUser?.Roles?.Contains("Admin") == true;

        protected internal bool CanManageMembers()
            => IsAdmin() || HasPermission("MemberWrite");

        protected internal bool CanViewMembers()
            => IsAdmin() || HasPermission("MemberRead");

        protected internal bool CanManageStock()
            => IsAdmin() || HasPermission("StockWrite");

        protected internal bool CanViewStock()
            => IsAdmin() || HasPermission("StockRead");

        protected IActionResult RedirectToLoginWithReturnUrl()
        {
            var currentPath = HttpContext.Request.Path;
            return RedirectToPage("/Login", new { redirect = currentPath });
        }
    }
}
