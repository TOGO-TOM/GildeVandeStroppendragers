using Microsoft.AspNetCore.Mvc;
using AdminMembers.Services;

namespace AdminMembers.Pages.Account
{
    public class IndexModel : AuthenticatedPageModel
    {
        private readonly AuthService _authService;

        public IndexModel(AuthService authService) => _authService = authService;

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
                ErrorMessage = "All fields are required.";
                return Page();
            }

            if (NewPassword != ConfirmNewPassword)
            {
                ErrorMessage = "New passwords do not match.";
                return Page();
            }

            if (NewPassword.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters.";
                return Page();
            }

            if (raw == null || !_authService.VerifyPasswordPublic(CurrentPassword, raw.PasswordHash))
            {
                ErrorMessage = "Current password is incorrect.";
                return Page();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ok = await _authService.ChangePasswordAsync(
                CurrentUser!.Id, NewPassword, CurrentUser!.Id, CurrentUser!.Username, ip);

            SuccessMessage = ok ? "Password updated successfully." : null;
            if (!ok) ErrorMessage = "Failed to update password. Please try again.";
            return Page();
        }
    }
}
