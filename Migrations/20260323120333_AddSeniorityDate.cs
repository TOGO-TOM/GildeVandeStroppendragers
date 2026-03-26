using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminMembers.Migrations
{
    /// <inheritdoc />
    public partial class AddSeniorityDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SeniorityDate",
                table: "Members",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeniorityDate",
                table: "Members");
        }
    }
}
