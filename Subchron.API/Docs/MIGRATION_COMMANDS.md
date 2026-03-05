# EF Core Migration Commands

## Prerequisites

1. **Connection strings** in `Subchron.API` (e.g. `appsettings.json`, `appsettings.Development.json`, or User Secrets):
   - `DefaultConnection` → Platform / Admin DB (SubchronDbContext)
   - `TenantConnection` → Tenant DB (TenantDbContext)

2. For **cloud**: point those connection strings to your cloud databases, then run the update commands below.

---

## Recommended: Update databases via Command Line (avoids PMC “Could not load assembly”)

From the **solution folder** (the folder that contains `Subchron.API` and `Subchron.Web`), open a terminal and run:

### 1. Platform / Admin DB (SubchronDbContext)

```bash
dotnet ef database update --context SubchronDbContext --project Subchron.API --startup-project Subchron.API
```

### 2. Tenant DB (TenantDbContext)

```bash
dotnet ef database update --context TenantDbContext --project Subchron.API --startup-project Subchron.API
```

Use the same connection strings as when you run the API (e.g. set cloud connection strings in User Secrets or appsettings before running).

---

## Alternative: Package Manager Console (Visual Studio)

If PMC gives “Could not load assembly 'Subchron.API'”, use the **dotnet ef** commands above instead.

1. **Default project:** Set to `Subchron.API` in PMC.
2. Run in order:

### 1. Platform DB

```powershell
Update-Database -Context SubchronDbContext -Project Subchron.API -StartupProject Subchron.API
```

### 2. Tenant DB

```powershell
Update-Database -Context TenantDbContext -Project Subchron.API -StartupProject Subchron.API
```

**Tip:** If you have “Multiple startup projects set”, set the solution to **Single startup project** = `Subchron.API` (Solution → right‑click → Properties → Startup Project), then try PMC again.

---

## Add new migrations (when you change the model)

### Platform (SubchronDbContext)

```powershell
Add-Migration YourMigrationName -Context SubchronDbContext -Project Subchron.API -StartupProject Subchron.API
Update-Database -Context SubchronDbContext -Project Subchron.API -StartupProject Subchron.API
```

### Tenant (TenantDbContext)

```powershell
Add-Migration YourTenantMigrationName -Context TenantDbContext -OutputDir MigrationsTenant -Project Subchron.API -StartupProject Subchron.API
Update-Database -Context TenantDbContext -Project Subchron.API -StartupProject Subchron.API
```

---

## Current migration layout

| Context           | Output folder      | Connection string   |
|------------------|--------------------|---------------------|
| SubchronDbContext | Migrations/        | DefaultConnection   |
| TenantDbContext   | MigrationsTenant/  | TenantConnection    |

Each database has its own `__EFMigrationsHistory` table.

---

## Table split (Platform vs Tenant)

**Platform DB (SubchronDbContext):** Organizations, OrganizationSettings, Plans, Subscriptions, Users, PasswordResetTokens, AuthLoginSessions, PaymentTransactions, BillingRecords, SuperAdminAuditLogs, EmailVerificationCodes, OrganizationPaymentMethods, DemoRequests.

**Tenant DB (TenantDbContext):** Employees, Departments, LeaveRequests, ShiftAssignments, TenantAuditLogs, Locations, AttendanceLogs, AttendanceCorrections, ExportJobs, LeaveTypes, OvertimeRequests, SignupPendings.

Tenant tables use soft refs (OrgID, UserID) to Platform data; no cross-DB FKs.
