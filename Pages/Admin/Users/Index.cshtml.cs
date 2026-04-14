using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Services;

namespace AdminMembers.Pages.Admin.Users
{
    public class IndexModel : AuthenticatedPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private readonly ILogger<IndexModel> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public IndexModel(ApplicationDbContext context, AuthService authService, ILogger<IndexModel> logger, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
            _localizer = localizer;
        }

        public List<User> Users { get; set; } = new();
        public List<User> PendingUsers { get; set; } = new();
        public List<Role> Roles { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        // Bound for create user form
        [BindProperty] public string NewUsername { get; set; } = string.Empty;
        [BindProperty] public string NewEmail { get; set; } = string.Empty;
        [BindProperty] public string NewPassword { get; set; } = string.Empty;
        [BindProperty] public List<int> NewRoleIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!IsAdmin()) return Forbid();
            await LoadDataAsync();
            return Page();
        }

        // ?? Create new user ??????????????????????????????????????????????????
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var (ok, msg, _) = await _authService.RegisterUserAsync(
                new Models.RegisterRequest
                {
                    Username = NewUsername,
                    Email    = NewEmail,
                    Password = NewPassword,
                    RoleIds  = NewRoleIds
                },
                CurrentUser!.Id, CurrentUser.Username, ip);

            if (ok) SuccessMessage = string.Format(_localizer["UserCreatedSuccess"].Value, NewUsername);
            else    ErrorMessage   = msg;

            return RedirectToPage();
        }

        // ?? Update roles ?????????????????????????????????????????????????????
        public async Task<IActionResult> OnPostUpdateRolesAsync(int userId, List<int> roleIds)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ok = await _authService.UpdateUserRolesAsync(userId, roleIds, CurrentUser!.Id, CurrentUser.Username, ip);
            SuccessMessage = ok ? _localizer["RolesUpdated"].Value : null;
            if (!ok) ErrorMessage = _localizer["FailedUpdateRoles"].Value;
            return RedirectToPage();
        }

        // ?? Toggle active / deactivate ????????????????????????????????????????
        public async Task<IActionResult> OnPostToggleActiveAsync(int userId, bool activate)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            bool ok;
            if (activate)
                ok = await _authService.ActivateUserAsync(userId, CurrentUser!.Id, CurrentUser.Username, ip);
            else
                ok = await _authService.DeactivateUserAsync(userId, CurrentUser!.Id, CurrentUser.Username, ip);

            if (!ok) ErrorMessage = _localizer["FailedUpdateUserStatus"].Value;
            else SuccessMessage = activate ? _localizer["UserActivated"].Value : _localizer["UserDeactivated"].Value;
            return RedirectToPage();
        }

        // ?? Force / clear 2FA requirement ????????????????????????????????????
        public async Task<IActionResult> OnPostSet2faRequiredAsync(int userId, bool required)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ok = await _authService.SetTotpRequiredAsync(userId, required, CurrentUser!.Id, CurrentUser.Username, ip);

            if (!ok) ErrorMessage = _localizer["FailedUpdate2FA"].Value;
            else SuccessMessage = required ? _localizer["TwoFANowRequired"].Value : _localizer["TwoFARequirementRemoved"].Value;
            return RedirectToPage();
        }

        // ?? Admin reset password ??????????????????????????????????????????????
        public async Task<IActionResult> OnPostResetPasswordAsync(int userId, string newPassword)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                ErrorMessage = _localizer["PasswordMin6"].Value;
                return RedirectToPage();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var (ok, message) = await _authService.AdminResetPasswordAsync(userId, newPassword, CurrentUser!.Id, CurrentUser.Username, ip);
            if (!ok) ErrorMessage = message ?? _localizer["FailedResetPassword"].Value;
            else SuccessMessage = _localizer["PasswordResetSuccess"].Value;
            return RedirectToPage();
        }

        // ?? Approve pending user ??????????????????????????????????????????????
        public async Task<IActionResult> OnPostApproveAsync(int userId, int roleId)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ok = await _authService.ApproveUserAsync(userId, roleId, CurrentUser!.Id, CurrentUser.Username, ip);
            if (!ok) ErrorMessage = _localizer["FailedApproveUser"].Value;
            else SuccessMessage = _localizer["UserApproved"].Value;
            return RedirectToPage();
        }

        // ?? Reject pending user ???????????????????????????????????????????????
        public async Task<IActionResult> OnPostRejectAsync(int userId)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _authService.DeleteUserAsync(userId, CurrentUser!.Id, CurrentUser.Username, ip);
            SuccessMessage = _localizer["RegistrationRejected"].Value;
            return RedirectToPage();
        }

        // ?? Delete user ???????????????????????????????????????????????????????
        public async Task<IActionResult> OnPostDeleteAsync(int userId)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            if (userId == CurrentUser!.Id)
            {
                ErrorMessage = _localizer["CannotDeleteOwnAccount"].Value;
                return RedirectToPage();
            }

            // Prevent non-Super Admins from deleting a Super Admin
            var targetUser = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (targetUser != null && targetUser.UserRoles.Any(ur => ur.Role.Name == "Super Admin") && !IsSuperAdmin())
            {
                ErrorMessage = _localizer["CannotDeleteSuperAdmin"].Value;
                return RedirectToPage();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ok = await _authService.DeleteUserAsync(userId, CurrentUser.Id, CurrentUser.Username, ip);
            if (!ok) ErrorMessage = _localizer["FailedDeleteUser"].Value;
            else SuccessMessage = _localizer["UserDeleted"].Value;
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
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
    }
}
