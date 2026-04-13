using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Services;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/feature-requests")]
    public class FeatureRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FeatureRequestService _featureRequestService;
        private readonly ILogger<FeatureRequestController> _logger;

        public FeatureRequestController(ApplicationDbContext context, FeatureRequestService featureRequestService, ILogger<FeatureRequestController> logger)
        {
            _context = context;
            _featureRequestService = featureRequestService;
            _logger = logger;
        }

        private bool IsSuperAdmin()
        {
            var roles = HttpContext.Request.Headers
                .TryGetValue("X-User-Roles", out var rolesValue)
                ? rolesValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();
            return roles.Contains("Super Admin");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!IsSuperAdmin()) return StatusCode(403, new { error = "Forbidden" });

            var requests = await _context.FeatureRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!IsSuperAdmin()) return StatusCode(403, new { error = "Forbidden" });

            var request = await _context.FeatureRequests.FindAsync(id);
            if (request == null) return NotFound();

            return Ok(request);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFeatureRequestDto dto)
        {
            if (!IsSuperAdmin()) return StatusCode(403, new { error = "Forbidden" });

            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { error = "Title and description are required." });

            // Read user info from middleware headers
            var userId = int.TryParse(HttpContext.Request.Headers["X-User-Id"], out var uid) ? uid : 0;
            var username = HttpContext.Request.Headers["X-User-Name"].ToString();

            var request = new FeatureRequest
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Status = "Pending",
                SubmittedByUserId = userId,
                SubmittedByUsername = username
            };

            _context.FeatureRequests.Add(request);
            await _context.SaveChangesAsync();

            // Trigger GitHub Action for AI analysis
            var (triggered, triggerError) = await _featureRequestService.TriggerAnalysisAsync(request);

            return Ok(new
            {
                success = true,
                request.Id,
                request.Status,
                actionTriggered = triggered,
                triggerError = triggerError
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsSuperAdmin()) return StatusCode(403, new { error = "Forbidden" });

            var request = await _context.FeatureRequests.FindAsync(id);
            if (request == null) return NotFound();

            _context.FeatureRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("{id}/retrigger")]
        public async Task<IActionResult> Retrigger(int id)
        {
            if (!IsSuperAdmin()) return StatusCode(403, new { error = "Forbidden" });

            var request = await _context.FeatureRequests.FindAsync(id);
            if (request == null) return NotFound();

            if (request.Status != "Pending")
                return BadRequest(new { error = "Only pending requests can be retriggered." });

            var (triggered, triggerError) = await _featureRequestService.TriggerAnalysisAsync(request);
            if (!triggered)
                return StatusCode(502, new { error = triggerError ?? "Failed to trigger AI analysis." });

            return Ok(new { success = true, actionTriggered = true });
        }

        /// <summary>
        /// Webhook callback from GitHub Actions to post analysis results.
        /// Secured by a shared secret in the X-Webhook-Secret header.
        /// </summary>
        [HttpPost("{id}/analysis-result")]
        public async Task<IActionResult> ReceiveAnalysisResult(int id, [FromBody] AnalysisResultDto dto, [FromHeader(Name = "X-Webhook-Secret")] string? webhookSecret)
        {
            if (string.IsNullOrEmpty(webhookSecret))
                return StatusCode(403, new { error = "Missing X-Webhook-Secret header" });

            var isValid = false;

            // Check 1: dedicated environment variable / app setting
            var envSecret = Environment.GetEnvironmentVariable("FEATURE_REQUEST_WEBHOOK_SECRET");
            if (!string.IsNullOrEmpty(envSecret) && webhookSecret == envSecret)
            {
                isValid = true;
            }

            // Check 2: match against stored GitHub token (first 16 chars or full)
            if (!isValid)
            {
                var settings = await _context.AiSettings.FirstOrDefaultAsync();
                if (settings != null && !string.IsNullOrEmpty(settings.GitHubToken))
                {
                    var shortSecret = settings.GitHubToken.Length >= 16
                        ? settings.GitHubToken[..16]
                        : settings.GitHubToken;

                    if (webhookSecret == shortSecret || webhookSecret == settings.GitHubToken)
                        isValid = true;
                }
            }

            if (!isValid)
            {
                _logger.LogWarning("Webhook secret validation failed. EnvVar set: {EnvSet}, Header length: {HeaderLen}",
                    !string.IsNullOrEmpty(envSecret), webhookSecret?.Length ?? 0);
                return StatusCode(403, new { error = "Invalid webhook secret" });
            }

            var request = await _context.FeatureRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.AnalysisSummary = dto.AnalysisSummary;
            request.Category = dto.Category;
            request.Priority = dto.Priority;
            request.FeasibilityAssessment = dto.FeasibilityAssessment;
            request.ArchitecturalPlan = dto.ArchitecturalPlan;
            request.AnalyzedAt = DateTime.UtcNow;
            request.PlannedAt = !string.IsNullOrEmpty(dto.ArchitecturalPlan) ? DateTime.UtcNow : null;
            request.Status = !string.IsNullOrEmpty(dto.ArchitecturalPlan) ? "Planned" : "Analyzed";

            await _context.SaveChangesAsync();
            _logger.LogInformation("Analysis result received for feature request {Id}. Status: {Status}", id, request.Status);

            return Ok(new { success = true });
        }

        /// <summary>
        /// AI Settings management (Super Admin only)
        /// </summary>
        [HttpGet("/api/settings/ai")]
        public async Task<IActionResult> GetAiSettings()
        {
            if (!IsSuperAdmin()) return StatusCode(403, new { error = "Forbidden" });

            var settings = await _context.AiSettings.FirstOrDefaultAsync();
            if (settings == null)
                return Ok(new { GitHubOwner = "", GitHubRepo = "", GitHubToken = "" });

            return Ok(new
            {
                settings.GitHubOwner,
                settings.GitHubRepo,
                GitHubToken = !string.IsNullOrEmpty(settings.GitHubToken) ? "••••••••" : ""
            });
        }

        [HttpPost("/api/settings/ai")]
        public async Task<IActionResult> UpdateAiSettings([FromBody] AiSettingsDto dto)
        {
            if (!IsSuperAdmin()) return StatusCode(403, new { error = "Forbidden" });

            try
            {
                var settings = await _context.AiSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new AiSettings();
                    _context.AiSettings.Add(settings);
                }

                settings.GitHubOwner = dto.GitHubOwner;
                settings.GitHubRepo = dto.GitHubRepo;

                if (!string.IsNullOrEmpty(dto.GitHubToken) && dto.GitHubToken != "••••••••")
                    settings.GitHubToken = dto.GitHubToken;

                settings.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AI settings");
                return StatusCode(500, new { error = "Failed to save AI settings" });
            }
        }
    }

    public class CreateFeatureRequestDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    public class AnalysisResultDto
    {
        public string? AnalysisSummary { get; set; }
        public string? Category { get; set; }
        public string? Priority { get; set; }
        public string? FeasibilityAssessment { get; set; }
        public string? ArchitecturalPlan { get; set; }
    }

    public class AiSettingsDto
    {
        public string? GitHubOwner { get; set; }
        public string? GitHubRepo { get; set; }
        public string? GitHubToken { get; set; }
    }
}
