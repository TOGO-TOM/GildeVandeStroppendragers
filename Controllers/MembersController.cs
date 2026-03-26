using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Services;
using System.Text;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MembersController> _logger;
        private readonly BackupService _backupService;
        private readonly ExportService _exportService;

        public MembersController(ApplicationDbContext context, ILogger<MembersController> logger, BackupService backupService, ExportService exportService)
        {
            _context = context;
            _logger = logger;
            _backupService = backupService;
            _exportService = exportService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Member>>> GetMembers([FromQuery] string? role = null)
        {
            try
            {
                _logger.LogInformation("GetMembers called");
                var query = _context.Members.Include(m => m.Address).AsQueryable();

                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(m => m.Role == role);
                }

                var members = await query.ToListAsync();
                _logger.LogInformation($"Returning {members.Count} members");
                return members;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMembers");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("debug")]
        public async Task<ActionResult> GetDebugInfo()
        {
            try
            {
                var memberCount = await _context.Members.CountAsync();
                var addressCount = await _context.Addresses.CountAsync();
                var canConnect = await _context.Database.CanConnectAsync();
                
                return Ok(new
                {
                    databaseConnected = canConnect,
                    memberCount = memberCount,
                    addressCount = addressCount,
                    connectionString = _context.Database.GetConnectionString()?.Replace("Password=", "Password=***"),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Member>> GetMember(int id)
        {
            var member = await _context.Members
                .Include(m => m.Address)
                .Include(m => m.CustomFieldValues)
                    .ThenInclude(cf => cf.CustomField)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
            {
                return NotFound();
            }

            return member;
        }

        [HttpGet("check-number/{memberNumber}")]
        public async Task<ActionResult<bool>> CheckMemberNumber(int memberNumber, [FromQuery] int? excludeId = null)
        {
            var exists = await _context.Members
                .AnyAsync(m => m.MemberNumber == memberNumber && (excludeId == null || m.Id != excludeId));
            
            return Ok(new { exists, available = !exists });
        }

        [HttpPost]
        public async Task<ActionResult<Member>> CreateMember(Member member)
        {
            // Check if member number already exists
            if (await _context.Members.AnyAsync(m => m.MemberNumber == member.MemberNumber))
            {
                return BadRequest(new { error = $"Member number '{member.MemberNumber}' is already in use." });
            }

            member.CreatedAt = DateTime.UtcNow;
            _context.Members.Add(member);
            
            try
            {
                await _context.SaveChangesAsync();
                
                // Add custom field values if provided
                if (member.CustomFieldValues != null && member.CustomFieldValues.Any())
                {
                    foreach (var cfv in member.CustomFieldValues)
                    {
                        cfv.MemberId = member.Id;
                        cfv.CreatedAt = DateTime.UtcNow;
                        _context.MemberCustomFields.Add(cfv);
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating member");
                return StatusCode(500, new { error = "Failed to create member. Please try again." });
            }

            return CreatedAtAction(nameof(GetMember), new { id = member.Id }, member);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMember(int id, Member member)
        {
            if (id != member.Id)
            {
                return BadRequest();
            }

            // Check if member number already exists (excluding current member)
            if (await _context.Members.AnyAsync(m => m.MemberNumber == member.MemberNumber && m.Id != id))
            {
                return BadRequest(new { error = $"Member number '{member.MemberNumber}' is already in use." });
            }

            member.UpdatedAt = DateTime.UtcNow;
            _context.Entry(member).State = EntityState.Modified;

            if (member.Address != null)
            {
                if (member.Address.Id == 0)
                {
                    _context.Addresses.Add(member.Address);
                }
                else
                {
                    _context.Entry(member.Address).State = EntityState.Modified;
                }
            }

            // Handle custom field values
            if (member.CustomFieldValues != null && member.CustomFieldValues.Any())
            {
                // Remove existing custom field values
                var existingValues = await _context.MemberCustomFields
                    .Where(mcf => mcf.MemberId == id)
                    .ToListAsync();
                _context.MemberCustomFields.RemoveRange(existingValues);

                // Add new custom field values
                foreach (var cfv in member.CustomFieldValues)
                {
                    cfv.MemberId = id;
                    cfv.CreatedAt = DateTime.UtcNow;
                    _context.MemberCustomFields.Add(cfv);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MemberExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member");
                return StatusCode(500, new { error = "Failed to update member. Please try again." });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMember(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }

            _context.Members.Remove(member);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllMembers()
        {
            try
            {
                var members = await _context.Members.ToListAsync();
                var count = members.Count;

                _context.Members.RemoveRange(members);
                await _context.SaveChangesAsync();

                // Reset identity seed
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Members', RESEED, 0)");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Addresses', RESEED, 0)");

                _logger.LogInformation($"Deleted all {count} members");

                return Ok(new { success = true, deletedCount = count, message = $"Successfully deleted {count} members" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all members");
                return StatusCode(500, new { error = "Failed to delete all members" });
            }
        }

        [HttpPatch("bulk-update")]
        public async Task<IActionResult> BulkUpdateMembers([FromBody] BulkUpdateRequest request)
        {
            try
            {
                if (request.MemberIds == null || !request.MemberIds.Any())
                {
                    return BadRequest(new { error = "No member IDs provided" });
                }

                var members = await _context.Members
                    .Where(m => request.MemberIds.Contains(m.Id))
                    .ToListAsync();

                if (!members.Any())
                {
                    return NotFound(new { error = "No members found with the provided IDs" });
                }

                int updatedCount = 0;

                foreach (var member in members)
                {
                    bool updated = false;

                    if (!string.IsNullOrEmpty(request.Gender))
                    {
                        member.Gender = request.Gender;
                        updated = true;
                    }

                    if (!string.IsNullOrEmpty(request.Role))
                    {
                        member.Role = request.Role;
                        updated = true;
                    }

                    if (request.IsAlive.HasValue)
                    {
                        member.IsAlive = request.IsAlive.Value;
                        updated = true;
                    }

                    if (updated)
                    {
                        member.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bulk updated {updatedCount} members");

                return Ok(new { success = true, updatedCount, message = $"Successfully updated {updatedCount} members" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk update");
                return StatusCode(500, new { error = "Failed to update members" });
            }
        }

        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCsv()
        {
            var members = await _context.Members
                .Include(m => m.Address)
                .ToListAsync();

            var csv = new StringBuilder();

            // Add BOM for UTF-8 to ensure special characters display correctly
            csv.Append('\ufeff');

            // CSV Header
            csv.AppendLine("Member Number,First Name,Last Name,Gender,Role,Birth Date,Email,Phone Number,Status,Seniority Date,Street,House Number,City,Postal Code,Country,Created At");

            // CSV Data
            foreach (var member in members)
            {
                var status = member.IsAlive ? "Alive" : "Deceased";
                var birthDate = member.BirthDate?.ToString("yyyy-MM-dd") ?? "";
                var seniorityDate = member.SeniorityDate?.ToString("yyyy-MM-dd") ?? "";
                csv.AppendLine($"\"{member.MemberNumber}\",\"{member.FirstName}\",\"{member.LastName}\",\"{member.Gender}\",\"{member.Role}\",\"{birthDate}\",\"{member.Email ?? ""}\",\"{member.PhoneNumber ?? ""}\",\"{status}\",\"{seniorityDate}\",\"{member.Address?.Street ?? ""}\",\"{member.Address?.HouseNumber ?? ""}\",\"{member.Address?.City ?? ""}\",\"{member.Address?.PostalCode ?? ""}\",\"{member.Address?.Country ?? ""}\",\"{member.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"members_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }

        [HttpPost("export/excel")]
        public async Task<IActionResult> ExportToExcel([FromBody] ExportRequest request)
        {
            try
            {
                var query = _context.Members.Include(m => m.Address).AsQueryable();

                // Apply role filter if provided
                if (request.FilterRoles != null && request.FilterRoles.Any())
                {
                    query = query.Where(m => request.FilterRoles.Contains(m.Role));
                }

                var members = await query.ToListAsync();

                if (request.SelectedFields == null || !request.SelectedFields.Any())
                {
                    return BadRequest(new { error = "No fields selected for export" });
                }

                var excelBytes = _exportService.ExportToExcel(members, request.SelectedFields);
                var fileName = $"members_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                return StatusCode(500, new { error = "Failed to export to Excel" });
            }
        }

        [HttpPost("export/pdf")]
        public async Task<IActionResult> ExportToPdf([FromBody] ExportRequest request)
        {
            try
            {
                var query = _context.Members.Include(m => m.Address).AsQueryable();

                // Apply role filter if provided
                if (request.FilterRoles != null && request.FilterRoles.Any())
                {
                    query = query.Where(m => request.FilterRoles.Contains(m.Role));
                }

                var members = await query.ToListAsync();

                if (request.SelectedFields == null || !request.SelectedFields.Any())
                {
                    return BadRequest(new { error = "No fields selected for export" });
                }

                // Get logo from settings
                var settings = await _context.AppSettings.FirstOrDefaultAsync();
                var logoData = settings?.LogoData;
                var companyName = settings?.CompanyName;

                var pdfBytes = _exportService.ExportToPdf(members, request.SelectedFields, logoData, companyName);
                var fileName = $"members_export_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                return StatusCode(500, new { error = "Failed to export to PDF" });
            }
        }

        [HttpPost("export/csv-custom")]
        public async Task<IActionResult> ExportToCsvCustom([FromBody] ExportRequest request)
        {
            try
            {
                var query = _context.Members.Include(m => m.Address).AsQueryable();

                // Apply role filter if provided
                if (request.FilterRoles != null && request.FilterRoles.Any())
                {
                    query = query.Where(m => request.FilterRoles.Contains(m.Role));
                }

                var members = await query.ToListAsync();

                if (request.SelectedFields == null || !request.SelectedFields.Any())
                {
                    return BadRequest(new { error = "No fields selected for export" });
                }

                var csvBytes = _exportService.ExportToCsv(members, request.SelectedFields);
                var fileName = $"members_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                return StatusCode(500, new { error = "Failed to export to CSV" });
            }
        }

        [HttpGet("export/available-fields")]
        public ActionResult<object> GetAvailableFields()
        {
            var fields = new[]
            {
                new { value = "Id", label = "ID" },
                new { value = "MemberNumber", label = "Member Number" },
                new { value = "FirstName", label = "First Name" },
                new { value = "LastName", label = "Last Name" },
                new { value = "Gender", label = "Gender" },
                new { value = "Role", label = "Role" },
                new { value = "BirthDate", label = "Birth Date" },
                new { value = "Email", label = "Email" },
                new { value = "PhoneNumber", label = "Phone Number" },
                new { value = "IsAlive", label = "Status" },
                new { value = "SeniorityDate", label = "Seniority Date" },
                new { value = "Street", label = "Street" },
                new { value = "HouseNumber", label = "House Number" },
                new { value = "City", label = "City" },
                new { value = "PostalCode", label = "Postal Code" },
                new { value = "Country", label = "Country" },
                new { value = "CreatedAt", label = "Created Date" },
                new { value = "UpdatedAt", label = "Updated Date" }
            };

            return Ok(fields);
        }

        [HttpPost("backup")]
        public async Task<IActionResult> CreateBackup([FromQuery] string? password = null)
        {
            try
            {
                var encryptedBackup = await _backupService.CreateEncryptedBackup(password);
                var fileName = $"members_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                
                return File(encryptedBackup, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                return StatusCode(500, new { error = "Failed to create backup" });
            }
        }

        [HttpPost("restore")]
        public async Task<IActionResult> RestoreBackup([FromForm] IFormFile backupFile, [FromQuery] string? password = null, [FromQuery] bool overwrite = false)
        {
            try
            {
                if (backupFile == null || backupFile.Length == 0)
                {
                    return BadRequest(new { error = "No backup file provided" });
                }

                using var memoryStream = new MemoryStream();
                await backupFile.CopyToAsync(memoryStream);
                var encryptedData = memoryStream.ToArray();

                var result = await _backupService.RestoreFromEncryptedBackup(encryptedData, password, overwrite);

                return Ok(new
                {
                    success = true,
                    message = $"Restore completed successfully",
                    backupDate = result.BackupDate,
                    totalMembers = result.TotalMembers,
                    importedMembers = result.ImportedMembers,
                    skippedMembers = result.SkippedMembers
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring backup");
                return StatusCode(500, new { error = "Failed to restore backup" });
            }
        }

        [HttpPost("import/csv")]
        public async Task<IActionResult> ImportFromCsv([FromForm] IFormFile csvFile, [FromForm] string fieldMapping)
        {
            try
            {
                if (csvFile == null || csvFile.Length == 0)
                {
                    return BadRequest(new { error = "No CSV file provided" });
                }

                var mapping = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(fieldMapping);
                if (mapping == null)
                {
                    return BadRequest(new { error = "Invalid field mapping" });
                }

                using var reader = new StreamReader(csvFile.OpenReadStream(), Encoding.UTF8);
                var csvContent = await reader.ReadToEndAsync();

                var lines = csvContent.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                if (lines.Length < 2)
                {
                    return BadRequest(new { error = "CSV file must have headers and at least one data row" });
                }

                // Detect separator (comma or semicolon)
                char separator = DetectSeparator(lines[0]);
                _logger.LogInformation($"Detected CSV separator: '{separator}'");

                // Parse headers
                var headers = lines[0].Split(separator).Select(h => h.Trim().Trim('"')).ToArray();
                
                var importedCount = 0;
                var skippedCount = 0;
                var errors = new List<string>();

                // Process data rows
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i], separator);
                        if (values.Length != headers.Length)
                        {
                            _logger.LogWarning($"Row {i + 1}: Column count mismatch. Expected {headers.Length}, got {values.Length}");
                            continue;
                        }

                        var row = new Dictionary<string, string>();
                        for (int j = 0; j < headers.Length; j++)
                        {
                            row[headers[j]] = values[j];
                        }

                        // Map CSV fields to Member properties
                        var member = new Member
                        {
                            MemberNumber = int.TryParse(GetMappedValue(row, mapping, "MemberNumber"), out var memberNum) ? memberNum : 0,
                            FirstName = GetMappedValue(row, mapping, "FirstName"),
                            LastName = GetMappedValue(row, mapping, "LastName"),
                            Gender = GetMappedValue(row, mapping, "Gender") ?? "Man",
                            Role = GetMappedValue(row, mapping, "Role") ?? "Kandidaat",
                            Email = GetMappedValueOrNull(row, mapping, "Email"),
                            PhoneNumber = GetMappedValueOrNull(row, mapping, "PhoneNumber"),
                            IsAlive = ParseBoolValue(GetMappedValue(row, mapping, "IsAlive")),
                            CreatedAt = DateTime.UtcNow
                        };

                        // Parse seniority date if provided
                        var seniorityDateStr = GetMappedValue(row, mapping, "SeniorityDate");
                        if (!string.IsNullOrEmpty(seniorityDateStr) && DateTime.TryParse(seniorityDateStr, out var seniorityDate))
                        {
                            member.SeniorityDate = seniorityDate;
                        }

                        // Parse birth date if provided
                        var birthDateStr = GetMappedValue(row, mapping, "BirthDate");
                        if (!string.IsNullOrEmpty(birthDateStr) && DateTime.TryParse(birthDateStr, out var birthDate))
                        {
                            member.BirthDate = birthDate;
                        }

                        // Create address if any address fields are provided
                        var street = GetMappedValueOrNull(row, mapping, "Street");
                        var houseNumber = GetMappedValueOrNull(row, mapping, "HouseNumber");
                        var city = GetMappedValueOrNull(row, mapping, "City");
                        var postalCode = GetMappedValueOrNull(row, mapping, "PostalCode");
                        var country = GetMappedValueOrNull(row, mapping, "Country");

                        // Create address if at least street, city, and postal code are provided
                        if (!string.IsNullOrWhiteSpace(street) && !string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(postalCode))
                        {
                            member.Address = new Address
                            {
                                Street = street,
                                HouseNumber = houseNumber,
                                City = city,
                                PostalCode = postalCode,
                                Country = country
                            };
                        }

                        // Validate required fields
                        if (member.MemberNumber == 0 || 
                            string.IsNullOrWhiteSpace(member.FirstName) || 
                            string.IsNullOrWhiteSpace(member.LastName))
                        {
                            errors.Add($"Row {i + 1}: Missing required fields (Member Number, First Name, or Last Name)");
                            continue;
                        }

                        // Check if member number already exists
                        if (await _context.Members.AnyAsync(m => m.MemberNumber == member.MemberNumber))
                        {
                            skippedCount++;
                            continue;
                        }

                        _context.Members.Add(member);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {i}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    importedCount,
                    skippedCount,
                    errorCount = errors.Count,
                    errors = errors.Take(10).ToList() // Return first 10 errors only
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CSV");
                return StatusCode(500, new { error = "Failed to import CSV" });
            }
        }

        private string GetMappedValue(Dictionary<string, string> row, Dictionary<string, string> mapping, string propertyName)
        {
            if (mapping.TryGetValue(propertyName, out var csvColumn) && 
                !string.IsNullOrWhiteSpace(csvColumn) &&
                row.TryGetValue(csvColumn, out var value))
            {
                var trimmed = value?.Trim().Trim('"');
                return string.IsNullOrWhiteSpace(trimmed) ? string.Empty : trimmed;
            }
            return string.Empty;
        }

        private string? GetMappedValueOrNull(Dictionary<string, string> row, Dictionary<string, string> mapping, string propertyName)
        {
            var value = GetMappedValue(row, mapping, propertyName);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private bool ParseBoolValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            
            var lower = value.ToLower().Trim();
            return lower == "true" || lower == "1" || lower == "yes" || lower == "alive";
        }

        private char DetectSeparator(string line)
        {
            int commaCount = 0;
            int semicolonCount = 0;
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes)
                {
                    if (c == ',') commaCount++;
                    if (c == ';') semicolonCount++;
                }
            }

            return semicolonCount > commaCount ? ';' : ',';
        }

        private string[] ParseCsvLine(string line, char separator = ',')
        {
            var values = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == separator && !inQuotes)
                {
                    values.Add(current.ToString().Trim().Trim('"'));
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(current.ToString().Trim().Trim('"'));
            return values.ToArray();
        }

        private bool MemberExists(int id)
        {
            return _context.Members.Any(e => e.Id == id);
        }
    }
}
