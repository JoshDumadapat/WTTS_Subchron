using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceCaptureSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoClockOutEnabled",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AutoClockOutMaxHours",
                table: "OrganizationSettings",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultShiftTemplateCode",
                table: "OrganizationSettings",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreventDoubleClockIn",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictByIp",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoClockOutEnabled",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "AutoClockOutMaxHours",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "DefaultShiftTemplateCode",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "PreventDoubleClockIn",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "RestrictByIp",
                table: "OrganizationSettings");
        }
    }
}
