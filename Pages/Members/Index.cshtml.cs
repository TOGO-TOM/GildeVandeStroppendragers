using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;

namespace AdminMembers.Pages.Members
{
    public class IndexModel : AuthenticatedPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Member> Members { get; set; } = new();
        public List<CustomField> CustomFields { get; set; } = new();
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int DeceasedMembers { get; set; }

        public async Task<IActionResult> OnGetAsync(string? search, string? sortBy)
        {
            if (!CheckAuthentication())
            {
                return RedirectToLoginWithReturnUrl();
            }

            try
            {
                // Load members with addresses and custom fields
                var query = _context.Members
                    .Include(m => m.Address)
                    .Include(m => m.CustomFieldValues)
                    .ThenInclude(cfv => cfv.CustomField)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(m => 
                        m.FirstName.ToLower().Contains(search) ||
                        m.LastName.ToLower().Contains(search) ||
                        (m.Email != null && m.Email.ToLower().Contains(search)) ||
                        (m.MemberNumber != null && m.MemberNumber.ToString().Contains(search))
                    );
                }

                // Apply sorting
                query = sortBy switch
                {
                    "lastName-asc" => query.OrderBy(m => m.LastName).ThenBy(m => m.FirstName),
                    "lastName-desc" => query.OrderByDescending(m => m.LastName).ThenByDescending(m => m.FirstName),
                    "memberNumber-asc" => query.OrderBy(m => m.MemberNumber),
                    "memberNumber-desc" => query.OrderByDescending(m => m.MemberNumber),
                    _ => query.OrderBy(m => m.Id)
                };

                Members = await query.ToListAsync();

                // Calculate statistics
                TotalMembers = Members.Count;
                ActiveMembers = Members.Count(m => m.IsAlive);
                DeceasedMembers = Members.Count(m => !m.IsAlive);

                // Load custom fields for form
                CustomFields = await _context.CustomFields
                    .Where(cf => cf.IsActive)
                    .OrderBy(cf => cf.DisplayOrder)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading members");
                return Page();
            }
        }
    }
}
