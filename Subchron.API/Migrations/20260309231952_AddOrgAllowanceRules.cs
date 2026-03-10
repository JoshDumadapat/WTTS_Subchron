using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgAllowanceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgPayConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrgPayConfigs",
                columns: table => new
                {
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    ApplyTaxThreshold = table.Column<bool>(type: "bit", nullable: false),
                    BIRPeriod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BIRTableVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CutoffWindowsJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    EnableBIR = table.Column<bool>(type: "bit", nullable: false),
                    EnableIncomeTax = table.Column<bool>(type: "bit", nullable: false),
                    EnablePagIbig = table.Column<bool>(type: "bit", nullable: false),
                    EnablePhilHealth = table.Column<bool>(type: "bit", nullable: false),
                    EnableSSS = table.Column<bool>(type: "bit", nullable: false),
                    HoursPerDay = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LockAttendanceAfterCutoff = table.Column<bool>(type: "bit", nullable: false),
                    PagIbigRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PayCycle = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhilHealthRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ProrateNewHires = table.Column<bool>(type: "bit", nullable: false),
                    SSSEmployerPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ThirteenthMonthBasis = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ThirteenthMonthNotes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgPayConfigs", x => x.OrgID);
                });
        }
    }
}
