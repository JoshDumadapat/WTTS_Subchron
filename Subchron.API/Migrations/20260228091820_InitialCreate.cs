using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoRequests",
                columns: table => new
                {
                    DemoRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    OrgSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DesiredMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReviewedByUserID = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrgID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoRequests", x => x.DemoRequestID);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Used = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    OrgID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrgCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.OrgID);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    PlanID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MaxEmployees = table.Column<int>(type: "int", nullable: false),
                    RetentionMonths = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.PlanID);
                });

            migrationBuilder.CreateTable(
                name: "SuperAdminAuditLogs",
                columns: table => new
                {
                    SuperAdminAuditLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    EntityID = table.Column<int>(type: "int", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperAdminAuditLogs", x => x.SuperAdminAuditLogID);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationPaymentMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    PayMongoPaymentMethodId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PayMongoCustomerId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationPaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationPaymentMethods_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                columns: table => new
                {
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AttendanceMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AllowManualEntry = table.Column<bool>(type: "bit", nullable: false),
                    RequireGeo = table.Column<bool>(type: "bit", nullable: false),
                    EnforceGeofence = table.Column<bool>(type: "bit", nullable: false),
                    DefaultGraceMinutes = table.Column<int>(type: "int", nullable: false),
                    RoundRule = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OTEnabled = table.Column<bool>(type: "bit", nullable: false),
                    OTThresholdHours = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    OTApprovalRequired = table.Column<bool>(type: "bit", nullable: false),
                    OTMaxHoursPerDay = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    LeaveEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LeaveApprovalRequired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.OrgID);
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Role = table.Column<int>(type: "int", nullable: false),
                    TotpEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TotpSecret = table.Column<byte[]>(type: "varbinary(64)", nullable: true),
                    RecoveryCodesHash = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    ExternalProvider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.CheckConstraint("CK_Users_Role_Valid", "[Role] IN (1,2,3,4,5,6,7)");
                    table.ForeignKey(
                        name: "FK_Users_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID");
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    SubscriptionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    PlanID = table.Column<int>(type: "int", nullable: false),
                    AttendanceMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ModePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FinalPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CONVERT(date, SYSUTCDATETIME())"),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionID);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Plans_PlanID",
                        column: x => x.PlanID,
                        principalTable: "Plans",
                        principalColumn: "PlanID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthLoginSessions",
                columns: table => new
                {
                    SessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthLoginSessions", x => x.SessionID);
                    table.ForeignKey(
                        name: "FK_AuthLoginSessions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    TokenID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.TokenID);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    SubscriptionID = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayMongoPaymentIntentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PayMongoPaymentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Subscriptions_SubscriptionID",
                        column: x => x.SubscriptionID,
                        principalTable: "Subscriptions",
                        principalColumn: "SubscriptionID");
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BillingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Expiry = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BillingEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BillingPhone = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    PayMongoPaymentMethodId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PayMongoCustomerId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    NameOnCard = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingRecords_Organizations_OrgID",
                        column: x => x.OrgID,
                        principalTable: "Organizations",
                        principalColumn: "OrgID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingRecords_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BillingRecords_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "PlanID", "BasePrice", "IsActive", "MaxEmployees", "PlanName", "RetentionMonths" },
                values: new object[,]
                {
                    { 1, 0m, true, 200, "Basic", 3 },
                    { 2, 0m, true, 400, "Standard", 12 },
                    { 3, 0m, true, 800, "Enterprise", 24 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "AvatarUrl", "CreatedAt", "Email", "EmailVerified", "ExternalId", "ExternalProvider", "IsActive", "LastLoginAt", "Name", "OrgID", "Password", "RecoveryCodesHash", "Role", "TotpSecret" },
                values: new object[] { 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ivanjoshdumadapat30@gmail.com", true, null, null, true, null, "Josh Dumadapat", null, "$2a$11$Aq6k23IxUxMsGwOMuwWQme7xSDkzu3N47OHmvsB44dQxdVLJLyMBe", null, 1, null });

            migrationBuilder.CreateIndex(
                name: "IX_AuthLoginSessions_UserID",
                table: "AuthLoginSessions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_OrgID",
                table: "BillingRecords",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_PaymentTransactionId",
                table: "BillingRecords",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_UserID",
                table: "BillingRecords",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_DemoRequests_OrgID",
                table: "DemoRequests",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationPaymentMethods_OrgID",
                table: "OrganizationPaymentMethods",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OrgCode",
                table: "Organizations",
                column: "OrgCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserID",
                table: "PasswordResetTokens",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CreatedAt",
                table: "PaymentTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrgID",
                table: "PaymentTransactions",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PayMongoPaymentIntentId",
                table: "PaymentTransactions",
                column: "PayMongoPaymentIntentId",
                unique: true,
                filter: "[PayMongoPaymentIntentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_SubscriptionID",
                table: "PaymentTransactions",
                column: "SubscriptionID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserID",
                table: "PaymentTransactions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_PlanName",
                table: "Plans",
                column: "PlanName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OrgID",
                table: "Subscriptions",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanID",
                table: "Subscriptions",
                column: "PlanID");

            migrationBuilder.CreateIndex(
                name: "IX_SuperAdminAuditLogs_CreatedAt",
                table: "SuperAdminAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SuperAdminAuditLogs_OrgID",
                table: "SuperAdminAuditLogs",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_SuperAdminAuditLogs_UserID",
                table: "SuperAdminAuditLogs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalProvider_ExternalId",
                table: "Users",
                columns: new[] { "ExternalProvider", "ExternalId" },
                unique: true,
                filter: "[ExternalProvider] IS NOT NULL AND [ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrgID",
                table: "Users",
                column: "OrgID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthLoginSessions");

            migrationBuilder.DropTable(
                name: "BillingRecords");

            migrationBuilder.DropTable(
                name: "DemoRequests");

            migrationBuilder.DropTable(
                name: "EmailVerificationCodes");

            migrationBuilder.DropTable(
                name: "OrganizationPaymentMethods");

            migrationBuilder.DropTable(
                name: "OrganizationSettings");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "SuperAdminAuditLogs");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
