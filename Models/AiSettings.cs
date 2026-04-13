namespace AdminMembers.Models
{
    public class AiSettings
    {
        public int Id { get; set; }

        /// <summary>GitHub Personal Access Token with workflow dispatch permission.</summary>
        public string? GitHubToken { get; set; }

        /// <summary>GitHub repository owner (e.g. "TOGO-TOM").</summary>
        public string? GitHubOwner { get; set; }

        /// <summary>GitHub repository name (e.g. "GildeVandeStroppendragers").</summary>
        public string? GitHubRepo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
