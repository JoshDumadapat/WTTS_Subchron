using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class FixLeaveShiftFkNoAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Organizations_OrgID",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftAssignments_Organizations_OrgID",
                table: "ShiftAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Organizations_OrgID",
                table: "LeaveRequests",
                column: "OrgID",
                principalTable: "Organizations",
                principalColumn: "OrgID");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftAssignments_Organizations_OrgID",
                table: "ShiftAssignments",
                column: "OrgID",
                principalTable: "Organizations",
                principalColumn: "OrgID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Organizations_OrgID",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftAssignments_Organizations_OrgID",
                table: "ShiftAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Organizations_OrgID",
                table: "LeaveRequests",
                column: "OrgID",
                principalTable: "Organizations",
                principalColumn: "OrgID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftAssignments_Organizations_OrgID",
                table: "ShiftAssignments",
                column: "OrgID",
                principalTable: "Organizations",
                principalColumn: "OrgID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
