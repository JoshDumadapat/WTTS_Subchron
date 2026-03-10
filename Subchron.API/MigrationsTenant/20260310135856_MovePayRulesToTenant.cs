using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class MovePayRulesToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompensationBasis",
                table: "OrgPayConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomUnitLabel",
                table: "OrgPayConfigs",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CustomWorkHours",
                table: "OrgPayConfigs",
                type: "decimal(7,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeductionRules",
                columns: table => new
                {
                    DeductionRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Statutory"),
                    DeductionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    FormulaExpression = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasEmployerShare = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasEmployeeShare = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EmployerSharePercent = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    EmployeeSharePercent = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    AutoCompute = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ComputeBasedOn = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "BasicPay"),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxDeductionAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    ScopeTagsJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    Notes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: ""),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeductionRules", x => x.DeductionRuleID);
                });

            migrationBuilder.CreateTable(
                name: "EarningRules",
                columns: table => new
                {
                    EarningRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppliesTo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DayType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Any"),
                    HolidayCombo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Standard"),
                    RestDayHandling = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "FollowAttendance"),
                    Scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "AllEmployees"),
                    ScopeTagsJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RateValue = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IncludeInBenefitBase = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: ""),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarningRules", x => x.EarningRuleID);
                });

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
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeductionRules_OrgID",
                table: "DeductionRules",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_EarningRules_OrgID",
                table: "EarningRules",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_OrgAllowanceRules_OrgID",
                table: "OrgAllowanceRules",
                column: "OrgID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeductionRules");

            migrationBuilder.DropTable(
                name: "EarningRules");

            migrationBuilder.DropTable(
                name: "OrgAllowanceRules");

            migrationBuilder.DropColumn(
                name: "CompensationBasis",
                table: "OrgPayConfigs");

            migrationBuilder.DropColumn(
                name: "CustomUnitLabel",
                table: "OrgPayConfigs");

            migrationBuilder.DropColumn(
                name: "CustomWorkHours",
                table: "OrgPayConfigs");
        }
    }
}
