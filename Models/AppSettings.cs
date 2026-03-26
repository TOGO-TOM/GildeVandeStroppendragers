namespace AdminMembers.Models
{
    public class AppSettings
    {
        public int Id { get; set; }
        public string? CompanyName { get; set; }
        public string? LogoFileName { get; set; }
        public byte[]? LogoData { get; set; }
        public string? LogoContentType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
