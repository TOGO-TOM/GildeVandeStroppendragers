namespace AdminMembers.Models
{
    public class CustomField
    {
        public int Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string FieldType { get; set; } = "Text";
        public bool IsRequired { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<MemberCustomField> MemberValues { get; set; } = new List<MemberCustomField>();
    }
}
