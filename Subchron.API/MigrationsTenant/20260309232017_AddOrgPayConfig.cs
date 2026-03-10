using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class AddOrgPayConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgPayConfigs");
        }
    }
}
