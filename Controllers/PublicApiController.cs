using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Attributes;
using AdminMembers.Services;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/public/members")]
    [FormatFilter]
    [Produces("application/json", "application/xml")]
    public class PublicApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public PublicApiController(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [HttpGet(".{format}")]
        [RequirePermission(Permission.Read)]
        public async Task<ActionResult> GetMembers([FromQuery] string? role = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 200) pageSize = 200;

            var query = _context.Members
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(m => m.Role == role);

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new PublicMemberResponse
                {
                    Id = m.Id,
                    MemberNumber = m.MemberNumber,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Gender = m.Gender,
                    Role = m.Role,
                    BirthDate = m.BirthDate,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    IsAlive = m.IsAlive,
                    SeniorityDate = m.SeniorityDate,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    Address = m.Address != null ? new PublicAddressResponse
                    {
                        Street = m.Address.Street,
                        HouseNumber = m.Address.HouseNumber,
                        City = m.Address.City,
                        PostalCode = m.Address.PostalCode,
                        Country = m.Address.Country
                    } : null,
                    CustomFields = m.CustomFieldValues
                        .Where(cf => cf.CustomField != null && cf.CustomField.IsActive)
                        .Select(cf => new PublicCustomFieldResponse
                        {
                            FieldName = cf.CustomField!.FieldName,
                            FieldLabel = cf.CustomField.FieldLabel,
                            Value = cf.Value
                        }).ToList()
                })
                .ToListAsync();

            var username = Request.Headers["X-Username"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogActionAsync(0, username, "API Read", "Member", null, $"Listed members (page {page}, pageSize {pageSize}, role: {role ?? "all"}, returned {data.Count}/{total})", ip);

            return Ok(new PublicMembersListResponse { Data = data, Total = total, Page = page, PageSize = pageSize });
        }

        [HttpGet("{id}")]
        [HttpGet("{id}.{format}")]
        [RequirePermission(Permission.Read)]
        public async Task<ActionResult> GetMember(int id)
        {
            var result = await _context.Members
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => new PublicMemberResponse
                {
                    Id = m.Id,
                    MemberNumber = m.MemberNumber,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Gender = m.Gender,
                    Role = m.Role,
                    BirthDate = m.BirthDate,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    IsAlive = m.IsAlive,
                    SeniorityDate = m.SeniorityDate,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    Address = m.Address != null ? new PublicAddressResponse
                    {
                        Street = m.Address.Street,
                        HouseNumber = m.Address.HouseNumber,
                        City = m.Address.City,
                        PostalCode = m.Address.PostalCode,
                        Country = m.Address.Country
                    } : null,
                    CustomFields = m.CustomFieldValues
                        .Where(cf => cf.CustomField != null && cf.CustomField.IsActive)
                        .Select(cf => new PublicCustomFieldResponse
                        {
                            FieldName = cf.CustomField!.FieldName,
                            FieldLabel = cf.CustomField.FieldLabel,
                            Value = cf.Value
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound(new { error = "Member not found" });

            var username = Request.Headers["X-Username"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogActionAsync(0, username, "API Read", "Member", id, $"Read member #{id} ({result.FirstName} {result.LastName})", ip);

            return Ok(result);
        }
    }

    // Response DTOs (exclude PhotoBase64 and Documents)
    [System.Xml.Serialization.XmlRoot("MembersResponse")]
    public class PublicMembersListResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        [System.Xml.Serialization.XmlArray("Data")]
        [System.Xml.Serialization.XmlArrayItem("Member")]
        public List<PublicMemberResponse> Data { get; set; } = new();
    }

    [System.Xml.Serialization.XmlRoot("Member")]
    public class PublicMemberResponse
    {
        public int Id { get; set; }
        public int? MemberNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsAlive { get; set; }
        public DateTime? SeniorityDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public PublicAddressResponse? Address { get; set; }
        [System.Xml.Serialization.XmlArray("CustomFields")]
        [System.Xml.Serialization.XmlArrayItem("CustomField")]
        public List<PublicCustomFieldResponse> CustomFields { get; set; } = new();
    }

    public class PublicAddressResponse
    {
        public string Street { get; set; } = string.Empty;
        public string? HouseNumber { get; set; }
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string? Country { get; set; }
    }

    public class PublicCustomFieldResponse
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
