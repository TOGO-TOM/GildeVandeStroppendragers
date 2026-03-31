using AdminMembers.Data;
using AdminMembers.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminMembers.Services
{
    public class AuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActionAsync(int? userId, string username, string action, string entityType, int? entityId, string details, string ipAddress)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Username = username,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Audit: {Username} performed {Action} on {EntityType} {EntityId} from {IpAddress}", 
                    username, action, entityType, entityId, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit action");
            }
        }

        public async Task<List<AuditLog>> GetLogsAsync(int pageNumber = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .OrderByDescending(al => al.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetLogsByUserAsync(int userId, int pageNumber = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetLogsByEntityAsync(string entityType, int? entityId, int pageNumber = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .Where(al => al.EntityType == entityType && (entityId == null || al.EntityId == entityId))
                .OrderByDescending(al => al.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalLogsCountAsync()
        {
            return await _context.AuditLogs.CountAsync();
        }

        public async Task<(List<AuditLog> Logs, int TotalCount)> GetFilteredLogsAsync(
            string? username, string? action, string? entityType, int page, int pageSize)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
                query = query.Where(l => l.Username.Contains(username));
            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(l => l.Action.Contains(action));
            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(l => l.EntityType == entityType);

            var total = await query.CountAsync();
            var logs  = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, total);
        }
    }
}
