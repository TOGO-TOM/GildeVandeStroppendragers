using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;

namespace AdminMembers.Pages.Stock
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

        public List<StockItem> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public string? AuthToken { get; set; }
        public bool CanWrite { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckAuthentication()) return RedirectToLoginWithReturnUrl();

            // Stock module: Super Admin, Admin, Stock Editor, Stock Viewer
            if (!IsAdmin() && !CanViewStock())
                return RedirectToPage("/Home");

            AuthToken = HttpContext.Session.GetString("AuthToken");
            CanWrite = CanManageStock();

            Items = await _context.StockItems
                .AsNoTracking()
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Name)
                .ToListAsync();

            TotalItems     = Items.Count;
            OutOfStockItems = Items.Count(i => i.CurrentStock <= 0);
            LowStockItems  = Items.Count(i => i.CurrentStock > 0 && i.MinimumStock.HasValue && i.CurrentStock <= i.MinimumStock.Value);

            return Page();
        }
    }
}
