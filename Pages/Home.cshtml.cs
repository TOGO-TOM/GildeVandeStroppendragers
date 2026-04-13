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

        public bool CanWriteAgenda { get; set; }
        public bool CanViewBoardReports { get; set; }
        public string? AuthToken { get; set; }

        public IActionResult OnGet()
        {
            if (!CheckAuthentication())
            {
                return RedirectToLoginWithReturnUrl();
            }

            CanWriteAgenda = CanManageAgenda();
            CanViewBoardReports = CanViewBoardReports();
            AuthToken = HttpContext.Session.GetString("AuthToken");

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}
