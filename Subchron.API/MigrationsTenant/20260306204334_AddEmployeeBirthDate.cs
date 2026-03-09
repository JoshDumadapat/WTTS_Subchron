using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class AddEmployeeBirthDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Employees
                SET BirthDate = CASE
                    WHEN Age IS NULL THEN NULL
                    ELSE DATEFROMPARTS(
                        DATEPART(year, SYSUTCDATETIME()) - Age,
                        7,
                        1)
                END");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "Employees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Employees
                SET Age = CASE
                    WHEN BirthDate IS NULL THEN NULL
                    ELSE DATEDIFF(year, BirthDate, SYSUTCDATETIME()) -
                        CASE WHEN DATEFROMPARTS(DATEPART(year, SYSUTCDATETIME()), DATEPART(month, BirthDate), DATEPART(day, BirthDate)) > CAST(SYSUTCDATETIME() AS date)
                            THEN 1 ELSE 0 END
                END");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Employees");
        }
    }
}
