using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Services;

namespace AdminMembers.Pages.Members
{
    public class IndexModel : AuthenticatedPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, AuditLogService auditLogService, ILogger<IndexModel> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // ?? Page data ??????????????????????????????????????????
        public List<Member> Members { get; set; } = new();
        public List<CustomField> CustomFields { get; set; } = new();
        public List<CustomField> FilterableCustomFields { get; set; } = new();
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int DeceasedMembers { get; set; }
        public Dictionary<string, int> RoleCounts { get; set; } = new();
        public int AllMembersCount { get; set; }
        public bool CanWrite { get; set; }
        public string? AuthToken { get; set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        // ?? Bound form ?????????????????????????????????????????
        [BindProperty] public MemberForm Form { get; set; } = new();

        public class MemberForm
        {
            public int Id { get; set; }
            public int? MemberNumber { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Gender { get; set; } = "Man";
            public string Role { get; set; } = "Stappend Lid";
            public bool IsAlive { get; set; } = true;
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public string? BirthDate { get; set; }
            public string? SeniorityDate { get; set; }

            // Address
            public int AddressId { get; set; }
            public string? Street { get; set; }
            public string? HouseNumber { get; set; }
            public string? City { get; set; }
            public string? PostalCode { get; set; }
            public string? Country { get; set; }
        }

        // ?? GET ????????????????????????????????????????????????
        public async Task<IActionResult> OnGetAsync(string? search, string? sortBy, string? filterRole, string? filterAlive)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            CanWrite = HasPermission("Write");
            AuthToken = HttpContext.Session.GetString("AuthToken");
            await LoadPageDataAsync(search, sortBy, filterRole, filterAlive);
            return Page();
        }

        // ?? POST: Save (create or update) ?????????????????????
        public async Task<IActionResult> OnPostSaveAsync(string? search, string? sortBy, string? filterRole, string? filterAlive)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            CanWrite = HasPermission("Write");

            if (!CanWrite)
            {
                ErrorMessage = "You do not have permission to modify members.";
                await LoadPageDataAsync(search, sortBy, filterRole, filterAlive);
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Form.FirstName) || string.IsNullOrWhiteSpace(Form.LastName))
            {
                ErrorMessage = "First name and last name are required.";
                await LoadPageDataAsync(search, sortBy, filterRole, filterAlive);
                return Page();
            }

            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userId = CurrentUser!.Id;
                var username = CurrentUser.Username;

                if (Form.Id == 0)
                {
                    // ?? CREATE ??????????????????????????????????
                    if (Form.MemberNumber.HasValue && Form.MemberNumber > 0 &&
                        await _context.Members.AnyAsync(m => m.MemberNumber == Form.MemberNumber))
                    {
                        ErrorMessage = $"Member number {Form.MemberNumber} is already in use.";
                        await LoadPageDataAsync(search, sortBy, filterRole, filterAlive);
                        return Page();
                    }

                    var member = MapFormToMember(new Member());
                    _context.Members.Add(member);
                    await _context.SaveChangesAsync();
                    await SaveCustomFieldValuesAsync(member.Id);
                    await _auditLogService.LogActionAsync(userId, username, "Member Created", "Member",
                        member.Id, $"Created member {member.FirstName} {member.LastName}", ip);
                    SuccessMessage = $"{member.FirstName} {member.LastName} was added successfully.";
                }
                else
                {
                    // ?? UPDATE ??????????????????????????????????
                    var existing = await _context.Members
                        .Include(m => m.Address)
                        .FirstOrDefaultAsync(m => m.Id == Form.Id);

                    if (existing == null)
                    {
                        ErrorMessage = "Member not found.";
                        await LoadPageDataAsync(search, sortBy, filterRole, filterAlive);
                        return Page();
                    }

                    if (Form.MemberNumber.HasValue && Form.MemberNumber > 0 &&
                        await _context.Members.AnyAsync(m => m.MemberNumber == Form.MemberNumber && m.Id != Form.Id))
                    {
                        ErrorMessage = $"Member number {Form.MemberNumber} is already in use.";
                        await LoadPageDataAsync(search, sortBy, filterRole, filterAlive);
                        return Page();
                    }

                    MapFormToMember(existing);
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await SaveCustomFieldValuesAsync(existing.Id);
                    await _auditLogService.LogActionAsync(userId, username, "Member Updated", "Member",
                        existing.Id, $"Updated member {existing.FirstName} {existing.LastName}", ip);
                    SuccessMessage = $"{existing.FirstName} {existing.LastName} was updated successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving member");
                ErrorMessage = "An error occurred while saving. Please try again.";
            }

            await LoadPageDataAsync(search, sortBy, filterRole, filterAlive);
            return Page();
        }

        // ?? POST: Delete ???????????????????????????????????????
        public async Task<IActionResult> OnPostDeleteAsync(int id, string? search, string? sortBy, string? filterRole, string? filterAlive)
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            CanWrite = HasPermission("Write");

            if (!CanWrite)
            {
                ErrorMessage = "You do not have permission to delete members.";
                await LoadPageDataAsync(search, sortBy, filterRole, filterAlive);
                return Page();
            }

            try
            {
                var member = await _context.Members.FindAsync(id);
                if (member != null)
                {
                    _context.Members.Remove(member);
                    await _context.SaveChangesAsync();
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    await _auditLogService.LogActionAsync(CurrentUser!.Id, CurrentUser.Username,
                        "Member Deleted", "Member", id,
                        $"Deleted member {member.FirstName} {member.LastName}", ip);
                    SuccessMessage = $"{member.FirstName} {member.LastName} was deleted.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting member {Id}", id);
                ErrorMessage = "An error occurred while deleting. Please try again.";
            }

            await LoadPageDataAsync(search, sortBy, filterRole, filterAlive);
            return Page();
        }

        // ?? Helpers ????????????????????????????????????????????
        private async Task LoadPageDataAsync(string? search, string? sortBy, string? filterRole, string? filterAlive)
        {
            // Single query for role counts and total � no need to load full entities
            var roleCounts = await _context.Members
                .AsNoTracking()
                .GroupBy(m => m.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            AllMembersCount = roleCounts.Sum(r => r.Count);
            RoleCounts = roleCounts.ToDictionary(g => g.Role, g => g.Count);

            var query = _context.Members
                .Include(m => m.Address)
                .Include(m => m.CustomFieldValues)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m =>
                    m.FirstName.Contains(search) ||
                    m.LastName.Contains(search) ||
                    (m.Email != null && m.Email.Contains(search)) ||
                    (m.MemberNumber != null && m.MemberNumber.ToString()!.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filterRole) && filterRole != "all")
                query = query.Where(m => m.Role == filterRole);

            // Custom field filters: ?filterCf_<id>=<value>
            foreach (var key in Request.Query.Keys.Where(k => k.StartsWith("filterCf_")))
            {
                if (!int.TryParse(key[9..], out var cfId)) continue;
                var cfVal = Request.Query[key].ToString();
                if (!string.IsNullOrEmpty(cfVal))
                    query = query.Where(m => m.CustomFieldValues.Any(cv => cv.CustomFieldId == cfId && cv.Value == cfVal));
            }

            // Compute alive/deceased counts BEFORE applying the alive filter so stat tiles always show accurate totals
            var aliveFlags = await query.Select(m => m.IsAlive).ToListAsync();
            TotalMembers    = aliveFlags.Count;
            ActiveMembers   = aliveFlags.Count(a => a);
            DeceasedMembers = aliveFlags.Count(a => !a);

            if (filterAlive == "alive")
                query = query.Where(m => m.IsAlive);
            else if (filterAlive == "deceased")
                query = query.Where(m => !m.IsAlive);

            query = sortBy switch
            {
                "lastName-asc"      => query.OrderBy(m => m.LastName).ThenBy(m => m.FirstName),
                "lastName-desc"     => query.OrderByDescending(m => m.LastName),
                "memberNumber-asc"  => query.OrderBy(m => m.MemberNumber),
                "memberNumber-desc" => query.OrderByDescending(m => m.MemberNumber),
                _                   => query.OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            };

            Members = await query.ToListAsync();

            CustomFields = await _context.CustomFields
                .Where(cf => cf.IsActive)
                .OrderBy(cf => cf.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            FilterableCustomFields = CustomFields.Where(cf => cf.IsFilterable).ToList();
        }

        private Member MapFormToMember(Member m)
        {
            m.MemberNumber  = Form.MemberNumber > 0 ? Form.MemberNumber : null;
            m.FirstName     = Form.FirstName.Trim();
            m.LastName      = Form.LastName.Trim();
            m.Gender        = Form.Gender;
            m.Role          = Form.Role;
            m.IsAlive       = Form.IsAlive;
            m.Email         = string.IsNullOrWhiteSpace(Form.Email) ? null : Form.Email.Trim();
            m.PhoneNumber   = string.IsNullOrWhiteSpace(Form.PhoneNumber) ? null : Form.PhoneNumber.Trim();
            m.BirthDate     = DateTime.TryParse(Form.BirthDate, out var bd) ? bd : null;
            m.SeniorityDate = DateTime.TryParse(Form.SeniorityDate, out var sd) ? sd : null;

            if (m.Address == null) m.Address = new Address();
            m.Address.MemberId    = m.Id;
            m.Address.Street      = Form.Street ?? string.Empty;
            m.Address.HouseNumber = Form.HouseNumber ?? string.Empty;
            m.Address.City        = Form.City ?? string.Empty;
            m.Address.PostalCode  = Form.PostalCode ?? string.Empty;
            m.Address.Country     = Form.Country ?? string.Empty;

            return m;
        }

        private async Task SaveCustomFieldValuesAsync(int memberId)
        {
            var existing = await _context.MemberCustomFields
                .Where(v => v.MemberId == memberId)
                .ToListAsync();
            _context.MemberCustomFields.RemoveRange(existing);

            foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("cf_")))
            {
                if (!int.TryParse(key[3..], out var cfId)) continue;
                var value = Request.Form[key].ToString().Trim();
                if (string.IsNullOrEmpty(value)) continue;

                _context.MemberCustomFields.Add(new MemberCustomField
                {
                    MemberId      = memberId,
                    CustomFieldId = cfId,
                    Value         = value,
                    CreatedAt     = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
