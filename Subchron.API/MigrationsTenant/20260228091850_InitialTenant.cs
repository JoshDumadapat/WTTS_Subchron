using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class InitialTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceCorrections",
                columns: table => new
                {
                    CorrectionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    AttendanceID = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserID = table.Column<int>(type: "int", nullable: false),
                    Reasons = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProposedTimeIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProposedTimeOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedByUserID = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceCorrections", x => x.CorrectionID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogs",
                columns: table => new
                {
                    AttendanceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    EmpID = table.Column<int>(type: "int", nullable: false),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MethodIn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MethodOut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GeoLat = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    GeoLong = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    GeoStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogs", x => x.AttendanceID);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepID);
                });

            migrationBuilder.CreateTable(
                name: "ExportJobs",
                columns: table => new
                {
                    ExportID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    ExportedByUserID = table.Column<int>(type: "int", nullable: false),
                    ExportType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.ExportID);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    LeaveTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultDaysPerYear = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.LeaveTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    LocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GeoLat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    GeoLong = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    RadiusMeters = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.LocationID);
                });

            migrationBuilder.CreateTable(
                name: "OvertimeRequests",
                columns: table => new
                {
                    OTRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    EmpID = table.Column<int>(type: "int", nullable: false),
                    OTDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedByUserID = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvertimeRequests", x => x.OTRequestID);
                });

            migrationBuilder.CreateTable(
                name: "SignupPendings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignupPendings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantAuditLogs",
                columns: table => new
                {
                    TenantAuditLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    EntityID = table.Column<int>(type: "int", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Meta = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAuditLogs", x => x.TenantAuditLogID);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmpID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    EmpNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Age = table.Column<int>(type: "int", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EmploymentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WorkState = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DateHired = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    StateProvince = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PhoneNormalized = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EmergencyContactRelation = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ArchivedByUserId = table.Column<int>(type: "int", nullable: true),
                    RestoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestoreReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RestoredByUserId = table.Column<int>(type: "int", nullable: true),
                    AttendanceQrToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AttendanceQrIssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmpID);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "Departments",
                        principalColumn: "DepID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    LeaveRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    EmpID = table.Column<int>(type: "int", nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedByUserID = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.LeaveRequestID);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmpID",
                        column: x => x.EmpID,
                        principalTable: "Employees",
                        principalColumn: "EmpID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftAssignments",
                columns: table => new
                {
                    ShiftAssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    EmpID = table.Column<int>(type: "int", nullable: false),
                    AssignmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftAssignments", x => x.ShiftAssignmentID);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_Employees_EmpID",
                        column: x => x.EmpID,
                        principalTable: "Employees",
                        principalColumn: "EmpID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_AttendanceID",
                table: "AttendanceCorrections",
                column: "AttendanceID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_OrgID",
                table: "AttendanceCorrections",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_RequestedByUserID",
                table: "AttendanceCorrections",
                column: "RequestedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_EmpID",
                table: "AttendanceLogs",
                column: "EmpID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_LogDate",
                table: "AttendanceLogs",
                column: "LogDate");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_OrgID",
                table: "AttendanceLogs",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_OrgID",
                table: "Departments",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_OrgID_DepartmentName",
                table: "Departments",
                columns: new[] { "OrgID", "DepartmentName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_AttendanceQrToken",
                table: "Employees",
                column: "AttendanceQrToken",
                unique: true,
                filter: "[AttendanceQrToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentID",
                table: "Employees",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OrgID",
                table: "Employees",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OrgID_EmpNumber",
                table: "Employees",
                columns: new[] { "OrgID", "EmpNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_OrgID_PhoneNormalized",
                table: "Employees",
                columns: new[] { "OrgID", "PhoneNormalized" },
                unique: true,
                filter: "[PhoneNormalized] IS NOT NULL AND [PhoneNormalized] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserID",
                table: "Employees",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_ExportedByUserID",
                table: "ExportJobs",
                column: "ExportedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_OrgID",
                table: "ExportJobs",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmpID",
                table: "LeaveRequests",
                column: "EmpID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_OrgID",
                table: "LeaveRequests",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_StartDate",
                table: "LeaveRequests",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status",
                table: "LeaveRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_OrgID",
                table: "LeaveTypes",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_OrgID",
                table: "Locations",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_EmpID",
                table: "OvertimeRequests",
                column: "EmpID");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_OrgID",
                table: "OvertimeRequests",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_OTDate",
                table: "OvertimeRequests",
                column: "OTDate");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_EmpID",
                table: "ShiftAssignments",
                column: "EmpID");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_OrgID_AssignmentDate",
                table: "ShiftAssignments",
                columns: new[] { "OrgID", "AssignmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_CreatedAt",
                table: "TenantAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_OrgID",
                table: "TenantAuditLogs",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_UserID",
                table: "TenantAuditLogs",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceCorrections");

            migrationBuilder.DropTable(
                name: "AttendanceLogs");

            migrationBuilder.DropTable(
                name: "ExportJobs");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LeaveTypes");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "OvertimeRequests");

            migrationBuilder.DropTable(
                name: "ShiftAssignments");

            migrationBuilder.DropTable(
                name: "SignupPendings");

            migrationBuilder.DropTable(
                name: "TenantAuditLogs");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
