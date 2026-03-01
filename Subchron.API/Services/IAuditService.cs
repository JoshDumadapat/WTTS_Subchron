namespace Subchron.API.Services;

/// <summary>Split audit: tenant operational events → Tenant DB (TenantAuditLogs); platform/superadmin events → Platform DB (SuperAdminAuditLogs).</summary>
public interface IAuditService
{
    /// <summary>Log to Tenant DB (TenantAuditLogs). OrgID required. Used for department/employee/leave/shift and other tenant-scoped actions.</summary>
    Task LogTenantAsync(int orgId, int? userId, string action, string? entityName, int? entityId, string? details, string? ipAddress = null, string? userAgent = null, string? meta = null, CancellationToken ct = default);

    /// <summary>Log to Platform DB (SuperAdminAuditLogs). Tenant admin login/logout, OrgSettings changes, etc. SuperAdmin must NOT read TenantAuditLogs.</summary>
    Task LogSuperAdminAsync(int? orgId, int? userId, string action, string? entityName, int? entityId, string? details, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
}
