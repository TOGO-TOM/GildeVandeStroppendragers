using Microsoft.AspNetCore.Mvc;
using AdminMembers.Models;
using AdminMembers.Services;

namespace AdminMembers.Pages.Admin
{
    public class AuditLogsModel : AuthenticatedPageModel
    {
        private readonly AuditLogService _auditLogService;

        public AuditLogsModel(AuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public List<AuditLog> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)] public string? FilterUsername { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterAction { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterEntity { get; set; }
        [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

        private const int PageSize = 50;

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();
            if (!HasPermission("AuditLogs")) return Forbid();

            var (logs, total) = await _auditLogService.GetFilteredLogsAsync(
                FilterUsername, FilterAction, FilterEntity, CurrentPage, PageSize);

            Logs       = logs;
            TotalCount = total;
            TotalPages = (int)Math.Ceiling(total / (double)PageSize);
            return Page();
        }
    }
}
