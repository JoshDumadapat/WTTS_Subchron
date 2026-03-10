using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Organizations;

public class OrgHolidayRequest
{
    [Required]
    public string Date { get; set; } = string.Empty; // yyyy-MM-dd

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Type { get; set; } = "RegularHoliday";

    [MaxLength(40)]
    public string Status { get; set; } = "Active";

    public bool? IsActive { get; set; }

    [MaxLength(40)]
    public string ScopeType { get; set; } = "Nationwide";

    public List<string>? ScopeValues { get; set; } = new();

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
        = string.Empty;

    public List<string>? EmployeeGroupScope { get; set; } = new();

    public bool IncludePayroll { get; set; } = true;
    public bool UsePayRules { get; set; } = true;
    public bool PaidWhenUnworked { get; set; } = true;

    [MaxLength(60)]
    public string PayrollClassification { get; set; } = "RegularHoliday";

    [MaxLength(60)]
    public string? RestDayPayrollClassification { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? PayrollRuleId { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? RestDayPayrollRuleId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ReferenceNo { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ReferenceUrl { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? OfficialTag { get; set; } = string.Empty;

    public string? PayrollNotes { get; set; }
        = string.Empty;

    public string? Notes { get; set; } = string.Empty;
}

public class OrgHolidayResponse
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string ScopeType { get; set; } = string.Empty;
    public List<string> ScopeValues { get; set; } = new();
    public string SourceTag { get; set; } = string.Empty;
    public string OverlapStrategy { get; set; } = string.Empty;
    public int Precedence { get; set; }
        = 100;
    public bool IncludeAttendance { get; set; }
        = true;
    public bool NonWorkingDay { get; set; }
        = true;
    public bool AllowWork { get; set; }
        = true;
    public bool ApplyRestDayRules { get; set; }
        = true;
    public string AttendanceClassification { get; set; } = string.Empty;
    public string? RestDayAttendanceClassification { get; set; }
        = string.Empty;
    public List<string> EmployeeGroupScope { get; set; } = new();
    public bool IncludePayroll { get; set; }
        = true;
    public bool UsePayRules { get; set; }
        = true;
    public bool PaidWhenUnworked { get; set; }
        = true;
    public string PayrollClassification { get; set; } = string.Empty;
    public string? RestDayPayrollClassification { get; set; }
        = string.Empty;
    public string? PayrollRuleId { get; set; } = string.Empty;
    public string? RestDayPayrollRuleId { get; set; }
        = string.Empty;
    public string? ReferenceNo { get; set; } = string.Empty;
    public string? ReferenceUrl { get; set; } = string.Empty;
    public string? OfficialTag { get; set; } = string.Empty;
    public string PayrollNotes { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool HasRuleConfigured { get; set; } = false;
    public bool IsSynced { get; set; }
        = false;
    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
        = DateTime.UtcNow;
}
