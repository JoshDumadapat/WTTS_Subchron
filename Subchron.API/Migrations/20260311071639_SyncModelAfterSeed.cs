using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Subchron.API.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "OrgID", "CreatedAt", "OrgCode", "OrgName", "Status" },
                values: new object[,]
                {
                    { 101, new DateTime(2025, 12, 2, 0, 0, 0, 0, DateTimeKind.Utc), "NWS101", "Northwind Systems", "Active" },
                    { 102, new DateTime(2025, 12, 5, 0, 0, 0, 0, DateTimeKind.Utc), "SRG102", "Skyline Retail Group", "Active" },
                    { 103, new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Utc), "PDL103", "Pacific Data Labs", "Active" },
                    { 104, new DateTime(2025, 12, 11, 0, 0, 0, 0, DateTimeKind.Utc), "MHO104", "Metro Health Ops", "Active" },
                    { 105, new DateTime(2025, 12, 14, 0, 0, 0, 0, DateTimeKind.Utc), "VXL105", "Vertex Logistics", "Active" },
                    { 106, new DateTime(2025, 12, 17, 0, 0, 0, 0, DateTimeKind.Utc), "BPS106", "Bluepeak Services", "Trial" },
                    { 107, new DateTime(2025, 12, 20, 0, 0, 0, 0, DateTimeKind.Utc), "CRF107", "Crescent Foods", "Active" },
                    { 108, new DateTime(2025, 12, 23, 0, 0, 0, 0, DateTimeKind.Utc), "OBW108", "Omni Build Works", "Suspended" },
                    { 109, new DateTime(2025, 12, 26, 0, 0, 0, 0, DateTimeKind.Utc), "HFC109", "Harbor Finance Co", "Active" },
                    { 110, new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc), "SME110", "Summit Education", "Trial" }
                });

            migrationBuilder.InsertData(
                table: "SuperAdminExpenses",
                columns: new[] { "Id", "Amount", "Category", "CreatedAt", "CreatedByUserId", "Description", "Notes", "OccurredAt", "ReferenceNumber", "TaxAmount", "Tin" },
                values: new object[,]
                {
                    { 3001, 42000m, "Infrastructure", new DateTime(2025, 12, 5, 1, 0, 0, 0, DateTimeKind.Utc), 1, "Cloud hosting and compute", "December cloud invoice", new DateTime(2025, 12, 5, 0, 0, 0, 0, DateTimeKind.Utc), "EXP-2025-12-001", 5040m, "123-456-789-000" },
                    { 3002, 18000m, "Marketing", new DateTime(2025, 12, 15, 1, 0, 0, 0, DateTimeKind.Utc), 1, "Marketing and ads", "December campaigns", new DateTime(2025, 12, 15, 0, 0, 0, 0, DateTimeKind.Utc), "EXP-2025-12-002", 2160m, "123-456-789-000" },
                    { 3003, 24000m, "Infrastructure", new DateTime(2026, 1, 6, 1, 0, 0, 0, DateTimeKind.Utc), 1, "Platform monitoring tools", "January tools renewal", new DateTime(2026, 1, 6, 0, 0, 0, 0, DateTimeKind.Utc), "EXP-2026-01-001", 2880m, "123-456-789-000" },
                    { 3004, 15000m, "Personnel", new DateTime(2026, 1, 20, 1, 0, 0, 0, DateTimeKind.Utc), 1, "Operations and support", "Support contractor payout", new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), "EXP-2026-01-002", 1800m, "123-456-789-000" },
                    { 3005, 12000m, "Infrastructure", new DateTime(2026, 2, 4, 1, 0, 0, 0, DateTimeKind.Utc), 1, "Data backup storage", "Backup and archival", new DateTime(2026, 2, 4, 0, 0, 0, 0, DateTimeKind.Utc), "EXP-2026-02-001", 1440m, "123-456-789-000" },
                    { 3006, 9000m, "Office", new DateTime(2026, 2, 18, 1, 0, 0, 0, DateTimeKind.Utc), 1, "Office and admin", "Office and legal supplies", new DateTime(2026, 2, 18, 0, 0, 0, 0, DateTimeKind.Utc), "EXP-2026-02-002", 1080m, "123-456-789-000" },
                    { 3007, 26000m, "Infrastructure", new DateTime(2026, 3, 3, 1, 0, 0, 0, DateTimeKind.Utc), 1, "Security and compliance tools", "March security stack", new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), "EXP-2026-03-001", 3120m, "123-456-789-000" },
                    { 3008, 17000m, "Marketing", new DateTime(2026, 3, 12, 1, 0, 0, 0, DateTimeKind.Utc), 1, "Sales outreach and events", "March partner events", new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), "EXP-2026-03-002", 2040m, "123-456-789-000" }
                });

            migrationBuilder.InsertData(
                table: "OrganizationSettings",
                columns: new[] { "OrgID", "AttendanceMode", "CreatedAt", "Currency", "DefaultShiftTemplateCode", "Timezone", "UpdatedAt" },
                values: new object[,]
                {
                    { 101, "QR", new DateTime(2025, 12, 2, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "DAY", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 102, "Hybrid", new DateTime(2025, 12, 5, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "DAY", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 103, "Biometric", new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "NIGHT", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 104, "QR", new DateTime(2025, 12, 11, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "DAY", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 105, "Hybrid", new DateTime(2025, 12, 14, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "DAY", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 106, "QR", new DateTime(2025, 12, 17, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "DAY", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 107, "Biometric", new DateTime(2025, 12, 20, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "NIGHT", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 108, "QR", new DateTime(2025, 12, 23, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "DAY", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 109, "Hybrid", new DateTime(2025, 12, 26, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "DAY", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 110, "QR", new DateTime(2025, 12, 29, 0, 0, 0, 0, DateTimeKind.Utc), "PHP", "DAY", "Asia/Manila", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[] { "SubscriptionID", "AttendanceMode", "BasePrice", "BillingCycle", "EndDate", "FinalPrice", "ModePrice", "OrgID", "PlanID", "StartDate", "Status" },
                values: new object[,]
                {
                    { 1001, "QR", 2499m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 101, 1, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1002, "QR", 5999m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 101, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1003, "QR", 5999m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 101, 2, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1004, "Hybrid", 8999m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 8999m, 0m, 101, 3, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 1005, "QR", 2499m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 102, 1, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1006, "QR", 2499m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 102, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1007, "Hybrid", 5999m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 102, 2, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1008, "Hybrid", 5999m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 102, 2, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 1009, "Biometric", 5999m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 103, 2, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1010, "Biometric", 5999m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 103, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1011, "Biometric", 8999m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 8999m, 0m, 103, 3, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1012, "Biometric", 8999m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 8999m, 0m, 103, 3, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 1013, "QR", 2499m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 104, 1, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1014, "QR", 2499m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 104, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1015, "QR", 2499m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 104, 1, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1016, "QR", 5999m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 104, 2, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 1017, "Hybrid", 5999m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 105, 2, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1018, "Hybrid", 5999m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 105, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1019, "Hybrid", 8999m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 8999m, 0m, 105, 3, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1020, "Hybrid", 8999m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 8999m, 0m, 105, 3, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 1021, "QR", 2499m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 106, 1, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1022, "QR", 2499m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 106, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1023, "QR", 2499m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 106, 1, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1024, "QR", 2499m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 106, 1, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Trial" },
                    { 1025, "Biometric", 2499m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 107, 1, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1026, "Biometric", 5999m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 107, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1027, "Biometric", 5999m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 107, 2, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1028, "Biometric", 8999m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 8999m, 0m, 107, 3, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 1029, "QR", 5999m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 108, 2, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1030, "QR", 5999m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 108, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1031, "QR", 5999m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 108, 2, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1032, "QR", 5999m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 108, 2, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Suspended" },
                    { 1033, "Hybrid", 2499m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 109, 1, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1034, "Hybrid", 2499m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 109, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1035, "Hybrid", 5999m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 109, 2, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1036, "Hybrid", 5999m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 109, 2, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 1037, "QR", 2499m, "Monthly", new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 110, 1, new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1038, "QR", 2499m, "Monthly", new DateTime(2026, 1, 31, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 110, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1039, "QR", 2499m, "Monthly", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 2499m, 0m, 110, 1, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Expired" },
                    { 1040, "QR", 5999m, "Monthly", new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Utc), 5999m, 0m, 110, 2, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Trial" }
                });

            migrationBuilder.InsertData(
                table: "PaymentTransactions",
                columns: new[] { "Id", "Amount", "CreatedAt", "Currency", "Description", "FailureCode", "FailureMessage", "OrgID", "PayMongoPaymentId", "PayMongoPaymentIntentId", "Status", "SubscriptionID", "UpdatedAt", "UserID" },
                values: new object[,]
                {
                    { 2001, 2499m, new DateTime(2025, 12, 1, 8, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 101, "seed-pay-2001", "seed-pi-2001", "paid", 1001, null, null },
                    { 2002, 5999m, new DateTime(2026, 1, 1, 8, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 101, "seed-pay-2002", "seed-pi-2002", "paid", 1002, null, null },
                    { 2003, 5999m, new DateTime(2026, 2, 1, 8, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 101, "seed-pay-2003", "seed-pi-2003", "paid", 1003, null, null },
                    { 2004, 8999m, new DateTime(2026, 3, 1, 8, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Enterprise", null, null, 101, "seed-pay-2004", "seed-pi-2004", "paid", 1004, null, null },
                    { 2005, 2499m, new DateTime(2025, 12, 1, 8, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 102, "seed-pay-2005", "seed-pi-2005", "paid", 1005, null, null },
                    { 2006, 2499m, new DateTime(2026, 1, 1, 8, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 102, "seed-pay-2006", "seed-pi-2006", "paid", 1006, null, null },
                    { 2007, 5999m, new DateTime(2026, 2, 1, 8, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 102, "seed-pay-2007", "seed-pi-2007", "paid", 1007, null, null },
                    { 2008, 5999m, new DateTime(2026, 3, 1, 8, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 102, "seed-pay-2008", "seed-pi-2008", "paid", 1008, null, null },
                    { 2009, 5999m, new DateTime(2025, 12, 1, 8, 30, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 103, "seed-pay-2009", "seed-pi-2009", "paid", 1009, null, null },
                    { 2010, 5999m, new DateTime(2026, 1, 1, 8, 30, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 103, "seed-pay-2010", "seed-pi-2010", "paid", 1010, null, null },
                    { 2011, 8999m, new DateTime(2026, 2, 1, 8, 30, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Enterprise", null, null, 103, "seed-pay-2011", "seed-pi-2011", "paid", 1011, null, null },
                    { 2012, 8999m, new DateTime(2026, 3, 1, 8, 30, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Enterprise", null, null, 103, "seed-pay-2012", "seed-pi-2012", "paid", 1012, null, null },
                    { 2013, 2499m, new DateTime(2025, 12, 1, 8, 45, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 104, "seed-pay-2013", "seed-pi-2013", "paid", 1013, null, null },
                    { 2014, 2499m, new DateTime(2026, 1, 1, 8, 45, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 104, "seed-pay-2014", "seed-pi-2014", "paid", 1014, null, null },
                    { 2015, 2499m, new DateTime(2026, 2, 1, 8, 45, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 104, "seed-pay-2015", "seed-pi-2015", "paid", 1015, null, null },
                    { 2016, 5999m, new DateTime(2026, 3, 1, 8, 45, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 104, "seed-pay-2016", "seed-pi-2016", "paid", 1016, null, null },
                    { 2017, 5999m, new DateTime(2025, 12, 1, 9, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 105, "seed-pay-2017", "seed-pi-2017", "paid", 1017, null, null },
                    { 2018, 5999m, new DateTime(2026, 1, 1, 9, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 105, "seed-pay-2018", "seed-pi-2018", "paid", 1018, null, null },
                    { 2019, 8999m, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Enterprise", null, null, 105, "seed-pay-2019", "seed-pi-2019", "paid", 1019, null, null },
                    { 2020, 8999m, new DateTime(2026, 3, 1, 9, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Enterprise", null, null, 105, "seed-pay-2020", "seed-pi-2020", "paid", 1020, null, null },
                    { 2021, 2499m, new DateTime(2025, 12, 1, 9, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 106, "seed-pay-2021", "seed-pi-2021", "paid", 1021, null, null },
                    { 2022, 2499m, new DateTime(2026, 1, 1, 9, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 106, "seed-pay-2022", "seed-pi-2022", "paid", 1022, null, null },
                    { 2023, 2499m, new DateTime(2026, 2, 1, 9, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 106, "seed-pay-2023", "seed-pi-2023", "paid", 1023, null, null },
                    { 2024, 2499m, new DateTime(2026, 3, 1, 9, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 106, "seed-pay-2024", "seed-pi-2024", "paid", 1024, null, null },
                    { 2025, 2499m, new DateTime(2025, 12, 1, 9, 30, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 107, "seed-pay-2025", "seed-pi-2025", "paid", 1025, null, null },
                    { 2026, 5999m, new DateTime(2026, 1, 1, 9, 30, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 107, "seed-pay-2026", "seed-pi-2026", "paid", 1026, null, null },
                    { 2027, 5999m, new DateTime(2026, 2, 1, 9, 30, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 107, "seed-pay-2027", "seed-pi-2027", "paid", 1027, null, null },
                    { 2028, 8999m, new DateTime(2026, 3, 1, 9, 30, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Enterprise", null, null, 107, "seed-pay-2028", "seed-pi-2028", "paid", 1028, null, null },
                    { 2029, 5999m, new DateTime(2025, 12, 1, 9, 45, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 108, "seed-pay-2029", "seed-pi-2029", "paid", 1029, null, null },
                    { 2030, 5999m, new DateTime(2026, 1, 1, 9, 45, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 108, "seed-pay-2030", "seed-pi-2030", "paid", 1030, null, null },
                    { 2031, 5999m, new DateTime(2026, 2, 1, 9, 45, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 108, "seed-pay-2031", "seed-pi-2031", "paid", 1031, null, null },
                    { 2032, 5999m, new DateTime(2026, 3, 1, 9, 45, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 108, "seed-pay-2032", "seed-pi-2032", "paid", 1032, null, null },
                    { 2033, 2499m, new DateTime(2025, 12, 1, 10, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 109, "seed-pay-2033", "seed-pi-2033", "paid", 1033, null, null },
                    { 2034, 2499m, new DateTime(2026, 1, 1, 10, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 109, "seed-pay-2034", "seed-pi-2034", "paid", 1034, null, null },
                    { 2035, 5999m, new DateTime(2026, 2, 1, 10, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 109, "seed-pay-2035", "seed-pi-2035", "paid", 1035, null, null },
                    { 2036, 5999m, new DateTime(2026, 3, 1, 10, 0, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 109, "seed-pay-2036", "seed-pi-2036", "paid", 1036, null, null },
                    { 2037, 2499m, new DateTime(2025, 12, 1, 10, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 110, "seed-pay-2037", "seed-pi-2037", "paid", 1037, null, null },
                    { 2038, 2499m, new DateTime(2026, 1, 1, 10, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 110, "seed-pay-2038", "seed-pi-2038", "paid", 1038, null, null },
                    { 2039, 2499m, new DateTime(2026, 2, 1, 10, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Basic", null, null, 110, "seed-pay-2039", "seed-pi-2039", "paid", 1039, null, null },
                    { 2040, 5999m, new DateTime(2026, 3, 1, 10, 15, 0, 0, DateTimeKind.Utc), "PHP", "Subchron - Standard", null, null, 110, "seed-pay-2040", "seed-pi-2040", "paid", 1040, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "OrganizationSettings",
                keyColumn: "OrgID",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2001);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2002);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2003);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2004);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2005);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2006);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2007);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2008);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2009);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2010);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2011);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2012);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2013);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2014);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2015);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2016);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2017);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2018);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2019);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2020);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2021);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2022);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2023);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2024);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2025);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2026);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2027);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2028);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2029);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2030);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2031);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2032);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2033);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2034);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2035);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2036);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2037);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2038);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2039);

            migrationBuilder.DeleteData(
                table: "PaymentTransactions",
                keyColumn: "Id",
                keyValue: 2040);

            migrationBuilder.DeleteData(
                table: "SuperAdminExpenses",
                keyColumn: "Id",
                keyValue: 3001);

            migrationBuilder.DeleteData(
                table: "SuperAdminExpenses",
                keyColumn: "Id",
                keyValue: 3002);

            migrationBuilder.DeleteData(
                table: "SuperAdminExpenses",
                keyColumn: "Id",
                keyValue: 3003);

            migrationBuilder.DeleteData(
                table: "SuperAdminExpenses",
                keyColumn: "Id",
                keyValue: 3004);

            migrationBuilder.DeleteData(
                table: "SuperAdminExpenses",
                keyColumn: "Id",
                keyValue: 3005);

            migrationBuilder.DeleteData(
                table: "SuperAdminExpenses",
                keyColumn: "Id",
                keyValue: 3006);

            migrationBuilder.DeleteData(
                table: "SuperAdminExpenses",
                keyColumn: "Id",
                keyValue: 3007);

            migrationBuilder.DeleteData(
                table: "SuperAdminExpenses",
                keyColumn: "Id",
                keyValue: 3008);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1005);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1006);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1007);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1008);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1009);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1010);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1011);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1012);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1013);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1014);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1015);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1016);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1017);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1018);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1019);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1020);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1021);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1022);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1023);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1024);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1025);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1026);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1027);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1028);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1029);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1030);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1031);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1032);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1033);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1034);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1035);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1036);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1037);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1038);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1039);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "SubscriptionID",
                keyValue: 1040);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "OrgID",
                keyValue: 110);
        }
    }
}
