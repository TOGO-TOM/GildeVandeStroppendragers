using Microsoft.AspNetCore.Mvc;
using AdminMembers.Services;

namespace AdminMembers.Pages.Account
{
    public class TotpSetupModel : AuthenticatedPageModel
    {
        private readonly AuthService _authService;
        private readonly TotpService _totpService;

        public TotpSetupModel(AuthService authService, TotpService totpService)
        {
            _authService = authService;
            _totpService = totpService;
        }

        [BindProperty] public string Secret     { get; set; } = string.Empty;
        [BindProperty] public string VerifyCode { get; set; } = string.Empty;

        public string  QrCodeDataUri   { get; set; } = string.Empty;
        public string  FormattedSecret { get; set; } = string.Empty;
        public bool    TotpEnabled     { get; set; }
        public string? SuccessMessage  { get; set; }
        public string? ErrorMessage    { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();

            var raw = await _authService.GetRawUserByIdAsync(CurrentUser!.Id);
            TotpEnabled = raw?.TotpEnabled ?? false;

            if (!TotpEnabled)
                BuildQr(raw?.TotpSecret);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();

            if (!_totpService.VerifyCode(Secret, VerifyCode))
            {
                ErrorMessage = "Invalid code. Please try again.";
                BuildQr(Secret);
                return Page();
            }

            await _totpService.EnableTotpAsync(CurrentUser!.Id, Secret);
            TotpEnabled    = true;
            SuccessMessage = "Two-factor authentication has been enabled successfully.";
            return Page();
        }

        public async Task<IActionResult> OnPostDisableAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            await _totpService.DisableTotpAsync(CurrentUser!.Id);
            return RedirectToPage("/Account/Index");
        }

        private void BuildQr(string? existingSecret)
        {
            Secret          = string.IsNullOrEmpty(existingSecret) ? _totpService.GenerateSecret() : existingSecret;
            FormattedSecret = string.Join(" ", Enumerable.Range(0, Secret.Length / 4).Select(i => Secret.Substring(i * 4, 4)));
            var uri         = _totpService.GetOtpUri(Secret, CurrentUser!.Username);
            QrCodeDataUri   = _totpService.GetQrCodeDataUri(uri);
        }
    }
}
