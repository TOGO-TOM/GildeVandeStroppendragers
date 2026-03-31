using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Services;

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
                    _logger.LogWarning($"Logo blob {settings.LogoBlobName} not found in blob storage, falling back to database");
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
                                    _logger.LogWarning(ex, $"Failed to delete old logo blob {settings.LogoBlobName}");
                                }
                            }

                            settings.LogoBlobName = blobName;
                            settings.LogoFileName = logo.FileName;
                            settings.LogoContentType = logo.ContentType;
                            // Keep database copy as fallback
                            settings.LogoData = logoData;
                            
                            _logger.LogInformation($"Logo uploaded to blob storage: {blobName}");
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
                            _logger.LogInformation($"Deleted logo blob {settings.LogoBlobName}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to delete logo blob {settings.LogoBlobName}");
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
    }
}
