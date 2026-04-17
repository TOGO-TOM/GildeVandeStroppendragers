using Microsoft.AspNetCore.Mvc;

namespace AdminMembers.Pages.Admin
{
    public class ApiKeysModel : AuthenticatedPageModel
    {
        public string AuthToken { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();

            // Super Admin only
            if (CurrentUser == null || !CurrentUser.Roles.Contains("Super Admin"))
                return Forbid();

            AuthToken = HttpContext.Session.GetString("AuthToken") ?? "";
            return Page();
        }
    }
}
