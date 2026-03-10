using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class AddAttendancePolicyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowIncompleteLogs",
                table: "OrgAttendanceConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoAbsentWithoutLog",
                table: "OrgAttendanceConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoFlagMissingPunch",
                table: "OrgAttendanceConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DefaultMissingPunchAction",
                table: "OrgAttendanceConfigs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "IGNORE");

            migrationBuilder.AddColumn<int>(
                name: "EarliestClockInMinutes",
                table: "OrgAttendanceConfigs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LatestClockInMinutes",
                table: "OrgAttendanceConfigs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualEntryAccessMode",
                table: "OrgAttendanceConfigs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "SUPERVISOR");

            migrationBuilder.AddColumn<bool>(
                name: "MarkUndertimeBasedOnSchedule",
                table: "OrgAttendanceConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseGracePeriodForLate",
                table: "OrgAttendanceConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowIncompleteLogs",
                table: "OrgAttendanceConfigs");

            migrationBuilder.DropColumn(
                name: "AutoAbsentWithoutLog",
                table: "OrgAttendanceConfigs");

            migrationBuilder.DropColumn(
                name: "AutoFlagMissingPunch",
                table: "OrgAttendanceConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultMissingPunchAction",
                table: "OrgAttendanceConfigs");

            migrationBuilder.DropColumn(
                name: "EarliestClockInMinutes",
                table: "OrgAttendanceConfigs");

            migrationBuilder.DropColumn(
                name: "LatestClockInMinutes",
                table: "OrgAttendanceConfigs");

            migrationBuilder.DropColumn(
                name: "ManualEntryAccessMode",
                table: "OrgAttendanceConfigs");

            migrationBuilder.DropColumn(
                name: "MarkUndertimeBasedOnSchedule",
                table: "OrgAttendanceConfigs");

            migrationBuilder.DropColumn(
                name: "UseGracePeriodForLate",
                table: "OrgAttendanceConfigs");
        }
    }
}
