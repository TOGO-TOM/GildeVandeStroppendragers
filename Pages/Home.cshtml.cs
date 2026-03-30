using Microsoft.AspNetCore.Mvc;

namespace AdminMembers.Pages
{
    public class HomeModel : AuthenticatedPageModel
    {
        private readonly ILogger<HomeModel> _logger;

        public HomeModel(ILogger<HomeModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            if (!CheckAuthentication())
            {
                return RedirectToLoginWithReturnUrl();
            }

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}
