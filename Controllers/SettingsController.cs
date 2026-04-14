using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Services;
using AdminMembers.Attributes;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly BlobStorageService? _blobStorageService;
        private readonly IConfiguration _configuration;

        public SettingsController(ApplicationDbContext context, ILogger<SettingsController> logger, IWebHostEnvironment environment, IConfiguration configuration, BlobStorageService? blobStorageService = null)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
            _blobStorageService = blobStorageService;
        }

        [HttpGet]
        public async Task<ActionResult<AppSettings>> GetSettings()
        {
            var settings = await _context.AppSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // Create default settings
                settings = new AppSettings
                {
                    CompanyName = "Member Administration",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AppSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            // Don't send logo data in GET, only metadata
            return Ok(new
            {
                settings.Id,
                settings.CompanyName,
                settings.LogoFileName,
                HasLogo = settings.LogoData != null && settings.LogoData.Length > 0
            });
        }

        [HttpGet("logo")]
        public async Task<IActionResult> GetLogo()
        {
            var settings = await _context.AppSettings.FirstOrDefaultAsync();
            
            // Try to get from blob storage first if configured
            if (_blobStorageService != null && settings?.LogoBlobName != null)
            {
                try
                {
                    var containerName = _configuration.GetValue<string>("AzureStorageBlob:LogoContainerName") ?? "logos";
                    var logoData = await _blobStorageService.DownloadBlobAsync(containerName, settings.LogoBlobName);
                    return File(logoData, settings.LogoContentType ?? "image/png");
                }
                catch (FileNotFoundException)
                {
                    _logger.LogWarning("Logo blob {BlobName} not found in blob storage, falling back to database", settings.LogoBlobName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving logo from blob storage, falling back to database");
                }
            }

            // Fallback to database
            if (settings?.LogoData == null || settings.LogoData.Length == 0)
            {
                return NotFound();
            }

            return File(settings.LogoData, settings.LogoContentType ?? "image/png");
        }

        [HttpPost]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> UpdateSettings([FromForm] string? companyName, [FromForm] IFormFile? logo)
        {
            try
            {
                var settings = await _context.AppSettings.FirstOrDefaultAsync();
                
                if (settings == null)
                {
                    settings = new AppSettings();
                    _context.AppSettings.Add(settings);
                }

                if (!string.IsNullOrWhiteSpace(companyName))
                {
                    settings.CompanyName = companyName;
                }

                if (logo != null && logo.Length > 0)
                {
                    // Validate file type
                    var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/gif" };
                    if (!allowedTypes.Contains(logo.ContentType.ToLower()))
                    {
                        return BadRequest(new { error = "Only image files (PNG, JPEG, GIF) are allowed." });
                    }

                    // Validate file size (max 2MB)
                    if (logo.Length > 2 * 1024 * 1024)
                    {
                        return BadRequest(new { error = "Logo file size must be less than 2MB." });
                    }

                    using var memoryStream = new MemoryStream();
                    await logo.CopyToAsync(memoryStream);
                    var logoData = memoryStream.ToArray();

                    // Try to upload to blob storage if configured
                    if (_blobStorageService != null)
                    {
                        try
                        {
                            var containerName = _configuration.GetValue<string>("AzureStorageBlob:LogoContainerName") ?? "logos";
                            var blobName = $"logo_{DateTime.UtcNow:yyyyMMdd_HHmmss}{Path.GetExtension(logo.FileName)}";
                            
                            await _blobStorageService.UploadBlobAsync(containerName, blobName, logoData, logo.ContentType);
                            
                            // Delete old blob if exists
                            if (!string.IsNullOrEmpty(settings.LogoBlobName))
                            {
                                try
                                {
                                    await _blobStorageService.DeleteBlobAsync(containerName, settings.LogoBlobName);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to delete old logo blob {BlobName}", settings.LogoBlobName);
                                }
                            }

                            settings.LogoBlobName = blobName;
                            settings.LogoFileName = logo.FileName;
                            settings.LogoContentType = logo.ContentType;
                            // Keep database copy as fallback
                            settings.LogoData = logoData;
                            
                            _logger.LogInformation("Logo uploaded to blob storage: {BlobName}", blobName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to upload logo to blob storage, storing in database only");
                            settings.LogoData = logoData;
                            settings.LogoFileName = logo.FileName;
                            settings.LogoContentType = logo.ContentType;
                        }
                    }
                    else
                    {
                        // Store in database if blob storage not configured
                        settings.LogoData = logoData;
                        settings.LogoFileName = logo.FileName;
                        settings.LogoContentType = logo.ContentType;
                    }
                }

                settings.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Settings updated successfully",
                    settings = new
                    {
                        settings.Id,
                        settings.CompanyName,
                        settings.LogoFileName,
                        HasLogo = settings.LogoData != null && settings.LogoData.Length > 0,
                        IsInBlobStorage = !string.IsNullOrEmpty(settings.LogoBlobName)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings");
                return StatusCode(500, new { error = "Failed to update settings" });
            }
        }

        [HttpDelete("logo")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> DeleteLogo()
        {
            try
            {
                var settings = await _context.AppSettings.FirstOrDefaultAsync();

                if (settings != null)
                {
                    // Delete from blob storage if exists
                    if (_blobStorageService != null && !string.IsNullOrEmpty(settings.LogoBlobName))
                    {
                        try
                        {
                            var containerName = _configuration.GetValue<string>("AzureStorageBlob:LogoContainerName") ?? "logos";
                            await _blobStorageService.DeleteBlobAsync(containerName, settings.LogoBlobName);
                            _logger.LogInformation("Deleted logo blob {BlobName}", settings.LogoBlobName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete logo blob {BlobName}", settings.LogoBlobName);
                        }
                    }

                    settings.LogoData = null;
                    settings.LogoFileName = null;
                    settings.LogoContentType = null;
                    settings.LogoBlobName = null;
                    settings.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, message = "Logo deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting logo");
                return StatusCode(500, new { error = "Failed to delete logo" });
            }
        }

        // Custom Fields Management
        [HttpGet("custom-fields")]
        public async Task<ActionResult<IEnumerable<CustomField>>> GetCustomFields()
        {
            try
            {
                var fields = await _context.CustomFields
                    .AsNoTracking()
                    .OrderBy(f => f.DisplayOrder)
                    .ToListAsync();

                return Ok(fields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting custom fields");
                return StatusCode(500, new { error = "Failed to load custom fields" });
            }
        }

        [HttpPost("custom-fields")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<ActionResult<CustomField>> CreateCustomField([FromBody] CustomField field)
        {
            try
            {
                // Validate field name is unique
                if (await _context.CustomFields.AnyAsync(f => f.FieldName == field.FieldName))
                {
                    return BadRequest(new { error = "A custom field with this name already exists" });
                }

                // Set display order to last
                var maxOrder = await _context.CustomFields.MaxAsync(f => (int?)f.DisplayOrder) ?? 0;
                field.DisplayOrder = maxOrder + 1;
                field.CreatedAt = DateTime.UtcNow;

                _context.CustomFields.Add(field);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCustomFields), new { id = field.Id }, field);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating custom field");
                return StatusCode(500, new { error = "Failed to create custom field" });
            }
        }

        [HttpPut("custom-fields/{id}")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> UpdateCustomField(int id, [FromBody] CustomField field)
        {
            try
            {
                if (id != field.Id)
                {
                    return BadRequest(new { error = "ID mismatch" });
                }

                var existingField = await _context.CustomFields.FindAsync(id);
                if (existingField == null)
                {
                    return NotFound();
                }

                // Check if field name is being changed and if it's unique
                if (existingField.FieldName != field.FieldName)
                {
                    if (await _context.CustomFields.AnyAsync(f => f.FieldName == field.FieldName && f.Id != id))
                    {
                        return BadRequest(new { error = "A custom field with this name already exists" });
                    }
                }

                existingField.FieldName = field.FieldName;
                existingField.FieldLabel = field.FieldLabel;
                existingField.FieldType = field.FieldType;
                existingField.IsRequired = field.IsRequired;
                existingField.IsActive = field.IsActive;
                existingField.IsFilterable = field.IsFilterable;
                existingField.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Custom field updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating custom field");
                return StatusCode(500, new { error = "Failed to update custom field" });
            }
        }

        [HttpDelete("custom-fields/{id}")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> DeleteCustomField(int id)
        {
            try
            {
                var field = await _context.CustomFields.FindAsync(id);
                if (field == null)
                {
                    return NotFound();
                }

                _context.CustomFields.Remove(field);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Custom field deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting custom field");
                return StatusCode(500, new { error = "Failed to delete custom field" });
            }
        }

        // ── Email Settings (Super Admin only) ──────────────────────────

        private bool IsSuperAdmin()
        {
            var roles = HttpContext.Request.Headers
                .TryGetValue("X-User-Roles", out var rolesValue)
                ? rolesValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();
            return roles.Contains("Super Admin");
        }

        [HttpGet("email")]
        public async Task<IActionResult> GetEmailSettings()
        {
            if (!IsSuperAdmin())
                return StatusCode(403, new { error = "Forbidden" });

            var settings = await _context.EmailSettings.FirstOrDefaultAsync();

            if (settings == null)
                return Ok(new
                {
                    Provider = "Smtp",
                    SmtpHost = "",
                    SmtpPort = 587,
                    UseSsl = true,
                    Username = "",
                    ApiKey = "",
                    FromAddress = "",
                    FromName = ""
                });

            return Ok(new
            {
                settings.Provider,
                settings.SmtpHost,
                settings.SmtpPort,
                settings.UseSsl,
                settings.Username,
                ApiKey = !string.IsNullOrEmpty(settings.ApiKey) ? "••••••••" : "",
                settings.FromAddress,
                settings.FromName
            });
        }

        [HttpPost("email")]
        public async Task<IActionResult> UpdateEmailSettings([FromBody] EmailSettingsDto dto)
        {
            if (!IsSuperAdmin())
                return StatusCode(403, new { error = "Forbidden" });

            try
            {
                var settings = await _context.EmailSettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    settings = new EmailSettings();
                    _context.EmailSettings.Add(settings);
                }

                settings.Provider    = dto.Provider ?? "Smtp";
                settings.SmtpHost    = dto.SmtpHost;
                settings.SmtpPort    = dto.SmtpPort > 0 ? dto.SmtpPort : 587;
                settings.UseSsl      = dto.UseSsl;
                settings.Username    = dto.Username;
                settings.FromAddress = dto.FromAddress;
                settings.FromName    = dto.FromName;

                // Only update API key if a real value was sent (not the masked placeholder)
                if (!string.IsNullOrEmpty(dto.ApiKey) && dto.ApiKey != "••••••••")
                    settings.ApiKey = dto.ApiKey;

                // Only update password if a real value was sent
                if (!string.IsNullOrEmpty(dto.Password) && dto.Password != "••••••••")
                    settings.Password = dto.Password;

                settings.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Email settings saved" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving email settings");
                return StatusCode(500, new { error = "Failed to save email settings" });
            }
        }

        [HttpPost("email/test")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailDto dto, [FromServices] EmailService emailService)
        {
            if (!IsSuperAdmin())
                return StatusCode(403, new { error = "Forbidden" });

            if (string.IsNullOrWhiteSpace(dto.ToEmail))
                return BadRequest(new { error = "Email address is required" });

            var success = await emailService.SendEmailAsync(
                dto.ToEmail,
                "Test",
                "Test Email",
                "<div style='font-family:sans-serif;padding:24px;'><h2>✅ Email Configuration Working</h2><p>This is a test email from your application.</p></div>");

            return success
                ? Ok(new { success = true, message = "Test email sent" })
                : StatusCode(500, new { error = "Failed to send test email. Check your email settings." });
        }
    }

    public class EmailSettingsDto
    {
        public string? Provider { get; set; }
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool UseSsl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ApiKey { get; set; }
        public string? FromAddress { get; set; }
        public string? FromName { get; set; }
    }

    public class TestEmailDto
    {
        public string? ToEmail { get; set; }
    }
}
