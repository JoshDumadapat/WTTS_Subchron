using System.Security.Claims;

namespace Subchron.Web.Rbac;

/// <summary>
/// RBAC: defines which roles can access which modules. Single source of truth for Web UI (nav + page access).
/// Traceability: all role-permission rules for the app area live here; keep in sync with API Authorization/RoleModuleAccess.
/// </summary>
public static class RoleModuleAccess
{
    /// <summary>Returns true if the current user (from claims) can access the given module.</summary>
    public static bool CanAccessModule(ClaimsPrincipal? user, string module)
    {
        if (user == null || string.IsNullOrEmpty(module)) return false;
        var roleStr = (user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value ?? "").Trim();
        if (string.IsNullOrEmpty(roleStr)) return false;

        // SuperAdmin and OrgAdmin (Administrator): full access to all app modules. "Admin" is display/short form of OrgAdmin.
        if (string.Equals(roleStr, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(roleStr, "OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(roleStr, "Admin", StringComparison.OrdinalIgnoreCase))
            return true;

        return roleStr.ToUpperInvariant() switch
        {
            "HR" => IsHRModule(module),
            "PAYROLL" => IsPayrollModule(module),
            "MANAGER" => IsManagerModule(module),
            "SUPERVISOR" => IsSupervisorModule(module),
            _ => false
        };
    }

    private static bool IsHRModule(string module) =>
        module == AppModule.Dashboard || module == AppModule.EmployeeManagement ||
        module == AppModule.DepartmentManagement || module == AppModule.Archive;

    private static bool IsPayrollModule(string module) =>
        module == AppModule.Dashboard || module == AppModule.Payroll || module == AppModule.Reports;

    private static bool IsManagerModule(string module) =>
        module == AppModule.Operations || module == AppModule.Dashboard ||
        module == AppModule.LeaveManagement || module == AppModule.ShiftSchedule;

    private static bool IsSupervisorModule(string module) =>
        module == AppModule.Operations || module == AppModule.Dashboard ||
        module == AppModule.LeaveManagement || module == AppModule.ShiftSchedule;

    /// <summary>True if the user has access to the backoffice (/App). Used to show "Back to Admin" in Employee portal; hide for Employee-only role.</summary>
    public static bool CanAccessBackoffice(ClaimsPrincipal? user)
    {
        if (user == null) return false;
        var roleStr = (user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value ?? "").Trim();
        return string.Equals(roleStr, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleStr, "OrgAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleStr, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleStr, "HR", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleStr, "Manager", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleStr, "Supervisor", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleStr, "Payroll", StringComparison.OrdinalIgnoreCase);
    }
}
