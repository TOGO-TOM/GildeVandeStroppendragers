namespace AdminMembers.Models
{
    public class FeatureRequest
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>Pending, Analyzed, Planned, Rejected</summary>
        public string Status { get; set; } = "Pending";

        // Agent 1 output — Request Analyzer
        public string? AnalysisSummary { get; set; }
        public string? Category { get; set; }
        public string? Priority { get; set; }

        // Agent 2 output — Feasibility & Architecture
        public string? FeasibilityAssessment { get; set; }
        public string? ArchitecturalPlan { get; set; }

        public int SubmittedByUserId { get; set; }
        public string SubmittedByUsername { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AnalyzedAt { get; set; }
        public DateTime? PlannedAt { get; set; }
    }
}
