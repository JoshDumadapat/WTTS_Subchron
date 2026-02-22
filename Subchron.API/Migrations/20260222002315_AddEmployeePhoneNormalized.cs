using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeePhoneNormalized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNormalized",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OrgID_PhoneNormalized",
                table: "Employees",
                columns: new[] { "OrgID", "PhoneNormalized" },
                unique: true,
                filter: "[PhoneNormalized] IS NOT NULL AND [PhoneNormalized] != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_OrgID_PhoneNormalized",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PhoneNormalized",
                table: "Employees");
        }
    }
}
