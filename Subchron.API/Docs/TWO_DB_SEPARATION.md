# Two-Database Physical Separation — Implementation Summary

## Properties and Fluent Configs Removed/Changed (No Cross-DB Navs)

### Tenant entities (Tenant DB) — REMOVE these navs and FK configs

| Entity | Remove property | Remove fluent config |
|--------|------------------|----------------------|
| **Employee** | `Organization` | `.HasOne(x => x.Organization)` / `.WithMany()` |
| **Employee** | `User` | `.HasOne(x => x.User)` / `.WithMany()` |
| **Department** | `Organization` | `.HasOne(x => x.Organization)` / `.WithMany()` |
| **LeaveRequest** | `Organization` | `.HasOne(x => x.Organization)` |
| **LeaveRequest** | `ReviewedByUser` | `.HasOne(x => x.ReviewedByUser)` |
| **ShiftAssignment** | `Organization` | `.HasOne(x => x.Organization)` |

**Keep:** Scalar `OrgID` (int), `UserID` (int?) / `ReviewedByUserID` (int?) on entities. **Keep** `Employee` nav and FK on LeaveRequest/ShiftAssignment (same DB).

### Platform entity (SubchronDbContext)

| Entity | Change |
|--------|--------|
| **Organization** | Remove `Departments` collection (Departments live in Tenant DB). |

### Audit

- **AuditLog** (old): Removed from Platform after data migration. Replaced by **SuperAdminAuditLog** (Platform) and **TenantAuditLog** (Tenant).
- **SuperAdminAuditLog**: No Organization/User navs; only scalar OrgID (int?), UserID (int?).
- **TenantAuditLog**: OrgID required (int), UserID optional (int?); no navs.

---

## Phase 0: TenantDbContext + Connection Strings + DI

**Goal:** Register a second DbContext (TenantDbContext) using TenantConnection; both connections in config.

**Success checks:** App starts; `TenantDbContext` can be resolved; migrations can be added for TenantDbContext with output dir `MigrationsTenant`.

---

## Phase 1: Tenant entities (no cross-DB navs) + TenantAuditLog

**Goal:** Tenant-side entity classes have only scalar FKs; new TenantAuditLog entity.

---

## Phase 2: TenantDbContext configuration + first migration

**Goal:** Tenant DB schema created in second database via EF migration.

---

## Phase 3: IAuditService split + SuperAdminAuditLog + claims helpers

**Goal:** LogTenantAsync → TenantAuditLogs; LogSuperAdminAsync → SuperAdminAuditLogs; shared claims helpers.

---

## Phase 4: SubchronDbContext trimmed to platform-only

**Goal:** Platform DbContext has no tenant tables; has SuperAdminAuditLog; SignupPending, EmailVerificationCode, OrganizationPaymentMethod added if needed. **Do not** drop tenant tables in Platform until after data migration (Phase 6).

---

## Phase 5: Controllers use TenantDbContext + audit split + security

**Goal:** Tenant controllers use TenantDbContext; audit calls use correct service; TenantAuditLogs endpoint forbids SuperAdmin; OrgID filter on tenant audit.

---

## Phase 6: Data migration scripts + final Platform migration

**Goal:** Copy tenant data to Tenant DB; verify; then run Platform migration that drops tenant tables and adds SuperAdminAuditLogs.
