using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeAttendanceQrToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceQrIssuedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceQrToken",
                table: "Employees",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_AttendanceQrToken",
                table: "Employees",
                column: "AttendanceQrToken",
                unique: true,
                filter: "[AttendanceQrToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_AttendanceQrToken",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AttendanceQrIssuedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AttendanceQrToken",
                table: "Employees");
        }
    }
}
