namespace AdminMembers.Models
{
    public class StockItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string Unit { get; set; } = "stuks";
        public decimal CurrentStock { get; set; } = 0;
        public decimal? MinimumStock { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();

        public StockStatus Status => CurrentStock <= 0
            ? StockStatus.Out
            : MinimumStock.HasValue && CurrentStock <= MinimumStock.Value
                ? StockStatus.Low
                : StockStatus.Ok;
    }

    public enum StockStatus { Ok, Low, Out }
}
