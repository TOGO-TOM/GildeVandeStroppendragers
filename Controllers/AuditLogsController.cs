using Microsoft.AspNetCore.Mvc;
using AdminMembers.Models;
using AdminMembers.Services;
using AdminMembers.Attributes;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : ControllerBase
    {
        private readonly AuditLogService _auditLogService;

        public AuditLogsController(AuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> GetLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            if (pageSize > 100) pageSize = 100;

            var logs = await _auditLogService.GetLogsAsync(pageNumber, pageSize);
            var totalCount = await _auditLogService.GetTotalLogsCountAsync();

            return Ok(new
            {
                logs,
                pageNumber,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpGet("user/{userId}")]
        [RequirePermission(Permission.Read)]
        public async Task<IActionResult> GetLogsByUser(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            if (pageSize > 100) pageSize = 100;

            var logs = await _auditLogService.GetLogsByUserAsync(userId, pageNumber, pageSize);
            return Ok(logs);
        }

        [HttpGet("entity/{entityType}")]
        [RequirePermission(Permission.Read)]
        public async Task<IActionResult> GetLogsByEntity(string entityType, [FromQuery] int? entityId = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            if (pageSize > 100) pageSize = 100;

            var logs = await _auditLogService.GetLogsByEntityAsync(entityType, entityId, pageNumber, pageSize);
            return Ok(logs);
        }
    }
}
