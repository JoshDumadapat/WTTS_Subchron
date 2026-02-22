namespace Subchron.API.Services;

/// <summary>Records audit events (login, logout, department/employee changes, etc.). Does not store sensitive data (passwords, tokens).</summary>
public interface IAuditService
{
    /// <summary>Log an audit entry. Details truncated to 500 chars. Optional ipAddress and userAgent for traceability.</summary>
    Task LogAsync(int? orgId, int? userId, string action, string? entityName, int? entityId, string? details, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
}
