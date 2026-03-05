using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class ExtendLeaveTypesPolicyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccrualType",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AllowNegativeBalance",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AppliesTo",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CarryOverMaxDays",
                table: "LeaveTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CarryOverType",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireApproval",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireDocument",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccrualType",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AllowNegativeBalance",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AppliesTo",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CarryOverMaxDays",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CarryOverType",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "RequireApproval",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "RequireDocument",
                table: "LeaveTypes");
        }
    }
}
