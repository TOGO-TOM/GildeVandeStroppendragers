namespace AdminMembers.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int StockItemId { get; set; }
        public StockMovementType Type { get; set; }
        public decimal Quantity { get; set; }
        public string? Note { get; set; }
        public int? CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public StockItem StockItem { get; set; } = null!;
    }

    public enum StockMovementType { In, Out, Correction }
}
