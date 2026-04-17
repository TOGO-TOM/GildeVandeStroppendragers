namespace AdminMembers.Models
{
    public class ApiKey
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KeyHash { get; set; } = string.Empty;
        public string KeyPrefix { get; set; } = string.Empty;
        public ApiKeyPermission Permission { get; set; } = ApiKeyPermission.Read;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public int CreatedByUserId { get; set; }
    }

    public enum ApiKeyPermission
    {
        Read = 1,
        ReadWrite = 2
    }
}
