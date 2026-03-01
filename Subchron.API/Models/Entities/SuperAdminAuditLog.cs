using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

/// <summary>Platform/superadmin-only audit (Platform DB). Tenant admin login/logout, OrgSettings changes; no tenant operational logs.</summary>
public class SuperAdminAuditLog
{
    public int SuperAdminAuditLogID { get; set; }

    public int? OrgID { get; set; }
    public int? UserID { get; set; }

    [Required, MaxLength(80)]
    public string Action { get; set; } = null!;

    [MaxLength(60)]
    public string? EntityName { get; set; }

    public int? EntityID { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(200)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
