using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class OrgAttendanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrgAttendanceConfigs",
                columns: table => new
                {
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    PrimaryMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AllowManualEntry = table.Column<bool>(type: "bit", nullable: false),
                    RequireGeo = table.Column<bool>(type: "bit", nullable: false),
                    EnforceGeofence = table.Column<bool>(type: "bit", nullable: false),
                    RestrictByIp = table.Column<bool>(type: "bit", nullable: false),
                    PreventDoubleClockIn = table.Column<bool>(type: "bit", nullable: false),
                    AutoClockOutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoClockOutMaxHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    DefaultShiftTemplateCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgAttendanceConfigs", x => x.OrgID);
                });

            migrationBuilder.CreateTable(
                name: "OrgAttendanceOvertimePolicies",
                columns: table => new
                {
                    OrgAttendanceOvertimePolicyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Basis = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RestHolidayOverride = table.Column<bool>(type: "bit", nullable: false),
                    DailyThresholdHours = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    WeeklyThresholdHours = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    EarlyOtAllowed = table.Column<bool>(type: "bit", nullable: false),
                    MicroOtBufferMinutes = table.Column<int>(type: "int", nullable: false),
                    RequireHoursMet = table.Column<bool>(type: "bit", nullable: false),
                    FilingMode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PreApprovalRequired = table.Column<bool>(type: "bit", nullable: false),
                    AllowPostFiling = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalFlowType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AutoApprove = table.Column<bool>(type: "bit", nullable: false),
                    RoundingMinutes = table.Column<int>(type: "int", nullable: false),
                    RoundingDirection = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    MinimumBlockMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxPerDayHours = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    MaxPerWeekHours = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    LimitMode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OverrideRole = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ScopeMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NightDiffEnabled = table.Column<bool>(type: "bit", nullable: false),
                    NightDiffWindowStart = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    NightDiffWindowEnd = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    NightDiffMinimumMinutes = table.Column<int>(type: "int", nullable: false),
                    NightDiffExcludeBreaks = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgAttendanceOvertimePolicies", x => x.OrgAttendanceOvertimePolicyID);
                });

            migrationBuilder.CreateTable(
                name: "OrgAttendanceNightDiffExclusions",
                columns: table => new
                {
                    OrgAttendanceNightDiffExclusionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgAttendanceOvertimePolicyID = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Site = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgAttendanceNightDiffExclusions", x => x.OrgAttendanceNightDiffExclusionID);
                    table.ForeignKey(
                        name: "FK_OrgAttendanceNightDiffExclusions_OrgAttendanceOvertimePolicies_OrgAttendanceOvertimePolicyID",
                        column: x => x.OrgAttendanceOvertimePolicyID,
                        principalTable: "OrgAttendanceOvertimePolicies",
                        principalColumn: "OrgAttendanceOvertimePolicyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgAttendanceOvertimeApprovalSteps",
                columns: table => new
                {
                    OrgAttendanceOvertimeApprovalStepID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgAttendanceOvertimePolicyID = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgAttendanceOvertimeApprovalSteps", x => x.OrgAttendanceOvertimeApprovalStepID);
                    table.ForeignKey(
                        name: "FK_OrgAttendanceOvertimeApprovalSteps_OrgAttendanceOvertimePolicies_OrgAttendanceOvertimePolicyID",
                        column: x => x.OrgAttendanceOvertimePolicyID,
                        principalTable: "OrgAttendanceOvertimePolicies",
                        principalColumn: "OrgAttendanceOvertimePolicyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgAttendanceOvertimeBuckets",
                columns: table => new
                {
                    OrgAttendanceOvertimeBucketID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgAttendanceOvertimePolicyID = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    ThresholdHours = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    MaxHours = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    MinimumBlockMinutes = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgAttendanceOvertimeBuckets", x => x.OrgAttendanceOvertimeBucketID);
                    table.ForeignKey(
                        name: "FK_OrgAttendanceOvertimeBuckets_OrgAttendanceOvertimePolicies_OrgAttendanceOvertimePolicyID",
                        column: x => x.OrgAttendanceOvertimePolicyID,
                        principalTable: "OrgAttendanceOvertimePolicies",
                        principalColumn: "OrgAttendanceOvertimePolicyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgAttendanceOvertimeScopeFilters",
                columns: table => new
                {
                    OrgAttendanceOvertimeScopeFilterID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgAttendanceOvertimePolicyID = table.Column<int>(type: "int", nullable: false),
                    FilterType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgAttendanceOvertimeScopeFilters", x => x.OrgAttendanceOvertimeScopeFilterID);
                    table.ForeignKey(
                        name: "FK_OrgAttendanceOvertimeScopeFilters_OrgAttendanceOvertimePolicies_OrgAttendanceOvertimePolicyID",
                        column: x => x.OrgAttendanceOvertimePolicyID,
                        principalTable: "OrgAttendanceOvertimePolicies",
                        principalColumn: "OrgAttendanceOvertimePolicyID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgAttendanceConfigs_OrgID",
                table: "OrgAttendanceConfigs",
                column: "OrgID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgAttendanceNightDiffExclusions_OrgAttendanceOvertimePolicyID",
                table: "OrgAttendanceNightDiffExclusions",
                column: "OrgAttendanceOvertimePolicyID");

            migrationBuilder.CreateIndex(
                name: "IX_OrgAttendanceOvertimeApprovalSteps_OrgAttendanceOvertimePolicyID_Order",
                table: "OrgAttendanceOvertimeApprovalSteps",
                columns: new[] { "OrgAttendanceOvertimePolicyID", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_OrgAttendanceOvertimeBuckets_OrgAttendanceOvertimePolicyID_Key",
                table: "OrgAttendanceOvertimeBuckets",
                columns: new[] { "OrgAttendanceOvertimePolicyID", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgAttendanceOvertimePolicies_OrgID",
                table: "OrgAttendanceOvertimePolicies",
                column: "OrgID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgAttendanceOvertimeScopeFilters_OrgAttendanceOvertimePolicyID",
                table: "OrgAttendanceOvertimeScopeFilters",
                column: "OrgAttendanceOvertimePolicyID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgAttendanceConfigs");

            migrationBuilder.DropTable(
                name: "OrgAttendanceNightDiffExclusions");

            migrationBuilder.DropTable(
                name: "OrgAttendanceOvertimeApprovalSteps");

            migrationBuilder.DropTable(
                name: "OrgAttendanceOvertimeBuckets");

            migrationBuilder.DropTable(
                name: "OrgAttendanceOvertimeScopeFilters");

            migrationBuilder.DropTable(
                name: "OrgAttendanceOvertimePolicies");
        }
    }
}
