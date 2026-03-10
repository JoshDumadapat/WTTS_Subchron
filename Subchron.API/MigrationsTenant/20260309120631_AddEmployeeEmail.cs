using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class AddEmployeeEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Employees",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OrgID_Email",
                table: "Employees",
                columns: new[] { "OrgID", "Email" },
                unique: true,
                filter: "[Email] IS NOT NULL AND [Email] != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_OrgID_Email",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Employees");
        }
    }
}
