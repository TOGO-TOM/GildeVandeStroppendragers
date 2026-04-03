using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Attributes;
using AdminMembers.Services;

namespace AdminMembers.Controllers
{
    [ApiController]
    [Route("api/stock")]
    public class StockController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly StockExportService _exportService;
        private readonly ILogger<StockController> _logger;

        public StockController(ApplicationDbContext context, AuditLogService auditLogService,
            StockExportService exportService, ILogger<StockController> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _exportService = exportService;
            _logger = logger;
        }

        // GET /api/stock
        [HttpGet]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.StockItems
                .AsNoTracking()
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Name)
                .ToListAsync();
            return Ok(items);
        }

        // GET /api/stock/{id}
        [HttpGet("{id}")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _context.StockItems.FindAsync(id);
            if (item == null) return NotFound(new { error = "Item not found" });
            return Ok(item);
        }

        // GET /api/stock/{id}/movements
        [HttpGet("{id}/movements")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> GetMovements(int id)
        {
            var movements = await _context.StockMovements
                .Where(m => m.StockItemId == id)
                .AsNoTracking()
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();
            return Ok(movements);
        }

        // POST /api/stock
        [HttpPost]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> Create([FromBody] StockItemRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { error = "Name is required" });

            var item = new StockItem
            {
                Name         = req.Name.Trim(),
                Description  = req.Description?.Trim(),
                Category     = req.Category?.Trim(),
                Unit         = string.IsNullOrWhiteSpace(req.Unit) ? "stuks" : req.Unit.Trim(),
                CurrentStock = req.CurrentStock,
                MinimumStock = req.MinimumStock,
                CreatedAt    = DateTime.UtcNow
            };

            _context.StockItems.Add(item);
            await _context.SaveChangesAsync();

            var userId   = int.TryParse(Request.Headers["X-User-Id"], out var uid) ? uid : 0;
            var username = Request.Headers["X-Username"].ToString();
            var ip       = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogActionAsync(userId, username, "StockItem Created", "StockItem", item.Id, $"Created {item.Name}", ip);

            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }

        // PUT /api/stock/{id}
        [HttpPut("{id}")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> Update(int id, [FromBody] StockItemRequest req)
        {
            var item = await _context.StockItems.FindAsync(id);
            if (item == null) return NotFound(new { error = "Item not found" });
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { error = "Name is required" });

            item.Name         = req.Name.Trim();
            item.Description  = req.Description?.Trim();
            item.Category     = req.Category?.Trim();
            item.Unit         = string.IsNullOrWhiteSpace(req.Unit) ? "stuks" : req.Unit.Trim();
            item.MinimumStock = req.MinimumStock;
            item.UpdatedAt    = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var userId   = int.TryParse(Request.Headers["X-User-Id"], out var uid) ? uid : 0;
            var username = Request.Headers["X-Username"].ToString();
            var ip       = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogActionAsync(userId, username, "StockItem Updated", "StockItem", item.Id, $"Updated {item.Name}", ip);

            return Ok(item);
        }

        // DELETE /api/stock/{id}
        [HttpDelete("{id}")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.StockItems.FindAsync(id);
            if (item == null) return NotFound(new { error = "Item not found" });

            _context.StockItems.Remove(item);
            await _context.SaveChangesAsync();

            var userId   = int.TryParse(Request.Headers["X-User-Id"], out var uid) ? uid : 0;
            var username = Request.Headers["X-Username"].ToString();
            var ip       = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogActionAsync(userId, username, "StockItem Deleted", "StockItem", id, $"Deleted {item.Name}", ip);

            return Ok(new { success = true });
        }

        // POST /api/stock/{id}/movement
        [HttpPost("{id}/movement")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> AddMovement(int id, [FromBody] StockMovementRequest req)
        {
            var item = await _context.StockItems.FindAsync(id);
            if (item == null) return NotFound(new { error = "Item not found" });

            if (req.Quantity <= 0)
                return BadRequest(new { error = "Quantity must be greater than 0" });

            var username = Request.Headers["X-Username"].ToString();
            var userId   = int.TryParse(Request.Headers["X-User-Id"], out var uid) ? uid : 0;

            // Apply movement to stock
            item.CurrentStock = req.Type switch
            {
                StockMovementType.In         => item.CurrentStock + req.Quantity,
                StockMovementType.Out        => item.CurrentStock - req.Quantity,
                StockMovementType.Correction => req.Quantity,
                _                            => item.CurrentStock
            };
            item.UpdatedAt = DateTime.UtcNow;

            var movement = new StockMovement
            {
                StockItemId        = id,
                Type               = req.Type,
                Quantity           = req.Quantity,
                Note               = req.Note?.Trim(),
                CreatedByUserId    = userId > 0 ? userId : null,
                CreatedByUsername  = username,
                CreatedAt          = DateTime.UtcNow
            };
            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await _auditLogService.LogActionAsync(userId, username, $"Stock {req.Type}", "StockItem", id,
                $"{req.Type} {req.Quantity} {item.Unit} for {item.Name}", ip);

            return Ok(new { item, movement });
        }

        // ?? Export endpoints ?????????????????????????????????????????????

        [HttpGet("export/excel")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> ExportExcel()
        {
            var items    = await GetAllItemsInternal();
            var settings = await _context.AppSettings.FirstOrDefaultAsync();
            var bytes    = _exportService.ExportToExcel(items, settings?.CompanyName);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"stock_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet("export/csv")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> ExportCsv()
        {
            var items = await GetAllItemsInternal();
            var bytes = _exportService.ExportToCsv(items);
            return File(bytes, "text/csv", $"stock_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet("export/pdf")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> ExportPdf()
        {
            var items    = await GetAllItemsInternal();
            var settings = await _context.AppSettings.FirstOrDefaultAsync();
            var bytes    = _exportService.ExportToPdf(items, settings?.LogoData, settings?.CompanyName);
            return File(bytes, "application/pdf", $"stock_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        [HttpGet("chart-data")]
        [RequirePermission(Permission.ReadWrite)]
        public async Task<IActionResult> GetChartData()
        {
            var items = await GetAllItemsInternal();

            var barData = items
                .OrderByDescending(i => i.CurrentStock)
                .Take(15)
                .Select(i => new { label = i.Name, value = i.CurrentStock, unit = i.Unit })
                .ToList();

            var statusData = new[]
            {
                new { label = "OK",          value = items.Count(i => i.Status == StockStatus.Ok) },
                new { label = "Laag",        value = items.Count(i => i.Status == StockStatus.Low) },
                new { label = "Uitverkocht", value = items.Count(i => i.Status == StockStatus.Out) }
            };

            var categoryData = items
                .GroupBy(i => i.Category ?? "Overig")
                .OrderBy(g => g.Key)
                .Select(g => new { label = g.Key, value = (double)g.Sum(i => i.CurrentStock), count = g.Count() })
                .ToList();

            return Ok(new { barData, statusData, categoryData });
        }

        private async Task<List<StockItem>> GetAllItemsInternal() =>
            await _context.StockItems
                .AsNoTracking()
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Name)
                .ToListAsync();
    }

    public class StockItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal? MinimumStock { get; set; }
    }

    public class StockMovementRequest
    {
        public StockMovementType Type { get; set; }
        public decimal Quantity { get; set; }
        public string? Note { get; set; }
    }
}
