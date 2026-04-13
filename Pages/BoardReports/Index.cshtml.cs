using Microsoft.AspNetCore.Mvc;

namespace AdminMembers.Pages.BoardReports
{
    public class IndexModel : AuthenticatedPageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public string? AuthToken { get; set; }
        public bool CanWrite { get; set; }

        public IActionResult OnGet()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();

            if (!CanViewBoardReports())
                return RedirectToPage("/Home");

            AuthToken = HttpContext.Session.GetString("AuthToken");
            CanWrite = CanManageBoardReports();

            return Page();
        }
    }
}
