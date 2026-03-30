using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;

namespace AdminMembers.Pages
{
    public class SettingsModel : AuthenticatedPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsModel> _logger;

        public SettingsModel(ApplicationDbContext context, ILogger<SettingsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<CustomField> CustomFields { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public AppSettings? GeneralSettings { get; set; }
        public bool HasLogo { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckAuthentication())
            {
                return RedirectToLoginWithReturnUrl();
            }

            try
            {
                // Load custom fields
                CustomFields = await _context.CustomFields
                    .OrderBy(cf => cf.DisplayOrder)
                    .ToListAsync();

                // Load users (only for Admins)
                if (HasPermission("ReadWrite"))
                {
                    Users = await _context.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .OrderBy(u => u.Username)
                        .ToListAsync();

                    Roles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();
                }

                // Load general settings
                GeneralSettings = await _context.AppSettings.FirstOrDefaultAsync();
                if (GeneralSettings == null)
                {
                    GeneralSettings = new AppSettings { CompanyName = "Member Administration" };
                }

                HasLogo = GeneralSettings.LogoData != null && GeneralSettings.LogoData.Length > 0;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings");
                return Page();
            }
        }
    }
}
