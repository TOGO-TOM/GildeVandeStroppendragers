using Microsoft.AspNetCore.Mvc;

namespace AdminMembers.Pages.Admin
{
    public class AiSettingsModel : AuthenticatedPageModel
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
