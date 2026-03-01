# EF Core Migration Commands — Package Manager Console

## Prerequisites

1. **Default project:** Set to `Subchron.API` in Package Manager Console.
2. **Connection strings** in `appsettings.json` (or User Secrets):
   - `DefaultConnection` → Platform DB (e.g. db42932)
   - `TenantConnection` → Tenant DB (e.g. db42933)

---

## Update both databases (cloud or local)

Run these in order:

### 1. Platform DB (SubchronDbContext → DefaultConnection)

```powershell
Update-Database -Context SubchronDbContext -Project Subchron.API -StartupProject Subchron.API
```

### 2. Tenant DB (TenantDbContext → TenantConnection)

```powershell
Update-Database -Context TenantDbContext -Project Subchron.API -StartupProject Subchron.API
```

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
