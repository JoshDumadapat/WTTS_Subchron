using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

/// <summary>Tenant-scoped audit log (Tenant DB). OrgID required; UserID soft reference to Platform Users. No nav properties.</summary>
public class TenantAuditLog
{
    public int TenantAuditLogID { get; set; }

    public int OrgID { get; set; }
    public int? UserID { get; set; }

    [Required, MaxLength(80)]
    public string Action { get; set; } = null!;

    [MaxLength(60)]
    public string? EntityName { get; set; }

    public int? EntityID { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }

    [MaxLength(1000)]
    public string? Meta { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(200)]
    public string? UserAgent { get; set; }
}
