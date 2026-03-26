namespace AdminMembers.Models
{
    public class MemberCustomField
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public Member? Member { get; set; }
        public int CustomFieldId { get; set; }
        public CustomField? CustomField { get; set; }
        public string? Value { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
