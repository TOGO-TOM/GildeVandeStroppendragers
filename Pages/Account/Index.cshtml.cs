using Microsoft.AspNetCore.Mvc;
using AdminMembers.Services;
using Microsoft.Extensions.Localization;

namespace AdminMembers.Pages.Account
{
    public class IndexModel : AuthenticatedPageModel
    {
        private readonly AuthService _authService;
        private readonly IStringLocalizer<AdminMembers.SharedResources> _localizer;

        public IndexModel(AuthService authService, IStringLocalizer<AdminMembers.SharedResources> localizer)
        {
            _authService = authService;
            _localizer = localizer;
        }

        [BindProperty] public string CurrentPassword    { get; set; } = string.Empty;
        [BindProperty] public string NewPassword        { get; set; } = string.Empty;
        [BindProperty] public string ConfirmNewPassword { get; set; } = string.Empty;

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage   { get; set; }
        public bool    TotpEnabled    { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            var raw = await _authService.GetRawUserByIdAsync(CurrentUser!.Id);
            TotpEnabled = raw?.TotpEnabled ?? false;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();

            var raw = await _authService.GetRawUserByIdAsync(CurrentUser!.Id);
            TotpEnabled = raw?.TotpEnabled ?? false;

            if (string.IsNullOrWhiteSpace(CurrentPassword) ||
                string.IsNullOrWhiteSpace(NewPassword) ||
                string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                ErrorMessage = _localizer["AllFieldsRequired"];
                return Page();
            }

            if (NewPassword != ConfirmNewPassword)
            {
                ErrorMessage = _localizer["NewPasswordsDoNotMatch"];
                return Page();
            }

            if (NewPassword.Length < 6)
            {
                ErrorMessage = _localizer["PasswordMinimumLength"];
                return Page();
            }

            if (raw == null || !_authService.VerifyPasswordPublic(CurrentPassword, raw.PasswordHash))
            {
                ErrorMessage = _localizer["CurrentPasswordIncorrect"];
                return Page();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var (ok, message) = await _authService.ChangePasswordAsync(
                CurrentUser!.Id, NewPassword, CurrentUser!.Id, CurrentUser!.Username, ip);

            SuccessMessage = ok ? (string?)_localizer["PasswordUpdatedSuccessfully"] : null;
            if (!ok) ErrorMessage = message ?? (string?)_localizer["PasswordUpdateFailed"];
            return Page();
        }
    }
}
