using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdminMembers.Services;
using Microsoft.Extensions.Localization;

namespace AdminMembers.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly PasswordPolicyService _passwordPolicy;
        private readonly IStringLocalizer<AdminMembers.SharedResources> _localizer;

        public ResetPasswordModel(AuthService authService, PasswordPolicyService passwordPolicy, IStringLocalizer<AdminMembers.SharedResources> localizer)
        {
            _authService = authService;
            _passwordPolicy = passwordPolicy;
            _localizer = localizer;
        }

        [BindProperty(SupportsGet = true)] public string Token { get; set; } = string.Empty;
        [BindProperty(SupportsGet = true)] public string Email { get; set; } = string.Empty;
        [BindProperty] public string NewPassword { get; set; } = string.Empty;
        [BindProperty] public string ConfirmPassword { get; set; } = string.Empty;

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string PasswordHint => _passwordPolicy.GetRequirementsHint();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword != ConfirmPassword)
            {
                ErrorMessage = _localizer["PasswordsDoNotMatch"];
                return Page();
            }

            var (ok, message) = await _authService.ResetPasswordWithTokenAsync(Email, Token, NewPassword);

            if (!ok)
            {
                ErrorMessage = message;
                return Page();
            }

            SuccessMessage = _localizer["PasswordResetCompleted"];
            return Page();
        }
    }
}
