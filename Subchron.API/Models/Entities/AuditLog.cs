using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

public class AuditLog
{
    public int AuditID { get; set; }

    public int? OrgID { get; set; }
    public Organization? Organization { get; set; }

    public int? UserID { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(60)]
    public string Action { get; set; } = null!;

    [MaxLength(60)]
    public string? EntityName { get; set; } 

    public int? EntityID { get; set; } 

    [MaxLength(500)]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(200)]
    public string? UserAgent { get; set; }
}