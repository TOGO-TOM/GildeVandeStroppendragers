using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdminMembers.Services;
using Microsoft.Extensions.Localization;

namespace AdminMembers.Pages
{
    public class TotpVerifyModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly TotpService _totpService;
        private readonly ILogger<TotpVerifyModel> _logger;
        private readonly IStringLocalizer<AdminMembers.SharedResources> _localizer;

        public TotpVerifyModel(AuthService authService, TotpService totpService, ILogger<TotpVerifyModel> logger, IStringLocalizer<AdminMembers.SharedResources> localizer)
        {
            _authService = authService;
            _totpService = totpService;
            _logger = logger;
            _localizer = localizer;
        }

        [BindProperty] public string Code        { get; set; } = string.Empty;
        [BindProperty] public bool   TrustDevice { get; set; }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var pendingId = HttpContext.Session.GetInt32("PendingTotpUserId");
            if (pendingId == null) return RedirectToPage("/Login");

            // Already trusted device — skip TOTP
            if (_totpService.IsDeviceTrusted(HttpContext.Request, pendingId.Value))
                return CompleteWithSession(pendingId.Value);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var pendingId = HttpContext.Session.GetInt32("PendingTotpUserId");
            if (pendingId == null) return RedirectToPage("/Login");

            var rawUser = await _authService.GetRawUserByIdAsync(pendingId.Value);
            if (rawUser == null || string.IsNullOrEmpty(rawUser.TotpSecret))
            {
                ErrorMessage = _localizer["SessionExpiredSignInAgain"];
                return Page();
            }

            if (!_totpService.VerifyCode(rawUser.TotpSecret, Code))
            {
                _logger.LogWarning("Failed TOTP attempt for user {UserId}", pendingId.Value);
                ErrorMessage = _localizer["InvalidCodeTryAgain"];
                return Page();
            }

            if (TrustDevice)
                _totpService.SetTrustedDeviceCookie(HttpContext.Response, pendingId.Value);

            return CompleteWithSession(pendingId.Value);
        }

        private IActionResult CompleteWithSession(int userId)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            // Fire-and-forget safe here — complete synchronously via GetAwaiter
            var result = _authService.CompleteLoginAsync(userId, ip).GetAwaiter().GetResult();

            if (!result.Success)
            {
                ErrorMessage = result.Message;
                return Page();
            }

            HttpContext.Session.Remove("PendingTotpUserId");
            var redirect = HttpContext.Session.GetString("PendingTotpRedirect") ?? "/Home";
            HttpContext.Session.Remove("PendingTotpRedirect");

            HttpContext.Session.SetString("AuthToken",    result.Token);
            HttpContext.Session.SetString("CurrentUser",  System.Text.Json.JsonSerializer.Serialize(result.User));
            HttpContext.Session.SetString("LoginTime",    DateTime.UtcNow.ToString("o"));

            return LocalRedirect(redirect);
        }
    }
}
