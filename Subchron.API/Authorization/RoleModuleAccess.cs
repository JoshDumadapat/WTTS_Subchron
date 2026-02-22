using System.Collections.Immutable;
using System.Security.Claims;
using Subchron.API.Models.Entities;

namespace Subchron.API.Authorization;

// RBAC: defines which roles can access which modules. Single source of truth for API authorization.

public static class RoleModuleAccess
{
    // Administrator (OrgAdmin): full access to all org modules
    private static readonly ImmutableHashSet<string> OrgAdminModules = ImmutableHashSet.Create(
        AppModule.Dashboard,
        AppModule.EmployeeManagement,
        AppModule.DepartmentManagement,
        AppModule.Archive,
        AppModule.Payroll,
        AppModule.Reports,
        AppModule.Operations,
        AppModule.OrganizationSettings,
        AppModule.AuditLog,
        AppModule.UsersAndRoles,
        AppModule.SystemSettings,
        AppModule.LeaveManagement,
        AppModule.ShiftSchedule
    );

    // HR: Dashboard, Employee Management, Department Management, Archive only (no Leave/Shift)
    private static readonly ImmutableHashSet<string> HRModules = ImmutableHashSet.Create(
        AppModule.Dashboard,
        AppModule.EmployeeManagement,
        AppModule.DepartmentManagement,
        AppModule.Archive
    );

    // Payroll Personnel: Dashboard, Payroll, Reports
    private static readonly ImmutableHashSet<string> PayrollModules = ImmutableHashSet.Create(
        AppModule.Dashboard,
        AppModule.Payroll,
        AppModule.Reports
    );

    // Manager: Operations, Dashboard, Leave Management, Shift/Schedule
    private static readonly ImmutableHashSet<string> ManagerModules = ImmutableHashSet.Create(
        AppModule.Operations,
        AppModule.Dashboard,
        AppModule.LeaveManagement,
        AppModule.ShiftSchedule
    );

    // Supervisor: Operations, Dashboard, Leave Management, Shift/Schedule
    private static readonly ImmutableHashSet<string> SupervisorModules = ImmutableHashSet.Create(
        AppModule.Operations,
        AppModule.Dashboard,
        AppModule.LeaveManagement,
        AppModule.ShiftSchedule
    );

    // Returns the set of module identifiers the given role can access.
    public static IReadOnlySet<string> GetModulesForRole(UserRoleType role)
    {
        return role switch
        {
            UserRoleType.SuperAdmin => OrgAdminModules, 
            UserRoleType.OrgAdmin => OrgAdminModules,
            UserRoleType.HR => HRModules,
            UserRoleType.Payroll => PayrollModules,
            UserRoleType.Manager => ManagerModules,
            UserRoleType.Supervisor => SupervisorModules,
            _ => ImmutableHashSet<string>.Empty
        };
    }

    // Returns true if the role can access the given module.
    public static bool CanAccessModule(UserRoleType role, string module)
    {
        if (string.IsNullOrEmpty(module)) return false;
        var set = GetModulesForRole(role);
        return set.Contains(module);
    }

    // Returns true if the current user can access the given module.
    public static bool CanAccessModule(ClaimsPrincipal? user, string module)
    {
        if (user == null || string.IsNullOrEmpty(module)) return false;
        var roleStr = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
        if (string.IsNullOrEmpty(roleStr)) return false;
        if (!Enum.TryParse<UserRoleType>(roleStr, ignoreCase: true, out var role)) return false;
        return CanAccessModule(role, module);
    }
}
