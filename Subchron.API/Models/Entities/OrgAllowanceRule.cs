using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

public class OrgAllowanceRule
{
    public int OrgAllowanceRuleID { get; set; }
    public int OrgID { get; set; }
    public Organization Organization { get; set; } = null!;

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string AllowanceType { get; set; } = "FixedPerPayroll";

    [MaxLength(40)]
    public string Category { get; set; } = "DeMinimis";

    public decimal Amount { get; set; }

    public bool IsTaxable { get; set; }
    public bool AttendanceDependent { get; set; }
    public bool ProrateIfPartialPeriod { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Comma-style tags stored as JSON array for filtering (sites, programs, LGUs, etc.).</summary>
    public string ScopeTagsJson { get; set; } = "[]";

    [MaxLength(400)]
    public string ComplianceNotes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
