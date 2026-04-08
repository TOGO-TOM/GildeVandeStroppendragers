using Microsoft.AspNetCore.Mvc;

namespace AdminMembers.Pages.Admin
{
    public class EmailSettingsModel : AuthenticatedPageModel
    {
        public string? AuthToken { get; set; }

        public IActionResult OnGet()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!IsSuperAdmin()) return Forbid();

            AuthToken = HttpContext.Session.GetString("AuthToken");
            return Page();
        }
    }
}
