using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminMembers.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Members_Email",
                table: "Members",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Members_FirstName",
                table: "Members",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_Members_IsAlive",
                table: "Members",
                column: "IsAlive");

            migrationBuilder.CreateIndex(
                name: "IX_Members_LastName",
                table: "Members",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFields_DisplayOrder",
                table: "CustomFields",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFields_IsActive",
                table: "CustomFields",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Members_Email",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_FirstName",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_IsAlive",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_LastName",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_CustomFields_DisplayOrder",
                table: "CustomFields");

            migrationBuilder.DropIndex(
                name: "IX_CustomFields_IsActive",
                table: "CustomFields");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs");
        }
    }
}
