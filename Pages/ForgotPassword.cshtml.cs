using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdminMembers.Services;

namespace AdminMembers.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly EmailService _emailService;

        public ForgotPasswordModel(AuthService authService, EmailService emailService)
        {
            _authService = authService;
            _emailService = emailService;
        }

        [BindProperty] public string Email { get; set; } = string.Empty;
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Please enter your email address.";
                return Page();
            }

            var (success, token) = await _authService.GeneratePasswordResetTokenAsync(Email);

            if (success)
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                await _emailService.SendPasswordResetAsync(Email, Email, token, baseUrl);
            }

            // Always show success to prevent email enumeration
            SuccessMessage = "If an account with that email exists, a reset link has been sent.";
            return Page();
        }
    }
}
