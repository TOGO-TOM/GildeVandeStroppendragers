using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public IndexModel(ApplicationDbContext context, AuthService authService, ILogger<IndexModel> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
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
            if (!HasPermission("ReadWrite")) return Forbid();
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

            if (ok) SuccessMessage = $"User '{NewUsername}' created.";
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
            SuccessMessage = ok ? "Roles updated." : null;
            if (!ok) ErrorMessage = "Failed to update roles.";
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

            if (!ok) ErrorMessage = "Failed to update user status.";
            else SuccessMessage = activate ? "User activated." : "User deactivated.";
            return RedirectToPage();
        }

        // ?? Force / clear 2FA requirement ????????????????????????????????????
        public async Task<IActionResult> OnPostSet2faRequiredAsync(int userId, bool required)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ok = await _authService.SetTotpRequiredAsync(userId, required, CurrentUser!.Id, CurrentUser.Username, ip);

            if (!ok) ErrorMessage = "Failed to update 2FA setting.";
            else SuccessMessage = required ? "2FA is now required for this user. Their existing TOTP has been reset so they must re-enrol." : "2FA requirement removed.";
            return RedirectToPage();
        }

        // ?? Admin reset password ??????????????????????????????????????????????
        public async Task<IActionResult> OnPostResetPasswordAsync(int userId, string newPassword)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters.";
                return RedirectToPage();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ok = await _authService.AdminResetPasswordAsync(userId, newPassword, CurrentUser!.Id, CurrentUser.Username, ip);
            if (!ok) ErrorMessage = "Failed to reset password.";
            else SuccessMessage = "Password reset successfully.";
            return RedirectToPage();
        }

        // ?? Approve pending user ??????????????????????????????????????????????
        public async Task<IActionResult> OnPostApproveAsync(int userId, int roleId)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ok = await _authService.ApproveUserAsync(userId, roleId, CurrentUser!.Id, CurrentUser.Username, ip);
            if (!ok) ErrorMessage = "Failed to approve user.";
            else SuccessMessage = "User approved.";
            return RedirectToPage();
        }

        // ?? Reject pending user ???????????????????????????????????????????????
        public async Task<IActionResult> OnPostRejectAsync(int userId)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _authService.DeleteUserAsync(userId, CurrentUser!.Id, CurrentUser.Username, ip);
            SuccessMessage = "Registration request rejected and removed.";
            return RedirectToPage();
        }

        // ?? Delete user ???????????????????????????????????????????????????????
        public async Task<IActionResult> OnPostDeleteAsync(int userId)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("ReadWrite")) return Forbid();

            if (userId == CurrentUser!.Id)
            {
                ErrorMessage = "You cannot delete your own account.";
                return RedirectToPage();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ok = await _authService.DeleteUserAsync(userId, CurrentUser.Id, CurrentUser.Username, ip);
            if (!ok) ErrorMessage = "Failed to delete user.";
            else SuccessMessage = "User deleted.";
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
