using System.Security.Cryptography;
using System.Text;
using AdminMembers.Data;
using AdminMembers.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminMembers.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly AuditLogService _auditLogService;
        private readonly PasswordPolicyService _passwordPolicy;

        public AuthService(ApplicationDbContext context, ILogger<AuthService> logger, AuditLogService auditLogService, PasswordPolicyService passwordPolicy)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
            _passwordPolicy = passwordPolicy;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, string ipAddress)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null || !user.IsActive)
                {
                    await _auditLogService.LogActionAsync(null, request.Username, "Login Failed", "User", null, "Invalid credentials or inactive user", ipAddress);
                    return new LoginResponse { Success = false, Message = "Invalid username or password" };
                }

                if (!user.IsApproved)
                {
                    await _auditLogService.LogActionAsync(user.Id, user.Username, "Login Failed", "User", user.Id, "Account pending admin approval", ipAddress);
                    return new LoginResponse { Success = false, Message = "Your account is pending administrator approval." };
                }

                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    await _auditLogService.LogActionAsync(user.Id, user.Username, "Login Failed", "User", user.Id, "Invalid password", ipAddress);
                    return new LoginResponse { Success = false, Message = "Invalid username or password" };
                }

                // Check TOTP — if enabled and device not pre-trusted, demand a code
                if (user.TotpEnabled && !string.IsNullOrEmpty(user.TotpSecret))
                {
                    // Store userId in session so the TOTP page can complete login
                    return new LoginResponse
                    {
                        Success       = false,
                        RequiresTotp  = true,
                        PendingUserId = user.Id,
                        Message       = "TOTP"
                    };
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _auditLogService.LogActionAsync(user.Id, user.Username, "Login Success", "User", user.Id, "User logged in successfully", ipAddress);

                var userDto = MapToUserDto(user);
                var token = GenerateToken(user);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                return new LoginResponse { Success = false, Message = "An error occurred during login" };
            }
        }

        public async Task<LoginResponse> CompleteLoginAsync(int userId, string ipAddress)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return new LoginResponse { Success = false, Message = "User not found." };

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditLogService.LogActionAsync(user.Id, user.Username, "Login Success", "User", user.Id, "Logged in (TOTP verified)", ipAddress);

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token   = GenerateToken(user),
                User    = MapToUserDto(user)
            };
        }

        public async Task<(bool Success, string Message, User? User)> RegisterUserAsync(RegisterRequest request, int createdByUserId, string createdByUsername, string ipAddress)
        {
            try
            {
                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return (false, "Username already exists", null);
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return (false, "Email already exists", null);
                }

                var (policyOk, policyMsg) = _passwordPolicy.Validate(request.Password);
                if (!policyOk) return (false, policyMsg, null);

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign roles
                if (request.RoleIds.Any())
                {
                    foreach (var roleId in request.RoleIds)
                    {
                        var userRole = new UserRole
                        {
                            UserId = user.Id,
                            RoleId = roleId
                        };
                        _context.UserRoles.Add(userRole);
                    }
                    await _context.SaveChangesAsync();
                }

                await _auditLogService.LogActionAsync(createdByUserId, createdByUsername, "User Created", "User", user.Id, $"User {user.Username} created", ipAddress);

                return (true, "User registered successfully", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Username}", request.Username);
                return (false, "An error occurred during registration", null);
            }
        }

        public async Task<(bool Success, string Message, User? User)> SelfRegisterAsync(SelfRegisterRequest request, string ipAddress)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                    return (false, "Username is already taken.", null);

                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                    return (false, "An account with this email already exists.", null);

                var (policyOk, policyMsg) = _passwordPolicy.Validate(request.Password);
                if (!policyOk) return (false, policyMsg, null);

                var user = new User
                {
                    Username  = request.Username,
                    Email     = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    IsActive  = false,   // cannot log in until admin approves
                    IsApproved = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await _auditLogService.LogActionAsync(null, request.Username, "Self Registration", "User", user.Id,
                    $"Self-registration request by {request.Username} ({request.Email}) — pending approval", ipAddress);

                return (true, "Your registration request has been submitted. An administrator will review it shortly.", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in self-registration for {Username}", request.Username);
                return (false, "An error occurred. Please try again.", null);
            }
        }

        public async Task<bool> ApproveUserAsync(int userId, int roleId, int approvedByUserId, string approvedByUsername, string ipAddress)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.IsApproved = true;
                user.IsActive   = true;

                var existingRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId);
                if (existingRole == null)
                {
                    _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
                }

                await _context.SaveChangesAsync();
                await _auditLogService.LogActionAsync(approvedByUserId, approvedByUsername, "User Approved", "User", userId,
                    $"Approved user {user.Username} with roleId {roleId}", ipAddress);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user {UserId}", userId);
                return false;
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user != null ? MapToUserDto(user) : null;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            return users.Select(MapToUserDto).ToList();
        }

        public async Task<bool> UpdateUserRolesAsync(int userId, List<int> roleIds, int updatedByUserId, string updatedByUsername, string ipAddress)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) return false;

                // Remove existing roles
                _context.UserRoles.RemoveRange(user.UserRoles);

                // Add new roles
                foreach (var roleId in roleIds)
                {
                    var userRole = new UserRole
                    {
                        UserId = userId,
                        RoleId = roleId
                    };
                    _context.UserRoles.Add(userRole);
                }

                await _context.SaveChangesAsync();
                await _auditLogService.LogActionAsync(updatedByUserId, updatedByUsername, "User Roles Updated", "User", userId, $"Roles updated for user {user.Username}", ipAddress);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user roles for userId: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string newPassword, int changedByUserId, string changedByUsername, string ipAddress)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();

                await _auditLogService.LogActionAsync(changedByUserId, changedByUsername, "Password Changed", "User", userId, $"Password changed for user {user.Username}", ipAddress);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for userId: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId, int deactivatedByUserId, string deactivatedByUsername, string ipAddress)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.IsActive = false;
                await _context.SaveChangesAsync();

                await _auditLogService.LogActionAsync(deactivatedByUserId, deactivatedByUsername, "User Deactivated", "User", userId, $"User {user.Username} deactivated", ipAddress);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<User>> GetPendingUsersAsync()
        {
            return await _context.Users
                .Where(u => !u.IsApproved)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ActivateUserAsync(int userId, int changedByUserId, string changedByUsername, string ipAddress)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.IsActive = true;
                await _context.SaveChangesAsync();

                await _auditLogService.LogActionAsync(changedByUserId, changedByUsername, "User Activated", "User", userId, $"User {user.Username} activated", ipAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> SetTotpRequiredAsync(int userId, bool required, int changedByUserId, string changedByUsername, string ipAddress)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.TotpRequired = required;

                // If forcing 2FA, also wipe existing TOTP so the user must re-enrol
                if (required && user.TotpEnabled)
                {
                    user.TotpSecret  = null;
                    user.TotpEnabled = false;
                }

                await _context.SaveChangesAsync();

                var action = required ? "2FA Required" : "2FA Not Required";
                await _auditLogService.LogActionAsync(changedByUserId, changedByUsername, action, "User", userId,
                    $"TotpRequired set to {required} for user {user.Username}", ipAddress);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting TotpRequired for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> AdminResetPasswordAsync(int userId, string newPassword, int changedByUserId, string changedByUsername, string ipAddress)
        {
            return await ChangePasswordAsync(userId, newPassword, changedByUserId, changedByUsername, ipAddress);
        }

        public async Task<bool> DeleteUserAsync(int userId, int deletedByUserId, string deletedByUsername, string ipAddress)
        {
            try
            {
                var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return false;

                _context.UserRoles.RemoveRange(user.UserRoles);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await _auditLogService.LogActionAsync(deletedByUserId, deletedByUsername, "User Deleted", "User", userId, $"User {user.Username} permanently deleted", ipAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return false;
            }
        }

        public async Task<(bool Success, string Token)> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null) return (false, string.Empty);

            var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();
            return (true, token);
        }

        public async Task<(bool Success, string Message)> ResetPasswordWithTokenAsync(string email, string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpiry < DateTime.UtcNow)
                return (false, "Invalid or expired reset link.");

            var (policyOk, policyMsg) = _passwordPolicy.Validate(newPassword);
            if (!policyOk) return (false, policyMsg);

            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            await _context.SaveChangesAsync();
            await _auditLogService.LogActionAsync(user.Id, user.Username, "Password Reset", "User", user.Id, "Password reset via email token", "system");
            return (true, "Password reset successfully.");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPasswordPublic(string password, string hash) => VerifyPassword(password, hash);

        public string HashPasswordPublic(string password) => HashPassword(password);

        public async Task<User?> GetRawUserByIdAsync(int userId)
            => await _context.Users.FindAsync(userId);

        private bool VerifyPassword(string password, string passwordHash)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == passwordHash;
        }

        private string GenerateToken(User user)
        {
            // Simple token generation (in production, use JWT)
            var tokenData = $"{user.Id}:{user.Username}:{DateTime.UtcNow.Ticks}";
            var tokenBytes = Encoding.UTF8.GetBytes(tokenData);
            return Convert.ToBase64String(tokenBytes);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                Permissions = user.UserRoles.Select(ur => ur.Role.Permission.ToString()).Distinct().ToList()
            };
        }
    }
}
