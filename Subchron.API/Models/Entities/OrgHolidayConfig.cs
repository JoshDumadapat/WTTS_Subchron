using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

public class OrgHolidayConfig
{
    public int OrgHolidayConfigID { get; set; }

    [Required]
    public int OrgID { get; set; }

    [Required]
    public DateTime HolidayDate { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Type { get; set; } = "RegularHoliday";

    [MaxLength(40)]
    public string Status { get; set; } = "Active";

    [MaxLength(40)]
    public string ScopeType { get; set; } = "Nationwide";

    [MaxLength(40)]
    public string SourceTag { get; set; } = "ManualEntry";

    [MaxLength(40)]
    public string OverlapStrategy { get; set; } = "HighestPrecedence";

    public int Precedence { get; set; } = 100;

    public bool IncludeAttendance { get; set; } = true;
    public bool NonWorkingDay { get; set; } = true;
    public bool AllowWork { get; set; } = true;
    public bool ApplyRestDayRules { get; set; } = true;

    [MaxLength(60)]
    public string AttendanceClassification { get; set; } = "Holiday";

    [MaxLength(60)]
    public string? RestDayAttendanceClassification { get; set; }
        = "";

    public bool IncludePayroll { get; set; } = true;
    public bool UsePayRules { get; set; } = true;
    public bool PaidWhenUnworked { get; set; } = true;

    [MaxLength(60)]
    public string PayrollClassification { get; set; } = "RegularHoliday";

    [MaxLength(60)]
    public string? RestDayPayrollClassification { get; set; }
        = "";

    [MaxLength(80)]
    public string? PayrollRuleId { get; set; }
        = string.Empty;

    [MaxLength(80)]
    public string? RestDayPayrollRuleId { get; set; }
        = string.Empty;

    [MaxLength(100)]
    public string? ReferenceNo { get; set; }
        = string.Empty;

    [MaxLength(200)]
    public string? ReferenceUrl { get; set; }
        = string.Empty;

    [MaxLength(80)]
    public string? OfficialTag { get; set; }
        = string.Empty;

    public string ScopeValuesJson { get; set; } = "[]";
    public string EmployeeGroupScopeJson { get; set; } = "[]";

    public string PayrollNotes { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public bool IsSynced { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
