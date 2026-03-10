using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class OrgLeaveConfigAndLeaveTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdvanceFilingDays",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AllowLeaveOnHoliday",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowLeaveOnRestDay",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowRetroactiveFiling",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ApproverRole",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CanOrgOverride",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "LeaveTypes",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompensationSource",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeductBalanceOn",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FilingUnit",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemProtected",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LeaveCategory",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LeaveExpiryCustomMonths",
                table: "LeaveTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeaveExpiryRule",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxConsecutiveDays",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinServiceMonths",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaidStatus",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresHrValidation",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresLegalQualification",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StatutoryCode",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TemplateKey",
                table: "LeaveTypes",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrgLeaveConfigs",
                columns: table => new
                {
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    FiscalYearStart = table.Column<int>(type: "int", nullable: false),
                    BalanceResetRule = table.Column<int>(type: "int", nullable: false),
                    ProratedForNewHires = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgLeaveConfigs", x => x.OrgID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgLeaveConfigs_OrgID",
                table: "OrgLeaveConfigs",
                column: "OrgID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgLeaveConfigs");

            migrationBuilder.DropColumn(
                name: "AdvanceFilingDays",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AllowLeaveOnHoliday",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AllowLeaveOnRestDay",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AllowRetroactiveFiling",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "ApproverRole",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CanOrgOverride",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CompensationSource",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "DeductBalanceOn",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "FilingUnit",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "IsSystemProtected",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "LeaveCategory",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "LeaveExpiryCustomMonths",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "LeaveExpiryRule",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "MaxConsecutiveDays",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "MinServiceMonths",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "PaidStatus",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "RequiresHrValidation",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "RequiresLegalQualification",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "StatutoryCode",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "TemplateKey",
                table: "LeaveTypes");
        }
    }
}
