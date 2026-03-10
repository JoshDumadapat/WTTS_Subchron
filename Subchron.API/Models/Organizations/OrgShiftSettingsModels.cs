using System.Text.Json.Serialization;

namespace Subchron.API.Models.Organizations;

public class OrgShiftSettingsResponse
{
    public List<OrgShiftTemplateDto> Templates { get; set; } = new();
    public OrgOvertimeSettingsDto Overtime { get; set; } = new();
    public OrgNightDifferentialSettingsDto NightDifferential { get; set; } = new();
}

public class OrgShiftSettingsUpdateRequest
{
    public List<OrgShiftTemplateDto>? Templates { get; set; }
    public OrgOvertimeSettingsDto? Overtime { get; set; }
    public OrgNightDifferentialSettingsDto? NightDifferential { get; set; }
}

public class OrgShiftTemplateDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Fixed"; // Fixed, Flexible, Open
    public List<string> WorkDays { get; set; } = new(); // Mon..Sun
    public OrgShiftFixedSettings? Fixed { get; set; }
    public OrgShiftFlexibleSettings? Flexible { get; set; }
    public OrgShiftOpenSettings? Open { get; set; }
    public List<OrgShiftBreakDto> Breaks { get; set; } = new();
    public List<OrgShiftDayOverrideDto> DayOverrides { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string? DisabledReason { get; set; }
}

public class OrgShiftBreakDto
{
    public string Name { get; set; } = string.Empty;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public bool IsPaid { get; set; }
}

public class OrgShiftDayOverrideDto
{
    public string Day { get; set; } = string.Empty;
    public bool IsOffDay { get; set; }
    public List<OrgShiftWindowDto> WorkWindows { get; set; } = new();
}

public class OrgShiftWindowDto
{
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
}

public class OrgShiftFixedSettings
{
    public string? StartTime { get; set; } // HH:mm
    public string? EndTime { get; set; }   // HH:mm
    public int BreakMinutes { get; set; }
    public int GraceMinutes { get; set; }
}

public class OrgShiftFlexibleSettings
{
    public string? EarliestStart { get; set; } // HH:mm
    public string? LatestEnd { get; set; }     // HH:mm
    public decimal RequiredDailyHours { get; set; }
    public decimal MaxDailyHours { get; set; }
}

public class OrgShiftOpenSettings
{
    public decimal RequiredWeeklyHours { get; set; }
}

public class OrgOvertimeSettingsDto
{
    public bool Enabled { get; set; }
    public decimal MinHoursBeforeOvertime { get; set; }
    public string Basis { get; set; } = "AfterShiftEnd"; // AfterShiftEnd, AfterTotalHoursDay, AfterTotalHoursWeek
    public decimal? WeeklyThresholdHours { get; set; }
    public bool PreApprovalRequired { get; set; }
    public string ApproverRole { get; set; } = "Supervisor";
    public bool AutoApprove { get; set; }
    public int RoundToMinutes { get; set; }
    public int MinimumBlockMinutes { get; set; }
    public decimal? MaxHoursPerDay { get; set; }
    public decimal? MaxHoursPerWeek { get; set; }
    public bool HardStopEnabled { get; set; }
    public string LimitMode { get; set; } = "SOFT"; // SOFT or HARD
    public string? OverrideRole { get; set; }
    public OrgOvertimeDayTypeRules DayTypes { get; set; } = new();
    public List<OrgOvertimeBucketRuleDto> BucketRules { get; set; } = new();
    public List<OrgOvertimeScopeRuleDto> ScopeRules { get; set; } = new();
    public List<OrgOvertimeApprovalStepDto> ApprovalSteps { get; set; } = new();
}

public class OrgOvertimeDayTypeRules
{
    public string RegularDayBehavior { get; set; } = "Default";
    public string RestDayBehavior { get; set; } = "SeparateBucket";
    public string HolidayBehavior { get; set; } = "SeparateBucket";
}

public class OrgOvertimeBucketRuleDto
{
    public string BucketCode { get; set; } = "RegularDay";
    public bool Enabled { get; set; } = true;
    public decimal? ThresholdHours { get; set; }
    public decimal? MaxHours { get; set; }
    public int MinimumBlockMinutes { get; set; }
}

public class OrgOvertimeScopeRuleDto
{
    public string ScopeType { get; set; } = "Department";
    public bool Include { get; set; } = true;
    public List<string> Values { get; set; } = new();
}

public class OrgOvertimeApprovalStepDto
{
    public int Order { get; set; }
    public string Role { get; set; } = "Supervisor";
    public bool Required { get; set; } = true;
}

public class OrgNightDifferentialSettingsDto
{
    public bool Enabled { get; set; } = true;
    public string StartTime { get; set; } = "22:00";
    public string EndTime { get; set; } = "06:00";
    public int MinimumMinutes { get; set; }
    public List<string> ExcludedAttendanceBuckets { get; set; } = new();
}
