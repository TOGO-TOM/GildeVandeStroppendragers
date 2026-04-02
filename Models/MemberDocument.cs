namespace AdminMembers.Models
{
    public class MemberDocument
    {
        public int Id { get; set; }
        public int MemberId { get; set; }

        public string FileName { get; set; } = string.Empty;       // Original file name shown to users
        public string BlobName { get; set; } = string.Empty;       // Key inside the blob container
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        public int UploadedByUserId { get; set; }
        public string UploadedByUsername { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Member? Member { get; set; }
    }
}
