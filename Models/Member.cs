namespace AdminMembers.Models
{
    public class Member
    {
        public int Id { get; set; }
        public int? MemberNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = "Man";
        public string Role { get; set; } = "Stappend Lid";
        public string? PhotoBase64 { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsAlive { get; set; } = true;
        public DateTime? SeniorityDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Address? Address { get; set; }
        public ICollection<MemberCustomField> CustomFieldValues { get; set; } = new List<MemberCustomField>();
        public ICollection<MemberDocument> Documents { get; set; } = new List<MemberDocument>();
    }
}
