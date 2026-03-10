using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class OrgHolidayConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrgHolidayConfigs",
                columns: table => new
                {
                    OrgHolidayConfigID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    HolidayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Active"),
                    ScopeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Nationwide"),
                    SourceTag = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "ManualEntry"),
                    OverlapStrategy = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "HighestPrecedence"),
                    Precedence = table.Column<int>(type: "int", nullable: false),
                    IncludeAttendance = table.Column<bool>(type: "bit", nullable: false),
                    NonWorkingDay = table.Column<bool>(type: "bit", nullable: false),
                    AllowWork = table.Column<bool>(type: "bit", nullable: false),
                    ApplyRestDayRules = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceClassification = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, defaultValue: "Holiday"),
                    RestDayAttendanceClassification = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    IncludePayroll = table.Column<bool>(type: "bit", nullable: false),
                    UsePayRules = table.Column<bool>(type: "bit", nullable: false),
                    PaidWhenUnworked = table.Column<bool>(type: "bit", nullable: false),
                    PayrollClassification = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, defaultValue: "RegularHoliday"),
                    RestDayPayrollClassification = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PayrollRuleId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RestDayPayrollRuleId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ReferenceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferenceUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OfficialTag = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ScopeValuesJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    EmployeeGroupScopeJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    PayrollNotes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: ""),
                    Notes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: ""),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgHolidayConfigs", x => x.OrgHolidayConfigID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgHolidayConfigs_OrgID_HolidayDate",
                table: "OrgHolidayConfigs",
                columns: new[] { "OrgID", "HolidayDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgHolidayConfigs");
        }
    }
}
