namespace AdminMembers.Models
{
    public class BulkUpdateRequest
    {
        public List<int> MemberIds { get; set; } = new();
        public string? Gender { get; set; }
        public string? Role { get; set; }
        public bool? IsAlive { get; set; }
    }
}
