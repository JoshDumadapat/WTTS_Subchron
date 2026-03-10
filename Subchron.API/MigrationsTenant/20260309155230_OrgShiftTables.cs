using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class OrgShiftTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrgShiftTemplates",
                columns: table => new
                {
                    OrgShiftTemplateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FixedStartTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    FixedEndTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    FixedBreakMinutes = table.Column<int>(type: "int", nullable: true),
                    FixedGraceMinutes = table.Column<int>(type: "int", nullable: true),
                    FlexibleEarliestStart = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    FlexibleLatestEnd = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    FlexibleRequiredDailyHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FlexibleMaxDailyHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    OpenRequiredWeeklyHours = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgShiftTemplates", x => x.OrgShiftTemplateID);
                });

            migrationBuilder.CreateTable(
                name: "OrgShiftTemplateBreaks",
                columns: table => new
                {
                    OrgShiftTemplateBreakID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgShiftTemplateID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StartTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    EndTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgShiftTemplateBreaks", x => x.OrgShiftTemplateBreakID);
                    table.ForeignKey(
                        name: "FK_OrgShiftTemplateBreaks_OrgShiftTemplates_OrgShiftTemplateID",
                        column: x => x.OrgShiftTemplateID,
                        principalTable: "OrgShiftTemplates",
                        principalColumn: "OrgShiftTemplateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgShiftTemplateDayOverrides",
                columns: table => new
                {
                    OrgShiftTemplateDayOverrideID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgShiftTemplateID = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsOffDay = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgShiftTemplateDayOverrides", x => x.OrgShiftTemplateDayOverrideID);
                    table.ForeignKey(
                        name: "FK_OrgShiftTemplateDayOverrides_OrgShiftTemplates_OrgShiftTemplateID",
                        column: x => x.OrgShiftTemplateID,
                        principalTable: "OrgShiftTemplates",
                        principalColumn: "OrgShiftTemplateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgShiftTemplateWorkDays",
                columns: table => new
                {
                    OrgShiftTemplateWorkDayID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgShiftTemplateID = table.Column<int>(type: "int", nullable: false),
                    DayCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgShiftTemplateWorkDays", x => x.OrgShiftTemplateWorkDayID);
                    table.ForeignKey(
                        name: "FK_OrgShiftTemplateWorkDays_OrgShiftTemplates_OrgShiftTemplateID",
                        column: x => x.OrgShiftTemplateID,
                        principalTable: "OrgShiftTemplates",
                        principalColumn: "OrgShiftTemplateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgShiftTemplateOverrideWindows",
                columns: table => new
                {
                    OrgShiftTemplateOverrideWindowID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgShiftTemplateDayOverrideID = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    EndTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgShiftTemplateOverrideWindows", x => x.OrgShiftTemplateOverrideWindowID);
                    table.ForeignKey(
                        name: "FK_OrgShiftTemplateOverrideWindows_OrgShiftTemplateDayOverrides_OrgShiftTemplateDayOverrideID",
                        column: x => x.OrgShiftTemplateDayOverrideID,
                        principalTable: "OrgShiftTemplateDayOverrides",
                        principalColumn: "OrgShiftTemplateDayOverrideID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgShiftTemplateBreaks_OrgShiftTemplateID",
                table: "OrgShiftTemplateBreaks",
                column: "OrgShiftTemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_OrgShiftTemplateDayOverrides_OrgShiftTemplateID",
                table: "OrgShiftTemplateDayOverrides",
                column: "OrgShiftTemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_OrgShiftTemplateOverrideWindows_OrgShiftTemplateDayOverrideID",
                table: "OrgShiftTemplateOverrideWindows",
                column: "OrgShiftTemplateDayOverrideID");

            migrationBuilder.CreateIndex(
                name: "IX_OrgShiftTemplates_OrgID_Code",
                table: "OrgShiftTemplates",
                columns: new[] { "OrgID", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgShiftTemplateWorkDays_OrgShiftTemplateID",
                table: "OrgShiftTemplateWorkDays",
                column: "OrgShiftTemplateID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgShiftTemplateBreaks");

            migrationBuilder.DropTable(
                name: "OrgShiftTemplateOverrideWindows");

            migrationBuilder.DropTable(
                name: "OrgShiftTemplateWorkDays");

            migrationBuilder.DropTable(
                name: "OrgShiftTemplateDayOverrides");

            migrationBuilder.DropTable(
                name: "OrgShiftTemplates");
        }
    }
}
