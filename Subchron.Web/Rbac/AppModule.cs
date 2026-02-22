namespace Subchron.Web.Rbac;

/// <summary>
/// Application module identifiers for RBAC (must match Subchron.API.Authorization.AppModule).
/// Traceability: single file defining every module that can be permission-guarded in the Web app.
/// </summary>
public static class AppModule
{
    public const string Dashboard = "Dashboard";
    public const string EmployeeManagement = "EmployeeManagement";
    public const string DepartmentManagement = "DepartmentManagement";
    public const string Archive = "Archive";
    public const string Payroll = "Payroll";
    public const string Reports = "Reports";
    public const string Operations = "Operations";
    public const string OrganizationSettings = "OrganizationSettings";
    public const string AuditLog = "AuditLog";
    public const string UsersAndRoles = "UsersAndRoles";
    public const string SystemSettings = "SystemSettings";
    public const string LeaveManagement = "LeaveManagement";
    public const string ShiftSchedule = "ShiftSchedule";
}
