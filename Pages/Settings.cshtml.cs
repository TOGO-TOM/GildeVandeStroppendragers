using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Services;

namespace AdminMembers.Pages
{
    public class SettingsModel : AuthenticatedPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsModel> _logger;
        private readonly AuthService _authService;

        public SettingsModel(ApplicationDbContext context, ILogger<SettingsModel> logger, AuthService authService)
        {
            _context = context;
            _logger = logger;
            _authService = authService;
        }

        public List<CustomField> CustomFields { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<User> PendingUsers { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public AppSettings? GeneralSettings { get; set; }
        public bool HasLogo { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int userId, int roleId)
        {
            if (!CheckAuthentication() || !HasPermission("ReadWrite"))
                return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _authService.ApproveUserAsync(userId, roleId, CurrentUser!.Id, CurrentUser.Username, ip);

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRejectAsync(int userId)
        {
            if (!CheckAuthentication() || !HasPermission("ReadWrite"))
                return Forbid();

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            await LoadDataAsync();
            return Page();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                CustomFields = await _context.CustomFields.OrderBy(cf => cf.DisplayOrder).ToListAsync();

                if (HasPermission("ReadWrite"))
                {
                    Users = await _context.Users
                        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                        .Where(u => u.IsApproved)
                        .OrderBy(u => u.Username)
                        .ToListAsync();

                    PendingUsers = await _context.Users
                        .Where(u => !u.IsApproved)
                        .OrderBy(u => u.CreatedAt)
                        .ToListAsync();

                    Roles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();
                }

                GeneralSettings = await _context.AppSettings.FirstOrDefaultAsync()
                    ?? new AppSettings { CompanyName = "Member Administration" };

                HasLogo = GeneralSettings.LogoData != null && GeneralSettings.LogoData.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings");
            }
        }
    }
}
