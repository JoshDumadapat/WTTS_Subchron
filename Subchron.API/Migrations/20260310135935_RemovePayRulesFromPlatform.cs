using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class RemovePayRulesFromPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeductionRules");

            migrationBuilder.DropTable(
                name: "EarningRules");

            migrationBuilder.DropTable(
                name: "OrgAllowanceRules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeductionRules",
                columns: table => new
                {
                    DeductionRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    AutoCompute = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Statutory"),
                    ComputeBasedOn = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "BasicPay"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DeductionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmployeeSharePercent = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    EmployerSharePercent = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    FormulaExpression = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasEmployeeShare = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    HasEmployerShare = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MaxDeductionAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: ""),
                    ScopeTagsJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeductionRules", x => x.DeductionRuleID);
                    table.ForeignKey(
                        name: "FK_DeductionRules_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EarningRules",
                columns: table => new
                {
                    EarningRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    AppliesTo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DayType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Any"),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HolidayCombo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Standard"),
                    IncludeInBenefitBase = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: ""),
                    RateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RateValue = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RestDayHandling = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "FollowAttendance"),
                    Scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "AllEmployees"),
                    ScopeTagsJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarningRules", x => x.EarningRuleID);
                    table.ForeignKey(
                        name: "FK_EarningRules_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgAllowanceRules",
                columns: table => new
                {
                    OrgAllowanceRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    AllowanceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    AttendanceDependent = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ComplianceNotes = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ProrateIfPartialPeriod = table.Column<bool>(type: "bit", nullable: false),
                    ScopeTagsJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
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
    }
}
