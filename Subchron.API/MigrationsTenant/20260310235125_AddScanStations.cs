using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class AddScanStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScanStations",
                columns: table => new
                {
                    ScanStationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    StationCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    QrEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IdEntryEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ScheduleMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedByUserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanStations", x => x.ScanStationID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScanStations_LocationID",
                table: "ScanStations",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_ScanStations_OrgID_StationCode",
                table: "ScanStations",
                columns: new[] { "OrgID", "StationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScanStations_OrgID_StationName",
                table: "ScanStations",
                columns: new[] { "OrgID", "StationName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanStations");
        }
    }
}
