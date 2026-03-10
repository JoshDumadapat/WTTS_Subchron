using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgPayConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DayType",
                table: "EarningRules",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Any");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "EarningRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "EarningRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HolidayCombo",
                table: "EarningRules",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "EarningRules",
                type: "NVARCHAR(MAX)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RestDayHandling",
                table: "EarningRules",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "FollowAttendance");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "EarningRules",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "AllEmployees");

            migrationBuilder.AddColumn<string>(
                name: "ScopeTagsJson",
                table: "EarningRules",
                type: "NVARCHAR(MAX)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "DeductionRules",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Statutory");

            migrationBuilder.AddColumn<string>(
                name: "ComputeBasedOn",
                table: "DeductionRules",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "BasicPay");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "DeductionRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDeductionAmount",
                table: "DeductionRules",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "DeductionRules",
                type: "NVARCHAR(MAX)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScopeTagsJson",
                table: "DeductionRules",
                type: "NVARCHAR(MAX)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "OrgAllowanceRules",
                columns: table => new
                {
                    OrgAllowanceRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AllowanceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceDependent = table.Column<bool>(type: "bit", nullable: false),
                    ProrateIfPartialPeriod = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ScopeTagsJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    ComplianceNotes = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgAllowanceRules", x => x.OrgAllowanceRuleID);
                    table.ForeignKey(
                        name: "FK_OrgAllowanceRules_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgPayConfigs",
                columns: table => new
                {
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PayCycle = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HoursPerDay = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CutoffWindowsJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    LockAttendanceAfterCutoff = table.Column<bool>(type: "bit", nullable: false),
                    ThirteenthMonthBasis = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ThirteenthMonthNotes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EnableBIR = table.Column<bool>(type: "bit", nullable: false),
                    BIRPeriod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BIRTableVersion = table.Column<int>(type: "int", nullable: false),
                    EnableSSS = table.Column<bool>(type: "bit", nullable: false),
                    SSSEmployerPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EnablePhilHealth = table.Column<bool>(type: "bit", nullable: false),
                    PhilHealthRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EnablePagIbig = table.Column<bool>(type: "bit", nullable: false),
                    PagIbigRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EnableIncomeTax = table.Column<bool>(type: "bit", nullable: false),
                    ProrateNewHires = table.Column<bool>(type: "bit", nullable: false),
                    ApplyTaxThreshold = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgPayConfigs", x => x.OrgID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgAllowanceRules_OrgID",
                table: "OrgAllowanceRules",
                column: "OrgID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgAllowanceRules");

            migrationBuilder.DropTable(
                name: "OrgPayConfigs");

            migrationBuilder.DropColumn(
                name: "DayType",
                table: "EarningRules");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "EarningRules");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "EarningRules");

            migrationBuilder.DropColumn(
                name: "HolidayCombo",
                table: "EarningRules");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "EarningRules");

            migrationBuilder.DropColumn(
                name: "RestDayHandling",
                table: "EarningRules");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "EarningRules");

            migrationBuilder.DropColumn(
                name: "ScopeTagsJson",
                table: "EarningRules");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "DeductionRules");

            migrationBuilder.DropColumn(
                name: "ComputeBasedOn",
                table: "DeductionRules");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "DeductionRules");

            migrationBuilder.DropColumn(
                name: "MaxDeductionAmount",
                table: "DeductionRules");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DeductionRules");

            migrationBuilder.DropColumn(
                name: "ScopeTagsJson",
                table: "DeductionRules");
        }
    }
}
