using Microsoft.AspNetCore.Mvc;
using AdminMembers.Models;
using AdminMembers.Services;
using AdminMembers.Data;
using AdminMembers.Attributes;
using Microsoft.EntityFrameworkCore;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(ApplicationDbContext context, AuditLogService auditLogService, ILogger<RolesController> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission(Permission.Read)]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _context.Roles.ToListAsync();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        [RequirePermission(Permission.Read)]
        public async Task<IActionResult> GetRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }
            return Ok(role);
        }

        [HttpPost]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> CreateRole([FromBody] Role role)
        {
            if (string.IsNullOrWhiteSpace(role.Name))
            {
                return BadRequest(new { message = "Role name is required" });
            }

            if (await _context.Roles.AnyAsync(r => r.Name == role.Name))
            {
                return BadRequest(new { message = "Role name already exists" });
            }

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var userId = int.Parse(Request.Headers["X-User-Id"].ToString());
            var username = Request.Headers["X-Username"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _auditLogService.LogActionAsync(userId, username, "Role Created", "Role", role.Id, $"Role {role.Name} created", ipAddress);

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }

        [HttpPut("{id}")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] Role role)
        {
            if (id != role.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existingRole = await _context.Roles.FindAsync(id);
            if (existingRole == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            existingRole.Name = role.Name;
            existingRole.Description = role.Description;
            existingRole.Permission = role.Permission;

            await _context.SaveChangesAsync();

            var userId = int.Parse(Request.Headers["X-User-Id"].ToString());
            var username = Request.Headers["X-Username"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _auditLogService.LogActionAsync(userId, username, "Role Updated", "Role", role.Id, $"Role {role.Name} updated", ipAddress);

            return Ok(existingRole);
        }

        [HttpDelete("{id}")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            // Check if role is in use
            if (await _context.UserRoles.AnyAsync(ur => ur.RoleId == id))
            {
                return BadRequest(new { message = "Cannot delete role that is assigned to users" });
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            var userId = int.Parse(Request.Headers["X-User-Id"].ToString());
            var username = Request.Headers["X-Username"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await _auditLogService.LogActionAsync(userId, username, "Role Deleted", "Role", id, $"Role {role.Name} deleted", ipAddress);

            return Ok(new { message = "Role deleted successfully" });
        }
    }
}
