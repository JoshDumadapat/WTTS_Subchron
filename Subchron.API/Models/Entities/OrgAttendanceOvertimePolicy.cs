namespace Subchron.API.Models.Entities;

public class OrgAttendanceOvertimePolicy
{
    public int OrgAttendanceOvertimePolicyID { get; set; }
    public int OrgID { get; set; }

    public bool Enabled { get; set; }
    public string Basis { get; set; } = "SHIFT_END";
    public bool RestHolidayOverride { get; set; }
    public decimal? DailyThresholdHours { get; set; }
    public decimal? WeeklyThresholdHours { get; set; }
    public bool EarlyOtAllowed { get; set; }
    public int MicroOtBufferMinutes { get; set; }
    public bool RequireHoursMet { get; set; }
    public string FilingMode { get; set; } = "AUTO";
    public bool PreApprovalRequired { get; set; }
    public bool AllowPostFiling { get; set; } = true;
    public string ApprovalFlowType { get; set; } = "SINGLE";
    public bool AutoApprove { get; set; }
    public int RoundingMinutes { get; set; } = 15;
    public string RoundingDirection { get; set; } = "NEAREST";
    public int MinimumBlockMinutes { get; set; } = 30;
    public decimal? MaxPerDayHours { get; set; }
    public decimal? MaxPerWeekHours { get; set; }
    public string LimitMode { get; set; } = "SOFT";
    public string? OverrideRole { get; set; }
    public string ScopeMode { get; set; } = "ALL";

    public bool NightDiffEnabled { get; set; } = true;
    public string NightDiffWindowStart { get; set; } = "22:00";
    public string NightDiffWindowEnd { get; set; } = "06:00";
    public int NightDiffMinimumMinutes { get; set; } = 30;
    public bool NightDiffExcludeBreaks { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrgAttendanceOvertimeBucket> Buckets { get; set; } = new List<OrgAttendanceOvertimeBucket>();
    public ICollection<OrgAttendanceOvertimeApprovalStep> ApprovalSteps { get; set; } = new List<OrgAttendanceOvertimeApprovalStep>();
    public ICollection<OrgAttendanceOvertimeScopeFilter> ScopeFilters { get; set; } = new List<OrgAttendanceOvertimeScopeFilter>();
    public ICollection<OrgAttendanceNightDiffExclusion> NightDiffExclusions { get; set; } = new List<OrgAttendanceNightDiffExclusion>();
}

public class OrgAttendanceOvertimeBucket
{
    public int OrgAttendanceOvertimeBucketID { get; set; }
    public int OrgAttendanceOvertimePolicyID { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public decimal? ThresholdHours { get; set; }
    public decimal? MaxHours { get; set; }
    public int? MinimumBlockMinutes { get; set; }
}

public class OrgAttendanceOvertimeApprovalStep
{
    public int OrgAttendanceOvertimeApprovalStepID { get; set; }
    public int OrgAttendanceOvertimePolicyID { get; set; }
    public int Order { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
}

public class OrgAttendanceOvertimeScopeFilter
{
    public int OrgAttendanceOvertimeScopeFilterID { get; set; }
    public int OrgAttendanceOvertimePolicyID { get; set; }
    public string FilterType { get; set; } = string.Empty; // Department | Site | EmploymentType | Role
    public string Value { get; set; } = string.Empty;
}

public class OrgAttendanceNightDiffExclusion
{
    public int OrgAttendanceNightDiffExclusionID { get; set; }
    public int OrgAttendanceOvertimePolicyID { get; set; }
    public string? Department { get; set; }
    public string? Site { get; set; }
    public string? Role { get; set; }
}
