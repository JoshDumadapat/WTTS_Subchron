# Complete Migration Plan: Two Physical SQL Server Databases

## Prerequisites

- **DefaultConnection** (Platform DB): e.g. `Server=...;Database=Subchron;...`
- **TenantConnection** (Tenant DB): e.g. `Server=...;Database=SubchronTenant;...` (same or different server)

Add both to `appsettings.json` (or User Secrets):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Subchron;Trusted_Connection=True;TrustServerCertificate=True;",
    "TenantConnection": "Server=(localdb)\\mssqllocaldb;Database=SubchronTenant;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

---

## Phase 0 — Goal: TenantDbContext + connection strings + DI

**Success checks:** App starts; `TenantDbContext` registered; both connection strings in config.

**Done in code:** Program.cs registers TenantDbContext with TenantConnection; SubchronDbContext with DefaultConnection.

**Commands:** None (code already applied).

**Errors to avoid:** Do not run `Update-Database` for TenantDbContext until Tenant DB exists and Phase 2 migration is added.

---

## Phase 1 — Goal: Tenant entities (no cross-DB navs) + TenantAuditLog

**Success checks:** Build succeeds; Employee/Department/LeaveRequest/ShiftAssignment have no Organization/User navs; TenantAuditLog entity exists.

**Done in code:** Entity classes and TenantDbContext configuration (already applied).

---

## Phase 2 — Goal: Create Tenant DB schema (separate database)

**Success checks:** Tenant DB has tables: Employees, Departments, LeaveRequests, ShiftAssignments, TenantAuditLogs, and `__EFMigrationsHistory`.

### 2.1 Create Tenant DB (if using a new database)

Run once (SQL Server or SSMS), or use a script:

```sql
-- Create Tenant database (run against your server)
CREATE DATABASE SubchronTenant;
GO
```

### 2.2 Add and apply Tenant migrations (output in MigrationsTenant)

**Package Manager Console (PMC):**

```powershell
# Add first migration for Tenant DB (output dir = MigrationsTenant)
Add-Migration InitialTenant -Context TenantDbContext -OutputDir MigrationsTenant -Project Subchron.API -StartupProject Subchron.API

# Apply to Tenant DB (TenantConnection)
Update-Database -Context TenantDbContext -Project Subchron.API -StartupProject Subchron.API
```

**CLI:**

```bash
cd Subchron.API
dotnet ef migrations add InitialTenant --context TenantDbContext --output-dir MigrationsTenant
dotnet ef database update --context TenantDbContext
```

**Errors to avoid:** Ensure `TenantConnection` is set (e.g. in appsettings.Development.json) so the design-time factory and startup project can find it.

---

## Phase 3 — Goal: IAuditService split + SuperAdminAuditLog + claims helpers

**Success checks:** LogTenantAsync writes to TenantAuditLogs; LogSuperAdminAsync writes to SuperAdminAuditLogs; ClaimsHelpers.GetOrgID/GetUserID/IsSuperAdmin work.

**Done in code:** IAuditService, AuditService, SuperAdminAuditLog, ClaimsHelpers (already applied).

---

## Phase 4 — Goal: Platform DB has SuperAdminAuditLogs (and optional SignupPending/EmailVerificationCode/OrganizationPaymentMethod); do NOT drop tenant tables yet

**Success checks:** Platform DB has SuperAdminAuditLogs table; existing tenant tables (Employees, Departments, etc.) still exist for data copy.

### 4.1 Add Platform migration (adds new tables only)

**PMC:**

```powershell
Add-Migration AddSuperAdminAuditLogAndPlatformTables -Context SubchronDbContext -Project Subchron.API -StartupProject Subchron.API
Update-Database -Context SubchronDbContext -Project Subchron.API -StartupProject Subchron.API
```

**CLI:**

```bash
dotnet ef migrations add AddSuperAdminAuditLogAndPlatformTables --context SubchronDbContext
dotnet ef database update --context SubchronDbContext
```

**Errors to avoid:** Do not remove or rename existing migrations; this migration only adds new tables (SuperAdminAuditLogs, SignupPendings, EmailVerificationCodes, OrganizationPaymentMethods if configured).

---

## Phase 5 — Goal: Controllers use TenantDbContext; audit split; TenantAuditLogs forbidden for SuperAdmin

**Success checks:** Tenant controllers use TenantDbContext; tenant audit list returns Forbid for SuperAdmin; OrgID filter on tenant audit.

**Done in code:** Controllers and AuditLogsController (already applied).

---

## Phase 6 — Data migration (safe, no data loss)

**Order:**

1. Create Tenant DB schema (Phase 2 — already done).
2. Copy tenant operational data from Platform DB to Tenant DB (SQL below).
3. Verify counts and sample records.
4. Switch app to use Tenant DB for tenant operations (already using TenantDbContext).
5. Only then: apply Platform migration that DROPS tenant tables and keeps only platform tables.

### 6.1 Copy tenant data (run against Platform DB, insert into Tenant DB)

Use **linked server** or **same-server two databases** or run from a script that connects to both. Below: **same server, two databases** (Platform = `Subchron`, Tenant = `SubchronTenant`). Adjust database names to match your connection strings.

```sql
-- ============================================================
-- Run this from a session that can access both databases.
-- Replace Subchron = Platform DB, SubchronTenant = Tenant DB
-- if your names differ.
-- ============================================================

USE SubchronTenant; -- Target
GO

-- Disable FK checks temporarily for insert order (LeaveRequests/ShiftAssignments depend on Employees)
ALTER TABLE LeaveRequests NOCHECK CONSTRAINT ALL;
ALTER TABLE ShiftAssignments NOCHECK CONSTRAINT ALL;
GO

-- 1) Copy Employees
INSERT INTO SubchronTenant.dbo.Employees (
    OrgID, UserID, EmpNumber, LastName, FirstName, MiddleName, Age, Gender, Role, WorkState, EmploymentType,
    DateHired, AddressLine1, AddressLine2, City, StateProvince, PostalCode, Country, Phone, PhoneNormalized,
    EmergencyContactName, EmergencyContactPhone, EmergencyContactRelation, IsArchived, ArchivedAt, ArchivedReason,
    ArchivedByUserId, RestoredAt, RestoreReason, RestoredByUserId, AttendanceQrToken, AttendanceQrIssuedAt,
    CreatedAt, UpdatedAt, CreatedByUserId, UpdatedByUserId, DepartmentID
)
SELECT
    OrgID, UserID, EmpNumber, LastName, FirstName, MiddleName, Age, Gender, Role, WorkState, EmploymentType,
    DateHired, AddressLine1, AddressLine2, City, StateProvince, PostalCode, Country, Phone, PhoneNormalized,
    EmergencyContactName, EmergencyContactPhone, EmergencyContactRelation, IsArchived, ArchivedAt, ArchivedReason,
    ArchivedByUserId, RestoredAt, RestoreReason, RestoredByUserId, AttendanceQrToken, AttendanceQrIssuedAt,
    CreatedAt, UpdatedAt, CreatedByUserId, UpdatedByUserId, DepartmentID
FROM Subchron.dbo.Employees;
-- If identity insert needed for EmpID continuity (optional):
-- SET IDENTITY_INSERT SubchronTenant.dbo.Employees ON;
-- (include EmpID in INSERT/SELECT)
-- SET IDENTITY_INSERT SubchronTenant.dbo.Employees OFF;

-- 2) Copy Departments
INSERT INTO SubchronTenant.dbo.Departments (OrgID, DepartmentName, Description, IsActive, CreatedAt)
SELECT OrgID, DepartmentName, Description, IsActive, CreatedAt
FROM Subchron.dbo.Departments;
-- Optional: SET IDENTITY_INSERT ... ON/OFF for DepID

-- 3) Copy LeaveRequests (EmpID references Employees in Tenant DB; same IDs if you did identity insert)
INSERT INTO SubchronTenant.dbo.LeaveRequests (
    OrgID, EmpID, LeaveType, StartDate, EndDate, Status, Reason, ReviewedByUserID, ReviewedAt, ReviewNotes, CreatedAt, CreatedByUserID
)
SELECT OrgID, EmpID, LeaveType, StartDate, EndDate, Status, Reason, ReviewedByUserID, ReviewedAt, ReviewNotes, CreatedAt, CreatedByUserID
FROM Subchron.dbo.LeaveRequests;

-- 4) Copy ShiftAssignments
INSERT INTO SubchronTenant.dbo.ShiftAssignments (
    OrgID, EmpID, AssignmentDate, StartTime, EndTime, Notes, CreatedAt, CreatedByUserID, UpdatedAt, UpdatedByUserID
)
SELECT OrgID, EmpID, AssignmentDate, StartTime, EndTime, Notes, CreatedAt, CreatedByUserID, UpdatedAt, UpdatedByUserID
FROM Subchron.dbo.ShiftAssignments;

-- 5) Split old AuditLogs: tenant operational -> TenantAuditLogs; platform/superadmin -> SuperAdminAuditLogs
--    Example: tenant actions = Department*, Employee*, Leave*, Shift*; rest or by OrgID=null -> SuperAdmin
INSERT INTO SubchronTenant.dbo.TenantAuditLogs (OrgID, UserID, Action, EntityName, EntityID, Details, Meta, IpAddress, UserAgent, CreatedAt)
SELECT
    COALESCE(OrgID, 0), UserID, Action, EntityName, EntityID, Details, NULL, IpAddress, UserAgent, CreatedAt
FROM Subchron.dbo.AuditLogs
WHERE OrgID IS NOT NULL
  AND (Action LIKE 'Department%' OR Action LIKE 'Employee%' OR Action LIKE 'Leave%' OR Action LIKE 'Shift%' OR EntityName IN ('Department','Employee','LeaveRequest','ShiftAssignment'));

INSERT INTO Subchron.dbo.SuperAdminAuditLogs (OrgID, UserID, Action, EntityName, EntityID, Details, IpAddress, UserAgent, CreatedAt)
SELECT OrgID, UserID, Action, EntityName, EntityID, Details, IpAddress, UserAgent, CreatedAt
FROM Subchron.dbo.AuditLogs
WHERE (OrgID IS NULL OR Action LIKE 'Login%' OR Action = 'Logout' OR Action LIKE 'LoginFailed%' OR EntityName = 'User' OR EntityName = 'Subscription' OR EntityName = 'Organizations');

-- Re-enable constraints
ALTER TABLE SubchronTenant.dbo.LeaveRequests WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE SubchronTenant.dbo.ShiftAssignments WITH CHECK CHECK CONSTRAINT ALL;
GO
```

**If you use different server/database names:** replace `Subchron` with your Platform DB name and `SubchronTenant` with your Tenant DB name. If databases are on different servers, use linked server or ETL/SSIS.

### 6.2 Verify counts

```sql
-- In Platform DB (Subchron)
SELECT 'Employees' AS T, COUNT(*) FROM Subchron.dbo.Employees
UNION ALL SELECT 'Departments', COUNT(*) FROM Subchron.dbo.Departments
UNION ALL SELECT 'LeaveRequests', COUNT(*) FROM Subchron.dbo.LeaveRequests
UNION ALL SELECT 'ShiftAssignments', COUNT(*) FROM Subchron.dbo.ShiftAssignments;

-- In Tenant DB (SubchronTenant)
SELECT 'Employees' AS T, COUNT(*) FROM SubchronTenant.dbo.Employees
UNION ALL SELECT 'Departments', COUNT(*) FROM SubchronTenant.dbo.Departments
UNION ALL SELECT 'LeaveRequests', COUNT(*) FROM SubchronTenant.dbo.LeaveRequests
UNION ALL SELECT 'ShiftAssignments', COUNT(*) FROM SubchronTenant.dbo.ShiftAssignments;
```

Counts for tenant tables should match between the two databases before proceeding.

### 6.3 Drop tenant tables from Platform DB (only after verification)

Create a **new** migration for SubchronDbContext that:

- Removes DbSets and configurations for: Employee, Department, LeaveRequest, ShiftAssignment, AuditLog.
- Leaves: Users, Organizations, OrganizationSettings, Plans, Subscriptions, BillingRecords, PaymentTransactions, AuthLoginSessions, PasswordResetTokens, EmailVerificationCodes, SignupPendings, OrganizationPaymentMethods, SuperAdminAuditLogs.

**Manual approach (recommended for drop):** Add a migration that contains only the drop operations:

```powershell
Add-Migration RemoveTenantTablesFromPlatform -Context SubchronDbContext -Project Subchron.API -StartupProject Subchron.API
```

Then edit the generated migration file to:

- Drop FK constraints that reference tenant tables.
- Drop tables: `AuditLogs`, `ShiftAssignments`, `LeaveRequests`, `Departments`, `Employees`.

Alternatively, run raw SQL in a migration:

```csharp
migrationBuilder.Sql(@"
    -- Drop FKs and tables in dependency order
    IF OBJECT_ID('FK_ShiftAssignments_Employees_EmpID', 'F') IS NOT NULL ALTER TABLE ShiftAssignments DROP CONSTRAINT FK_ShiftAssignments_Employees_EmpID;
    IF OBJECT_ID('FK_ShiftAssignments_Organizations_OrgID', 'F') IS NOT NULL ALTER TABLE ShiftAssignments DROP CONSTRAINT FK_ShiftAssignments_Organizations_OrgID;
    DROP TABLE IF EXISTS ShiftAssignments;
    -- ... similar for LeaveRequests, Departments, Employees, AuditLogs
");
```

After this migration, Platform DB has no tenant operational tables; Tenant DB has only tenant tables. Each DB has its own `__EFMigrationsHistory`.

---

## Summary of commands (quick reference)

| Step | Context | Command |
|------|---------|--------|
| Add Tenant migration | TenantDbContext | `Add-Migration InitialTenant -Context TenantDbContext -OutputDir MigrationsTenant` |
| Update Tenant DB | TenantDbContext | `Update-Database -Context TenantDbContext` |
| Add Platform migration (new tables) | SubchronDbContext | `Add-Migration AddSuperAdminAuditLogAndPlatformTables -Context SubchronDbContext` |
| Update Platform DB | SubchronDbContext | `Update-Database -Context SubchronDbContext` |
| After data copy: drop tenant tables | SubchronDbContext | Add migration that drops Employees, Departments, LeaveRequests, ShiftAssignments, AuditLogs |

---

## Errors to avoid (all phases)

- Do not add cross-DB foreign keys or EF navigation properties between Platform and Tenant.
- Do not run the “drop tenant tables” migration before copying and verifying data.
- Ensure both connection strings are set before running any migration or startup.
- SuperAdmin must never read TenantAuditLogs (enforced in AuditLogsController with Forbid).
- Tenant audit list must always filter by OrgID from claims.
