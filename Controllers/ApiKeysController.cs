using Microsoft.AspNetCore.Mvc;
using AdminMembers.Models;
using AdminMembers.Services;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/admin/apikeys")]
    public class ApiKeysController : ControllerBase
    {
        private readonly ApiKeyService _apiKeyService;
        private readonly AuditLogService _auditLogService;

        public ApiKeysController(ApiKeyService apiKeyService, AuditLogService auditLogService)
        {
            _apiKeyService = apiKeyService;
            _auditLogService = auditLogService;
        }

        private bool IsSuperAdmin()
        {
            var roles = Request.Headers.TryGetValue("X-User-Roles", out var rolesValue)
                ? rolesValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();
            return roles.Contains("Super Admin");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!IsSuperAdmin())
                return StatusCode(403, new { message = "Super Admin access required" });

            var keys = await _apiKeyService.GetAllKeysAsync();
            var result = keys.Select(k => new
            {
                k.Id,
                k.Name,
                KeyPrefix = k.KeyPrefix + "****",
                Permission = k.Permission.ToString(),
                k.IsActive,
                k.CreatedAt,
                k.ExpiresAt,
                k.CreatedByUserId
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateApiKeyRequest req)
        {
            if (!IsSuperAdmin())
                return StatusCode(403, new { message = "Super Admin access required" });

            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { error = "Name is required" });

            if (req.Name.Trim().Length > 100)
                return BadRequest(new { error = "Name must be 100 characters or less" });

            if (req.ExpiresAt.HasValue && req.ExpiresAt.Value <= DateTime.UtcNow)
                return BadRequest(new { error = "ExpiresAt must be in the future" });

            var permission = req.Permission?.ToLower() == "readwrite"
                ? ApiKeyPermission.ReadWrite
                : ApiKeyPermission.Read;

            var userId = int.TryParse(Request.Headers["X-User-Id"], out var uid) ? uid : 0;
            var (apiKey, rawKey) = await _apiKeyService.CreateApiKeyAsync(req.Name.Trim(), permission, userId, req.ExpiresAt);

            var username = Request.Headers["X-Username"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogActionAsync(userId, username, "ApiKey Created", "ApiKey", apiKey.Id, $"Created API key: {apiKey.Name} ({permission})", ip);

            return Ok(new
            {
                apiKey.Id,
                apiKey.Name,
                Key = rawKey,
                Permission = apiKey.Permission.ToString(),
                apiKey.IsActive,
                apiKey.CreatedAt,
                apiKey.ExpiresAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Revoke(int id)
        {
            if (!IsSuperAdmin())
                return StatusCode(403, new { message = "Super Admin access required" });

            var result = await _apiKeyService.RevokeKeyAsync(id);
            if (!result)
                return NotFound(new { error = "API key not found" });

            var userId = int.TryParse(Request.Headers["X-User-Id"], out var uid) ? uid : 0;
            var username = Request.Headers["X-Username"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogActionAsync(userId, username, "ApiKey Revoked", "ApiKey", id, $"Revoked API key #{id}", ip);

            return Ok(new { success = true });
        }

        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsSuperAdmin())
                return StatusCode(403, new { message = "Super Admin access required" });

            var result = await _apiKeyService.DeleteKeyAsync(id);
            if (!result)
                return NotFound(new { error = "API key not found" });

            var userId = int.TryParse(Request.Headers["X-User-Id"], out var uid) ? uid : 0;
            var username = Request.Headers["X-Username"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogActionAsync(userId, username, "ApiKey Deleted", "ApiKey", id, $"Permanently deleted API key #{id}", ip);

            return Ok(new { success = true });
        }
    }

    public class CreateApiKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Permission { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
