using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class TrimOrganizationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowManualEntry",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "AttendanceOvertimeSettingsJson",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "AutoClockOutEnabled",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "AutoClockOutMaxHours",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "DefaultGraceMinutes",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "EnforceGeofence",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "LeaveBalanceResetRule",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "LeaveFiscalYearStart",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "LeaveProratedForNewHires",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "NightDifferentialSettingsJson",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "OTApprovalRequired",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "OTEnabled",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "OTMaxHoursPerDay",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "OTThresholdHours",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "OvertimeSettingsJson",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "PreventDoubleClockIn",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "RequireGeo",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "RestrictByIp",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "RoundRule",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "ShiftTemplatesJson",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "WeeklyOtThresholdHours",
                table: "OrganizationSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowManualEntry",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceOvertimeSettingsJson",
                table: "OrganizationSettings",
                type: "NVARCHAR(MAX)",
                nullable: true);

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

            migrationBuilder.AddColumn<int>(
                name: "DefaultGraceMinutes",
                table: "OrganizationSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EnforceGeofence",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LeaveBalanceResetRule",
                table: "OrganizationSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LeaveFiscalYearStart",
                table: "OrganizationSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LeaveProratedForNewHires",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NightDifferentialSettingsJson",
                table: "OrganizationSettings",
                type: "NVARCHAR(MAX)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OTApprovalRequired",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OTEnabled",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OTMaxHoursPerDay",
                table: "OrganizationSettings",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OTThresholdHours",
                table: "OrganizationSettings",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OvertimeSettingsJson",
                table: "OrganizationSettings",
                type: "NVARCHAR(MAX)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreventDoubleClockIn",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireGeo",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictByIp",
                table: "OrganizationSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoundRule",
                table: "OrganizationSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShiftTemplatesJson",
                table: "OrganizationSettings",
                type: "NVARCHAR(MAX)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyOtThresholdHours",
                table: "OrganizationSettings",
                type: "decimal(6,2)",
                nullable: true);
        }
    }
}
