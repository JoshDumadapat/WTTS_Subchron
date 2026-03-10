using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class DepartmentAndEmployeeDefaultAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedLocationId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedShiftTemplateCode",
                table: "Employees",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultLocationId",
                table: "Departments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultShiftTemplateCode",
                table: "Departments",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedLocationId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AssignedShiftTemplateCode",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DefaultLocationId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DefaultShiftTemplateCode",
                table: "Departments");
        }
    }
}
