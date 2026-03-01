using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPayComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EarningRules",
                columns: table => new
                {
                    EarningRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppliesTo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RateValue = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IncludeInBenefitBase = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarningRules", x => x.EarningRuleID);
                    table.ForeignKey(
                        name: "FK_EarningRules_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeductionRules",
                columns: table => new
                {
                    DeductionRuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeductionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    FormulaExpression = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasEmployerShare = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasEmployeeShare = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EmployerSharePercent = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    EmployeeSharePercent = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    AutoCompute = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeductionRules", x => x.DeductionRuleID);
                    table.ForeignKey(
                        name: "FK_DeductionRules_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EarningRules_OrgID",
                table: "EarningRules",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_DeductionRules_OrgID",
                table: "DeductionRules",
                column: "OrgID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EarningRules");
            migrationBuilder.DropTable(name: "DeductionRules");
        }
    }
}
