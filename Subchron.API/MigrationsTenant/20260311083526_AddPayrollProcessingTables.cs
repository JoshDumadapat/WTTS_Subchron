using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class AddPayrollProcessingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    PayrollRunID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayCycle = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "SemiMonthly"),
                    CompensationBasis = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Monthly"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Processed"),
                    ProcessedCount = table.Column<int>(type: "int", nullable: false),
                    TotalGrossPay = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    TotalNetPay = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedByUserID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.PayrollRunID);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunEmployees",
                columns: table => new
                {
                    PayrollRunEmployeeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRunID = table.Column<int>(type: "int", nullable: false),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    EmpID = table.Column<int>(type: "int", nullable: false),
                    EmpNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, defaultValue: ""),
                    WorkedHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    BasePay = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    OvertimePay = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Allowances = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Deductions = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    FormulaSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    BreakdownJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunEmployees", x => x.PayrollRunEmployeeID);
                    table.ForeignKey(
                        name: "FK_PayrollRunEmployees_Employees_EmpID",
                        column: x => x.EmpID,
                        principalTable: "Employees",
                        principalColumn: "EmpID");
                    table.ForeignKey(
                        name: "FK_PayrollRunEmployees_PayrollRuns_PayrollRunID",
                        column: x => x.PayrollRunID,
                        principalTable: "PayrollRuns",
                        principalColumn: "PayrollRunID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunEmployees_EmpID",
                table: "PayrollRunEmployees",
                column: "EmpID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunEmployees_OrgID",
                table: "PayrollRunEmployees",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunEmployees_PayrollRunID",
                table: "PayrollRunEmployees",
                column: "PayrollRunID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_OrgID",
                table: "PayrollRuns",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_ProcessedAt",
                table: "PayrollRuns",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollRunEmployees");

            migrationBuilder.DropTable(
                name: "PayrollRuns");
        }
    }
}
