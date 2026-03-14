using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class AddEmployeeCompensationAndDeductionProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BasePayAmount",
                table: "Employees",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CompensationBasisOverride",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "UseOrgDefault");

            migrationBuilder.AddColumn<string>(
                name: "CustomUnitLabel",
                table: "Employees",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomWorkHours",
                table: "Employees",
                type: "decimal(7,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeDeductionProfiles",
                columns: table => new
                {
                    EmployeeDeductionProfileID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    EmpID = table.Column<int>(type: "int", nullable: false),
                    DeductionRuleID = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "UseRule"),
                    Value = table.Column<decimal>(type: "decimal(12,4)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDeductionProfiles", x => x.EmployeeDeductionProfileID);
                    table.ForeignKey(
                        name: "FK_EmployeeDeductionProfiles_DeductionRules_DeductionRuleID",
                        column: x => x.DeductionRuleID,
                        principalTable: "DeductionRules",
                        principalColumn: "DeductionRuleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDeductionProfiles_Employees_EmpID",
                        column: x => x.EmpID,
                        principalTable: "Employees",
                        principalColumn: "EmpID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeductionProfiles_DeductionRuleID",
                table: "EmployeeDeductionProfiles",
                column: "DeductionRuleID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeductionProfiles_EmpID",
                table: "EmployeeDeductionProfiles",
                column: "EmpID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeductionProfiles_OrgID",
                table: "EmployeeDeductionProfiles",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeductionProfiles_OrgID_EmpID_DeductionRuleID",
                table: "EmployeeDeductionProfiles",
                columns: new[] { "OrgID", "EmpID", "DeductionRuleID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeDeductionProfiles");

            migrationBuilder.DropColumn(
                name: "BasePayAmount",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CompensationBasisOverride",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CustomUnitLabel",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CustomWorkHours",
                table: "Employees");
        }
    }
}
