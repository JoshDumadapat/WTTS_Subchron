using Subchron.API.Data;
using Subchron.API.Models.Entities;

namespace Subchron.API.Services;

public class AuditService : IAuditService
{
    private const int MaxDetailsLength = 500;
    private readonly SubchronDbContext _db;

    public AuditService(SubchronDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int? orgId, int? userId, string action, string? entityName, int? entityId, string? details, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        var truncated = details == null ? null : details.Length <= MaxDetailsLength ? details : details[..MaxDetailsLength];
        var log = new AuditLog
        {
            OrgID = orgId,
            UserID = userId,
            Action = action.Length > 80 ? action[..80] : action,
            EntityName = entityName == null ? null : (entityName.Length > 60 ? entityName[..60] : entityName),
            EntityID = entityId,
            Details = truncated,
            IpAddress = ipAddress != null && ipAddress.Length > 64 ? ipAddress[..64] : ipAddress,
            UserAgent = userAgent != null && userAgent.Length > 200 ? userAgent[..200] : userAgent,
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
