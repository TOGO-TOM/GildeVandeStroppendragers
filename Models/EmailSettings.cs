namespace AdminMembers.Models
{
    public class EmailSettings
    {
        public int Id { get; set; }

        /// <summary>Smtp, ApiKey, or MailerSend</summary>
        public string Provider { get; set; } = "Smtp";

        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }

        /// <summary>API key used for providers like SendGrid, Mailgun, etc.</summary>
        public string? ApiKey { get; set; }

        public string? FromAddress { get; set; }
        public string? FromName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
