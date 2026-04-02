using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Force-applies all pending EF migrations.
        /// Protected by a deploy secret — only callable from the pipeline.
        /// </summary>
        [HttpPost("migrate")]
        public async Task<IActionResult> RunMigrations([FromHeader(Name = "X-Deploy-Secret")] string? secret)
        {
            var expected = _configuration["DeploySecret"];
            if (string.IsNullOrEmpty(expected) || secret != expected)
                return Unauthorized(new { error = "Invalid or missing deploy secret." });

            try
            {
                var pending = (await _context.Database.GetPendingMigrationsAsync()).ToList();
                _logger.LogInformation("Pending migrations: {Count} — {Names}", pending.Count, string.Join(", ", pending));

                await _context.Database.MigrateAsync();

                var applied = (await _context.Database.GetAppliedMigrationsAsync()).ToList();
                _logger.LogInformation("Migrations applied successfully. Total applied: {Count}", applied.Count);

                return Ok(new
                {
                    success = true,
                    pendingApplied = pending,
                    totalApplied = applied.Count,
                    message = pending.Count == 0 ? "No pending migrations." : $"Applied {pending.Count} migration(s)."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed via admin endpoint.");
                return StatusCode(500, new { error = ex.Message, type = ex.GetType().Name });
            }
        }
    }
}
