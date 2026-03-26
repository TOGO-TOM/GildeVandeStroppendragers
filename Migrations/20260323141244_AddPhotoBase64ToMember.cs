using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminMembers.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoBase64ToMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoBase64",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoBase64",
                table: "Members");
        }
    }
}
