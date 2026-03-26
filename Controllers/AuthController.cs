using Microsoft.AspNetCore.Mvc;
using AdminMembers.Models;
using AdminMembers.Services;
using AdminMembers.Attributes;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var response = await _authService.LoginAsync(request, ipAddress);

            if (!response.Success)
            {
                return Unauthorized(new { message = response.Message });
            }

            return Ok(response);
        }

        [HttpPost("register")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            var userId = int.Parse(Request.Headers["X-User-Id"].ToString());
            var username = Request.Headers["X-Username"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var (success, message, user) = await _authService.RegisterUserAsync(request, userId, username, ipAddress);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message, userId = user?.Id });
        }

        [HttpGet("users")]
        [RequirePermission(Permission.Read)]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("users/{id}")]
        [RequirePermission(Permission.Read)]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(user);
        }

        [HttpPut("users/{id}/roles")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> UpdateUserRoles(int id, [FromBody] List<int> roleIds)
        {
            var userId = int.Parse(Request.Headers["X-User-Id"].ToString());
            var username = Request.Headers["X-Username"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var success = await _authService.UpdateUserRolesAsync(id, roleIds, userId, username, ipAddress);

            if (!success)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "User roles updated successfully" });
        }

        [HttpPut("users/{id}/password")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] string newPassword)
        {
            var userId = int.Parse(Request.Headers["X-User-Id"].ToString());
            var username = Request.Headers["X-Username"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var success = await _authService.ChangePasswordAsync(id, newPassword, userId, username, ipAddress);

            if (!success)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPut("users/{id}/deactivate")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var userId = int.Parse(Request.Headers["X-User-Id"].ToString());
            var username = Request.Headers["X-Username"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var success = await _authService.DeactivateUserAsync(id, userId, username, ipAddress);

            if (!success)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "User deactivated successfully" });
        }
    }
}
