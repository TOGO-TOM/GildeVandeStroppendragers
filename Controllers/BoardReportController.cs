using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Attributes;
using AdminMembers.Services;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/boardreports")]
    public class BoardReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly BoardReportExportService _exportService;
        private readonly BlobStorageService? _blobStorageService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BoardReportController> _logger;

        public BoardReportController(
            ApplicationDbContext context,
            AuditLogService auditLogService,
            BoardReportExportService exportService,
            IConfiguration configuration,
            ILogger<BoardReportController> logger,
            BlobStorageService? blobStorageService = null)
        {
            _context = context;
            _auditLogService = auditLogService;
            _exportService = exportService;
            _configuration = configuration;
            _logger = logger;
            _blobStorageService = blobStorageService;
        }

        // GET /api/boardreports
        [HttpGet]
        [RequirePermission(Permission.Read, ResourceType.BoardReport)]
        public async Task<IActionResult> GetAll()
        {
            var reports = await _context.BoardReports
                .AsNoTracking()
                .Include(r => r.Attendees)
                    .ThenInclude(a => a.Member)
                .OrderByDescending(r => r.MeetingDate)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    MeetingDate = r.MeetingDate.ToString("yyyy-MM-dd"),
                    r.Location,
                    r.Status,
                    r.CreatedByUsername,
                    CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd"),
                    AttendeesCount = r.Attendees.Count,
                    PresentCount = r.Attendees.Count(a => a.IsPresent)
                })
                .ToListAsync();

            return Ok(reports);
        }

        // GET /api/boardreports/{id}
        [HttpGet("{id}")]
        [RequirePermission(Permission.Read, ResourceType.BoardReport)]
        public async Task<IActionResult> GetById(int id)
        {
            var report = await _context.BoardReports
                .AsNoTracking()
                .Include(r => r.Attendees)
                    .ThenInclude(a => a.Member)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            return Ok(new
            {
                report.Id,
                report.Title,
                MeetingDate = report.MeetingDate.ToString("yyyy-MM-dd"),
                report.Location,
                report.AgendaItems,
                report.Content,
                report.Notes,
                report.Status,
                report.CreatedByUserId,
                report.CreatedByUsername,
                CreatedAt = report.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                UpdatedAt = report.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ss"),
                Attendees = report.Attendees.Select(a => new
                {
                    a.Id,
                    a.MemberId,
                    a.IsPresent,
                    MemberName = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : null
                }).ToList()
            });
        }

        // POST /api/boardreports
        [HttpPost]
        [RequirePermission(Permission.Write, ResourceType.BoardReport)]
        public async Task<IActionResult> Create([FromBody] BoardReportDto dto)
        {
            var username = Request.Headers["X-Username"].ToString();
            var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault();
            int.TryParse(userIdStr, out int userId);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var report = new BoardReport
            {
                Title = dto.Title,
                MeetingDate = DateTime.Parse(dto.MeetingDate),
                Location = dto.Location,
                AgendaItems = dto.AgendaItems,
                Content = dto.Content,
                Notes = dto.Notes,
                Status = dto.Status ?? "Draft",
                CreatedByUserId = userId,
                CreatedByUsername = username,
                CreatedAt = DateTime.UtcNow
            };

            _context.BoardReports.Add(report);
            await _context.SaveChangesAsync();

            // Add attendees
            if (dto.Attendees?.Any() == true)
            {
                foreach (var att in dto.Attendees)
                {
                    _context.BoardReportAttendees.Add(new BoardReportAttendee
                    {
                        BoardReportId = report.Id,
                        MemberId = att.MemberId,
                        IsPresent = att.IsPresent
                    });
                }
                await _context.SaveChangesAsync();
            }

            await _auditLogService.LogActionAsync(userId, username,
                "BoardReport Created", "BoardReport", report.Id,
                $"Created board report '{report.Title}'", ip);
            _logger.LogInformation("Board report {Id} created by {User}", report.Id, username);

            return Ok(new { report.Id });
        }

        // PUT /api/boardreports/{id}
        [HttpPut("{id}")]
        [RequirePermission(Permission.Write, ResourceType.BoardReport)]
        public async Task<IActionResult> Update(int id, [FromBody] BoardReportDto dto)
        {
            var report = await _context.BoardReports
                .Include(r => r.Attendees)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            var username = Request.Headers["X-Username"].ToString();
            var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault();
            int.TryParse(userIdStr, out int userId);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            report.Title = dto.Title;
            report.MeetingDate = DateTime.Parse(dto.MeetingDate);
            report.Location = dto.Location;
            report.AgendaItems = dto.AgendaItems;
            report.Content = dto.Content;
            report.Notes = dto.Notes;
            report.Status = dto.Status ?? report.Status;
            report.UpdatedAt = DateTime.UtcNow;

            // Replace attendees
            _context.BoardReportAttendees.RemoveRange(report.Attendees);
            if (dto.Attendees?.Any() == true)
            {
                foreach (var att in dto.Attendees)
                {
                    _context.BoardReportAttendees.Add(new BoardReportAttendee
                    {
                        BoardReportId = report.Id,
                        MemberId = att.MemberId,
                        IsPresent = att.IsPresent
                    });
                }
            }

            await _context.SaveChangesAsync();
            await _auditLogService.LogActionAsync(userId, username,
                "BoardReport Updated", "BoardReport", report.Id,
                $"Updated board report '{report.Title}'", ip);

            return Ok(new { report.Id });
        }

        // DELETE /api/boardreports/{id}
        [HttpDelete("{id}")]
        [RequirePermission(Permission.Write, ResourceType.BoardReport)]
        public async Task<IActionResult> Delete(int id)
        {
            var report = await _context.BoardReports.FindAsync(id);
            if (report == null) return NotFound();

            var username = Request.Headers["X-Username"].ToString();
            var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault();
            int.TryParse(userIdStr, out int userId);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _context.BoardReports.Remove(report);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(userId, username,
                "BoardReport Deleted", "BoardReport", report.Id,
                $"Deleted board report '{report.Title}'", ip);
            return Ok();
        }

        // GET /api/boardreports/{id}/export/word
        [HttpGet("{id}/export/word")]
        [RequirePermission(Permission.Read, ResourceType.BoardReport)]
        public async Task<IActionResult> ExportWord(int id)
        {
            var report = await _context.BoardReports
                .Include(r => r.Attendees).ThenInclude(a => a.Member)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            var logoData = await GetLogoDataAsync();
            var bytes = _exportService.ExportToWord(report, logoData);
            var fileName = $"Verslag_{report.MeetingDate:yyyyMMdd}_{SanitizeFileName(report.Title)}.docx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        // GET /api/boardreports/{id}/export/pdf
        [HttpGet("{id}/export/pdf")]
        [RequirePermission(Permission.Read, ResourceType.BoardReport)]
        public async Task<IActionResult> ExportPdf(int id)
        {
            var report = await _context.BoardReports
                .Include(r => r.Attendees).ThenInclude(a => a.Member)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            var logoData = await GetLogoDataAsync();
            var bytes = _exportService.ExportToPdf(report, logoData);
            var fileName = $"Verslag_{report.MeetingDate:yyyyMMdd}_{SanitizeFileName(report.Title)}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        private async Task<byte[]?> GetLogoDataAsync()
        {
            try
            {
                var settings = await _context.AppSettings.FirstOrDefaultAsync();
                if (_blobStorageService != null && settings?.LogoBlobName != null)
                {
                    try
                    {
                        var containerName = _configuration.GetValue<string>("AzureStorageBlob:LogoContainerName") ?? "logos";
                        return await _blobStorageService.DownloadBlobAsync(containerName, settings.LogoBlobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch logo from blob storage, falling back to database");
                    }
                }
                return settings?.LogoData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch logo data");
                return null;
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Where(c => !invalid.Contains(c)).Take(50).ToArray()).Replace(' ', '_');
        }
    }

    // DTOs
    public class BoardReportDto
    {
        public string Title { get; set; } = string.Empty;
        public string MeetingDate { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? AgendaItems { get; set; }
        public string? Content { get; set; }
        public string? Notes { get; set; }
        public string? Status { get; set; }
        public List<BoardReportAttendeeDto>? Attendees { get; set; }
    }

    public class BoardReportAttendeeDto
    {
        public int MemberId { get; set; }
        public bool IsPresent { get; set; } = true;
    }
}
