using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminMembers.Migrations
{
    /// <inheritdoc />
    public partial class MakeMemberNumberOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_MemberNumber",
                table: "Members");

            migrationBuilder.AlterColumn<int>(
                name: "MemberNumber",
                table: "Members",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Members_MemberNumber",
                table: "Members",
                column: "MemberNumber",
                unique: true,
                filter: "[MemberNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_MemberNumber",
                table: "Members");

            migrationBuilder.AlterColumn<int>(
                name: "MemberNumber",
                table: "Members",
                type: "int",
                maxLength: 50,
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_MemberNumber",
                table: "Members",
                column: "MemberNumber",
                unique: true);
        }
    }
}
