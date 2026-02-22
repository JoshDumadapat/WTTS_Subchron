# Role-Based Access Control (RBAC) – File Traceability

All role-based access logic is centralized so you can trace and update permissions in one place.

## API (Subchron.API)

| File | Purpose |
|------|--------|
| **Authorization/AppModule.cs** | Module name constants (Dashboard, EmployeeManagement, DepartmentManagement, Archive, Payroll, Reports, Operations, OrganizationSettings, AuditLog, UsersAndRoles, SystemSettings, LeaveManagement, ShiftSchedule). |
| **Authorization/RoleModuleAccess.cs** | Defines which role can access which module; `CanAccessModule(ClaimsPrincipal, module)` and `GetModulesForRole(UserRoleType)`. |
| **Models/Entities/User.cs** | `UserRoleType` enum: SuperAdmin, OrgAdmin, HR, Manager, Employee, Payroll, Supervisor. |
| **Data/SubchronDbContext.cs** | User role check constraint `[Role] IN (1,2,3,4,5,6,7)`. |

## Web (Subchron.Web)

| File | Purpose |
|------|--------|
| **Rbac/AppModule.cs** | Same module name constants as API (keep in sync). |
| **Rbac/RoleModuleAccess.cs** | `CanAccessModule(ClaimsPrincipal, module)` for nav and page access; role→module rules (must match API). |
| **Program.cs** | Authorization policies (Backoffice, SupervisorOnly, PayrollOnly, etc.) and post-login redirect by role. |
| **Pages/App/Shared/_LayoutAdmin.cshtml** | Sidebar: each nav item wrapped in `RoleModuleAccess.CanAccessModule(User, AppModule.X)`. |

## Role → Module Mapping (reference)

- **Administrator (OrgAdmin)** / **SuperAdmin**: All modules.
- **HR**: Dashboard, Employee Management, Department Management, Archive, Leave Management.
- **Payroll**: Dashboard, Payroll, Reports.
- **Manager**: Operations, Dashboard, Leave Management, Shift Schedule.
- **Supervisor**: Operations, Dashboard, Leave Management, Shift Schedule.

To change what a role can do, edit **API: Authorization/RoleModuleAccess.cs** and **Web: Rbac/RoleModuleAccess.cs** (keep both in sync).
