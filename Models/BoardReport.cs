namespace AdminMembers.Models
{
    public class BoardReport
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime MeetingDate { get; set; }
        public string? MeetingTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? AgendaItems { get; set; }
        public string? Content { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Draft"; // Draft, Final
        public int CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<BoardReportAttendee> Attendees { get; set; } = new List<BoardReportAttendee>();
    }

    public class BoardReportAttendee
    {
        public int Id { get; set; }
        public int BoardReportId { get; set; }
        public int MemberId { get; set; }
        public bool IsPresent { get; set; } = true;

        public BoardReport? BoardReport { get; set; }
        public Member? Member { get; set; }
    }
}
