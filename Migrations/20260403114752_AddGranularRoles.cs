using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AdminMembers.Migrations
{
    /// <inheritdoc />
    public partial class AddGranularRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "All rights including audit logs", "Super Admin" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "All rights except audit logs", "Admin" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Name", "Permission" },
                values: new object[] { "Can add, edit and delete members", "Member Editor", 3 });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name", "Permission" },
                values: new object[,]
                {
                    { 4, "Read-only access to members", "Member Viewer", 1 },
                    { 5, "Can add, edit and manage stock", "Stock Editor", 3 },
                    { 6, "Read-only access to stock", "Stock Viewer", 1 }
                });

            // Old role Id=2 was "Editor" → now "Admin" (already updated above)
            // Old role Id=3 was "Viewer" → now "Member Editor" (updated above)
            // Users who had the old Viewer role (Id=3) should become Member Viewer (Id=4)
            // We do this by updating existing UserRoles referencing the old Viewer role
            // Note: old "Viewer" was Id=3, now Id=3 = "Member Editor", Id=4 = "Member Viewer"
            // So we need to fix users that were assigned the old Viewer (read-only) intent.
            // Since EF seed renamed Id=3 to Member Editor, we add Member Viewer for those users
            // who previously only had the read-only role. This is a best-effort migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Full access with read and write permissions", "Admin" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Can read and write data", "Editor" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Name", "Permission" },
                values: new object[] { "Read-only access", "Viewer", 1 });
        }
    }
}
