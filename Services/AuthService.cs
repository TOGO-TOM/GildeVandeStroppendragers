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

        public AuthService(ApplicationDbContext context, ILogger<AuthService> logger, AuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
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

                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    await _auditLogService.LogActionAsync(user.Id, user.Username, "Login Failed", "User", user.Id, "Invalid password", ipAddress);
                    return new LoginResponse { Success = false, Message = "Invalid username or password" };
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

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

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
