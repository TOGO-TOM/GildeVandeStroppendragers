using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AdminMembers.Services;

namespace AdminMembers.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(AuthService authService, ILogger<LoginModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public string? Username { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        public string? ErrorMessage { get; set; }
        public string? RedirectUrl { get; set; }

        public void OnGet(string? redirect)
        {
            // Check if already logged in
            var token = HttpContext.Session.GetString("AuthToken");
            var userJson = HttpContext.Session.GetString("CurrentUser");
            
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
            {
                // Already logged in, redirect
                var redirectPage = redirect ?? "/Home";
                Response.Redirect(redirectPage);
                return;
            }

            RedirectUrl = redirect;
        }

        public async Task<IActionResult> OnPostAsync(string? redirect)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                RedirectUrl = redirect;
                return Page();
            }

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var loginRequest = new AdminMembers.Models.LoginRequest 
                { 
                    Username = Username, 
                    Password = Password 
                };
                
                var result = await _authService.LoginAsync(loginRequest, ipAddress);

                if (result.Success && result.User != null)
                {
                    // Store authentication data in session
                    HttpContext.Session.SetString("AuthToken", result.Token);
                    HttpContext.Session.SetString("CurrentUser", System.Text.Json.JsonSerializer.Serialize(result.User));
                    HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString("o"));

                    _logger.LogInformation($"User {Username} logged in successfully via Razor Pages");

                    // Redirect to requested page or home
                    var redirectPage = redirect ?? "/Home";
                    return RedirectToPage(redirectPage);
                }
                else
                {
                    ErrorMessage = result.Message ?? "Invalid username or password.";
                    RedirectUrl = redirect;
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", Username);
                ErrorMessage = "An error occurred during login. Please try again.";
                RedirectUrl = redirect;
                return Page();
            }
        }
    }
}
