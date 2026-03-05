using System.Linq;
using System.Text.Json.Serialization;

namespace Subchron.API.Models.Organizations;

public class OrgAttendanceOvertimeDto
{
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
    public List<OrgAttendanceOvertimeApprovalStepDto> ApprovalSteps { get; set; } = new();
    public bool AutoApprove { get; set; }
    public int RoundingMinutes { get; set; } = 15;
    public string RoundingDirection { get; set; } = "NEAREST";
    public int MinimumBlockMinutes { get; set; } = 30;
    public decimal? MaxPerDayHours { get; set; }
    public decimal? MaxPerWeekHours { get; set; }
    public string LimitMode { get; set; } = "SOFT";
    public string? OverrideRole { get; set; }
    public List<OrgAttendanceOvertimeBucketDto> Buckets { get; set; } = new();
    public string ScopeMode { get; set; } = "ALL";
    public OrgAttendanceOvertimeScopeFiltersDto ScopeFilters { get; set; } = new();
    public OrgAttendanceNightDifferentialDto NightDifferential { get; set; } = OrgAttendanceOvertimeDefaults.BuildNightDifferential();
}

public class OrgAttendanceOvertimeBucketDto
{
    public string Key { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public decimal? ThresholdHours { get; set; }
    public decimal? MaxHours { get; set; }
    public int? MinimumBlockMinutes { get; set; }
}

public class OrgAttendanceOvertimeApprovalStepDto
{
    [JsonPropertyName("order")]
    public int Order { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
}

public class OrgAttendanceOvertimeScopeFiltersDto
{
    public List<string> Departments { get; set; } = new();
    public List<string> Sites { get; set; } = new();
    public List<string> EmploymentTypes { get; set; } = new();
    public List<string> Roles { get; set; } = new();
}

public class OrgAttendanceNightDifferentialDto
{
    public bool Enabled { get; set; } = true;
    public string WindowStart { get; set; } = "22:00";
    public string WindowEnd { get; set; } = "06:00";
    public int MinimumMinutes { get; set; } = 30;
    public bool ExcludeBreaks { get; set; } = true;
    public List<string> ExcludeDepartments { get; set; } = new();
    public List<string> ExcludeSites { get; set; } = new();
    public List<string> ExcludeRoles { get; set; } = new();
    public List<OrgAttendanceNightDifferentialScopedExclusionDto> ScopedExclusions { get; set; } = new();
}

public class OrgAttendanceNightDifferentialScopedExclusionDto
{
    public string Department { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public static class OrgAttendanceOvertimeDefaults
{
    public static readonly string[] BucketCodes = new[]
    {
        "RegularDay",
        "RestDay",
        "SpecialWorkingHoliday",
        "SpecialNonWorkingHoliday",
        "RegularHoliday",
        "RestDaySpecialWorkingHoliday",
        "RestDaySpecialNonWorkingHoliday",
        "RestDayRegularHoliday"
    };

    public static OrgAttendanceOvertimeDto BuildSettings()
    {
        return new OrgAttendanceOvertimeDto
        {
            Enabled = false,
            Basis = "SHIFT_END",
            RestHolidayOverride = false,
            DailyThresholdHours = 8,
            WeeklyThresholdHours = null,
            EarlyOtAllowed = false,
            MicroOtBufferMinutes = 0,
            RequireHoursMet = false,
            FilingMode = "AUTO",
            PreApprovalRequired = false,
            AllowPostFiling = true,
            ApprovalFlowType = "SINGLE",
            ApprovalSteps = new List<OrgAttendanceOvertimeApprovalStepDto>
            {
                new() { Order = 1, Role = "SUPERVISOR", Required = true }
            },
            AutoApprove = false,
            RoundingMinutes = 15,
            RoundingDirection = "NEAREST",
            MinimumBlockMinutes = 30,
            MaxPerDayHours = null,
            MaxPerWeekHours = null,
            LimitMode = "SOFT",
            OverrideRole = string.Empty,
            Buckets = BucketCodes.Select(code => new OrgAttendanceOvertimeBucketDto
            {
                Key = code,
                Enabled = code == "RegularDay"
            }).ToList(),
            ScopeMode = "ALL",
            ScopeFilters = new OrgAttendanceOvertimeScopeFiltersDto(),
            NightDifferential = BuildNightDifferential()
        };
    }

    public static OrgAttendanceNightDifferentialDto BuildNightDifferential()
    {
        return new OrgAttendanceNightDifferentialDto
        {
            Enabled = true,
            WindowStart = "22:00",
            WindowEnd = "06:00",
            MinimumMinutes = 30,
            ExcludeBreaks = true,
            ExcludeDepartments = new List<string>(),
            ExcludeSites = new List<string>(),
            ExcludeRoles = new List<string>(),
            ScopedExclusions = new List<OrgAttendanceNightDifferentialScopedExclusionDto>()
        };
    }
}
