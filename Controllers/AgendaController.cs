using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Attributes;
using AdminMembers.Services;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/agenda")]
    public class AgendaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly ILogger<AgendaController> _logger;

        public AgendaController(ApplicationDbContext context, AuditLogService auditLogService, ILogger<AgendaController> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // GET /api/agenda?year=2026&month=4
        [HttpGet]
        [RequirePermission(Permission.Read, ResourceType.Agenda)]
        public async Task<IActionResult> GetAll([FromQuery] int? year, [FromQuery] int? month)
        {
            var now = DateTime.UtcNow;
            var y = year ?? now.Year;
            var m = month ?? now.Month;

            // Return events that overlap with the calendar view (include buffer for edge days)
            var rangeStart = new DateTime(y, m, 1).AddDays(-7);
            var rangeEnd = new DateTime(y, m, 1).AddMonths(1).AddDays(7);

            var events = await _context.AgendaEvents
                .AsNoTracking()
                .Where(e => e.StartDate < rangeEnd && (e.EndDate ?? e.StartDate) >= rangeStart)
                .OrderBy(e => e.StartDate)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    StartDate = e.StartDate.ToString("yyyy-MM-dd"),
                    StartTime = e.IsAllDay ? (string?)null : e.StartDate.ToString("HH:mm"),
                    EndDate = e.EndDate.HasValue ? e.EndDate.Value.ToString("yyyy-MM-dd") : null,
                    EndTime = (!e.IsAllDay && e.EndDate.HasValue) ? e.EndDate.Value.ToString("HH:mm") : null,
                    e.Location,
                    e.IsAllDay,
                    e.Color,
                    e.CreatedByUsername,
                    CreatedAt = e.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(events);
        }

        // POST /api/agenda
        [HttpPost]
        [RequirePermission(Permission.Write, ResourceType.Agenda)]
        public async Task<IActionResult> Create([FromBody] AgendaEventDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { error = "Title is required." });

            var userId = int.Parse(Request.Headers["X-User-Id"]!);
            var username = Request.Headers["X-Username"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var startDate = dto.StartDate.Date;
            if (!dto.IsAllDay && !string.IsNullOrEmpty(dto.StartTime) && TimeSpan.TryParse(dto.StartTime, out var st))
                startDate = startDate.Add(st);

            DateTime? endDate = dto.EndDate?.Date;
            if (!dto.IsAllDay && endDate.HasValue && !string.IsNullOrEmpty(dto.EndTime) && TimeSpan.TryParse(dto.EndTime, out var et))
                endDate = endDate.Value.Add(et);

            var ev = new AgendaEvent
            {
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                StartDate = startDate,
                EndDate = endDate,
                Location = dto.Location?.Trim(),
                IsAllDay = dto.IsAllDay,
                Color = dto.Color ?? "#6366f1",
                CreatedByUserId = userId,
                CreatedByUsername = username,
                CreatedAt = DateTime.UtcNow
            };

            _context.AgendaEvents.Add(ev);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(userId, username,
                "Event Created", "AgendaEvent", ev.Id,
                $"Created event '{ev.Title}' on {ev.StartDate:yyyy-MM-dd}", ip);

            return Ok(new { ev.Id, ev.Title, StartDate = ev.StartDate.ToString("yyyy-MM-dd") });
        }

        // PUT /api/agenda/{id}
        [HttpPut("{id}")]
        [RequirePermission(Permission.Write, ResourceType.Agenda)]
        public async Task<IActionResult> Update(int id, [FromBody] AgendaEventDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { error = "Title is required." });

            var ev = await _context.AgendaEvents.FindAsync(id);
            if (ev == null) return NotFound(new { error = "Event not found." });

            var userId = int.Parse(Request.Headers["X-User-Id"]!);
            var username = Request.Headers["X-Username"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var startDate = dto.StartDate.Date;
            if (!dto.IsAllDay && !string.IsNullOrEmpty(dto.StartTime) && TimeSpan.TryParse(dto.StartTime, out var st))
                startDate = startDate.Add(st);

            DateTime? endDate = dto.EndDate?.Date;
            if (!dto.IsAllDay && endDate.HasValue && !string.IsNullOrEmpty(dto.EndTime) && TimeSpan.TryParse(dto.EndTime, out var et))
                endDate = endDate.Value.Add(et);

            ev.Title = dto.Title.Trim();
            ev.Description = dto.Description?.Trim();
            ev.StartDate = startDate;
            ev.EndDate = endDate;
            ev.Location = dto.Location?.Trim();
            ev.IsAllDay = dto.IsAllDay;
            ev.Color = dto.Color ?? ev.Color;
            ev.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(userId, username,
                "Event Updated", "AgendaEvent", ev.Id,
                $"Updated event '{ev.Title}'", ip);

            return Ok(new { ev.Id });
        }

        // DELETE /api/agenda/{id}
        [HttpDelete("{id}")]
        [RequirePermission(Permission.Write, ResourceType.Agenda)]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.AgendaEvents.FindAsync(id);
            if (ev == null) return NotFound(new { error = "Event not found." });

            var userId = int.Parse(Request.Headers["X-User-Id"]!);
            var username = Request.Headers["X-Username"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _context.AgendaEvents.Remove(ev);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(userId, username,
                "Event Deleted", "AgendaEvent", ev.Id,
                $"Deleted event '{ev.Title}'", ip);

            return Ok(new { success = true });
        }

        public class AgendaEventDto
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public DateTime StartDate { get; set; }
            public string? StartTime { get; set; }
            public DateTime? EndDate { get; set; }
            public string? EndTime { get; set; }
            public string? Location { get; set; }
            public bool IsAllDay { get; set; } = true;
            public string? Color { get; set; }
        }
    }
}
