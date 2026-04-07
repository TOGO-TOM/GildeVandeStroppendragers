using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdminMembers.Models;
using AdminMembers.Services;
using Microsoft.Extensions.Localization;

namespace AdminMembers.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly ILogger<RegisterModel> _logger;
        private readonly PasswordPolicyService _passwordPolicy;
        private readonly IStringLocalizer<AdminMembers.SharedResources> _localizer;

        public RegisterModel(AuthService authService, ILogger<RegisterModel> logger, PasswordPolicyService passwordPolicy, IStringLocalizer<AdminMembers.SharedResources> localizer)
        {
            _authService = authService;
            _logger = logger;
            _passwordPolicy = passwordPolicy;
            _localizer = localizer;
        }

        [BindProperty] public string Username { get; set; } = string.Empty;
        [BindProperty] public string Email { get; set; } = string.Empty;
        [BindProperty] public string Password { get; set; } = string.Empty;
        [BindProperty] public string ConfirmPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string PasswordHint => _passwordPolicy.GetRequirementsHint();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = _localizer["AllFieldsRequired"];
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = _localizer["PasswordsDoNotMatch"];
                return Page();
            }

            if (Password.Length < 6)
            {
                ErrorMessage = _localizer["PasswordMinimumLength"];
                return Page();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var request = new SelfRegisterRequest
            {
                Username        = Username.Trim(),
                Email           = Email.Trim(),
                Password        = Password,
                ConfirmPassword = ConfirmPassword
            };

            var (success, message, _) = await _authService.SelfRegisterAsync(request, ip);

            if (success)
                SuccessMessage = message;
            else
                ErrorMessage = message;

            return Page();
        }
    }
}
