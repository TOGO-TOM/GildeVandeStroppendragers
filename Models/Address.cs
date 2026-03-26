namespace AdminMembers.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string Street { get; set; } = string.Empty;
        public string? HouseNumber { get; set; }
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string? Country { get; set; }

        public int MemberId { get; set; }
        public Member? Member { get; set; }
    }
}
