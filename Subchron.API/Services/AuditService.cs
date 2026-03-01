using Subchron.API.Data;
using Subchron.API.Models.Entities;

namespace Subchron.API.Services;

public class AuditService : IAuditService
{
    private const int MaxDetailsLength = 500;
    private const int MaxMetaLength = 1000;
    private const int MaxActionLength = 80;
    private const int MaxEntityNameLength = 60;
    private const int MaxIpLength = 64;
    private const int MaxUserAgentLength = 200;

    private readonly SubchronDbContext _platformDb;
    private readonly TenantDbContext _tenantDb;

    public AuditService(SubchronDbContext platformDb, TenantDbContext tenantDb)
    {
        _platformDb = platformDb;
        _tenantDb = tenantDb;
    }

    public async Task LogTenantAsync(int orgId, int? userId, string action, string? entityName, int? entityId, string? details, string? ipAddress = null, string? userAgent = null, string? meta = null, CancellationToken ct = default)
    {
        var log = new TenantAuditLog
        {
            OrgID = orgId,
            UserID = userId,
            Action = Truncate(action, MaxActionLength),
            EntityName = Truncate(entityName, MaxEntityNameLength),
            EntityID = entityId,
            Details = Truncate(details, MaxDetailsLength),
            Meta = Truncate(meta, MaxMetaLength),
            IpAddress = Truncate(ipAddress, MaxIpLength),
            UserAgent = Truncate(userAgent, MaxUserAgentLength),
            CreatedAt = DateTime.UtcNow
        };
        _tenantDb.TenantAuditLogs.Add(log);
        await _tenantDb.SaveChangesAsync(ct);
    }

    public async Task LogSuperAdminAsync(int? orgId, int? userId, string action, string? entityName, int? entityId, string? details, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        var log = new SuperAdminAuditLog
        {
            OrgID = orgId,
            UserID = userId,
            Action = Truncate(action, MaxActionLength),
            EntityName = Truncate(entityName, MaxEntityNameLength),
            EntityID = entityId,
            Details = Truncate(details, MaxDetailsLength),
            IpAddress = Truncate(ipAddress, MaxIpLength),
            UserAgent = Truncate(userAgent, MaxUserAgentLength),
            CreatedAt = DateTime.UtcNow
        };
        _platformDb.SuperAdminAuditLogs.Add(log);
        await _platformDb.SaveChangesAsync(ct);
    }

    private static string? Truncate(string? value, int maxLen)
    {
        if (value == null) return null;
        return value.Length <= maxLen ? value : value[..maxLen];
    }
}
