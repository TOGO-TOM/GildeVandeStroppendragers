using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminMembers.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureRequestsAndAiSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GitHubToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GitHubOwner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GitHubRepo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnalysisSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FeasibilityAssessment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchitecturalPlan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequests_CreatedAt",
                table: "FeatureRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequests_Status",
                table: "FeatureRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRequests_SubmittedByUserId",
                table: "FeatureRequests",
                column: "SubmittedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiSettings");

            migrationBuilder.DropTable(
                name: "FeatureRequests");
        }
    }
}
