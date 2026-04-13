using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminMembers.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AgendaItems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardReportAttendees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardReportId = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    IsPresent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardReportAttendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardReportAttendees_BoardReports_BoardReportId",
                        column: x => x.BoardReportId,
                        principalTable: "BoardReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardReportAttendees_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name", "Permission" },
                values: new object[] { 7, "Secretary - read/write all homepage features", "Secretaris", 3 });

            migrationBuilder.CreateIndex(
                name: "IX_BoardReportAttendees_BoardReportId_MemberId",
                table: "BoardReportAttendees",
                columns: new[] { "BoardReportId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardReportAttendees_MemberId",
                table: "BoardReportAttendees",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardReports_MeetingDate",
                table: "BoardReports",
                column: "MeetingDate");

            migrationBuilder.CreateIndex(
                name: "IX_BoardReports_Status",
                table: "BoardReports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardReportAttendees");

            migrationBuilder.DropTable(
                name: "BoardReports");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
