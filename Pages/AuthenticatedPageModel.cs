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

            // Check session timeout (15 minutes)
            var loginTimeStr = HttpContext.Session.GetString("LoginTime");
            if (!string.IsNullOrEmpty(loginTimeStr))
            {
                if (DateTime.TryParse(loginTimeStr, out DateTime loginTime))
                {
                    if ((DateTime.UtcNow - loginTime).TotalMinutes > 15)
                    {
                        // Session expired
                        HttpContext.Session.Clear();
                        IsAuthenticated = false;
                        return false;
                    }
                }
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

        protected bool HasPermission(string permission)
        {
            if (CurrentUser == null || CurrentUser.Roles == null)
                return false;

            // Admin has all permissions
            if (CurrentUser.Roles.Contains("Admin"))
                return true;

            // Editor has read/write
            if (permission == "Read" && (CurrentUser.Roles.Contains("Editor") || CurrentUser.Roles.Contains("Viewer")))
                return true;

            if (permission == "Write" && CurrentUser.Roles.Contains("Editor"))
                return true;

            if (permission == "ReadWrite" && CurrentUser.Roles.Contains("Editor"))
                return true;

            return false;
        }

        protected IActionResult RedirectToLoginWithReturnUrl()
        {
            var currentPath = HttpContext.Request.Path;
            return RedirectToPage("/Login", new { redirect = currentPath });
        }
    }
}
