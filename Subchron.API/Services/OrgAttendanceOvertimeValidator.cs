using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Subchron.API.Models.Organizations;

namespace Subchron.API.Services;

public static class OrgAttendanceOvertimeValidator
{
    private static readonly HashSet<string> AllowedBasis = new(StringComparer.OrdinalIgnoreCase)
    {
        "SHIFT_END",
        "DAILY_HOURS",
        "WEEKLY_HOURS",
        "LATER_OF_BOTH"
    };

    private static readonly HashSet<string> AllowedRoundingDirections = new(StringComparer.OrdinalIgnoreCase)
    {
        "NEAREST",
        "UP",
        "DOWN"
    };

    private static readonly HashSet<int> AllowedRoundingMinutes = new() { 1, 5, 10, 15, 30 };

    private static readonly HashSet<string> AllowedLimitModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SOFT",
        "HARD"
    };

    private static readonly HashSet<string> AllowedScopeModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ALL",
        "SELECTED"
    };

    private static readonly Regex TimeRegex = new("^(?:[01]\\d|2[0-3]):[0-5]\\d$", RegexOptions.Compiled);

    public static OrgAttendanceOvertimeDto Normalize(OrgAttendanceOvertimeDto? input)
    {
        var source = input ?? OrgAttendanceOvertimeDefaults.BuildSettings();
        var basis = NormalizeBasis(source.Basis);
        var needsDaily = basis is "DAILY_HOURS" or "LATER_OF_BOTH";
        var needsWeekly = basis is "WEEKLY_HOURS" or "LATER_OF_BOTH";

        var dailyThreshold = NormalizeNullableHours(source.DailyThresholdHours, 0, 24, "Daily threshold hours");
        var weeklyThreshold = NormalizeNullableHours(source.WeeklyThresholdHours, 0, 168, "Weekly threshold hours");

        if (needsDaily && dailyThreshold == null)
            throw new OrgAttendanceOvertimeValidationException("Enter the daily OT threshold in hours.");
        if (needsWeekly && weeklyThreshold == null)
            throw new OrgAttendanceOvertimeValidationException("Enter the weekly OT threshold in hours.");

        var roundingMinutes = source.RoundingMinutes;
        if (!AllowedRoundingMinutes.Contains(roundingMinutes))
            roundingMinutes = 15;

        var roundingDirection = AllowedRoundingDirections.Contains(source.RoundingDirection ?? string.Empty)
            ? source.RoundingDirection!.ToUpperInvariant()
            : "NEAREST";

        var limitMode = AllowedLimitModes.Contains(source.LimitMode ?? string.Empty)
            ? source.LimitMode!.ToUpperInvariant()
            : "SOFT";

        var scopeMode = AllowedScopeModes.Contains(source.ScopeMode ?? string.Empty)
            ? source.ScopeMode!.ToUpperInvariant()
            : "ALL";

        var normalizedBuckets = NormalizeBuckets(source.Buckets);
        var normalizedSteps = NormalizeApprovalSteps(source.ApprovalSteps);
        var scopeFilters = NormalizeScopeFilters(scopeMode, source.ScopeFilters);

        if (source.AutoApprove && source.PreApprovalRequired)
            throw new OrgAttendanceOvertimeValidationException("Disable Auto-Approve before requiring pre-approval.");

        return new OrgAttendanceOvertimeDto
        {
            Enabled = source.Enabled,
            Basis = basis,
            RestHolidayOverride = source.RestHolidayOverride,
            DailyThresholdHours = needsDaily ? dailyThreshold : null,
            WeeklyThresholdHours = needsWeekly ? weeklyThreshold : null,
            EarlyOtAllowed = source.EarlyOtAllowed,
            MicroOtBufferMinutes = ClampInt(source.MicroOtBufferMinutes, 0, 240),
            RequireHoursMet = source.RequireHoursMet,
            FilingMode = string.IsNullOrWhiteSpace(source.FilingMode) ? "AUTO" : source.FilingMode.Trim().ToUpperInvariant(),
            PreApprovalRequired = source.PreApprovalRequired && !source.AutoApprove,
            AllowPostFiling = source.AllowPostFiling,
            ApprovalFlowType = string.IsNullOrWhiteSpace(source.ApprovalFlowType) ? "SINGLE" : source.ApprovalFlowType.Trim().ToUpperInvariant(),
            ApprovalSteps = normalizedSteps,
            AutoApprove = source.AutoApprove,
            RoundingMinutes = roundingMinutes,
            RoundingDirection = roundingDirection,
            MinimumBlockMinutes = ClampInt(source.MinimumBlockMinutes, 0, 480),
            MaxPerDayHours = NormalizeNullableHours(source.MaxPerDayHours, 0, 24, "Max OT per day"),
            MaxPerWeekHours = NormalizeNullableHours(source.MaxPerWeekHours, 0, 168, "Max OT per week"),
            LimitMode = limitMode,
            OverrideRole = (limitMode == "SOFT") ? (source.OverrideRole ?? string.Empty).Trim() : string.Empty,
            Buckets = normalizedBuckets,
            ScopeMode = scopeMode,
            ScopeFilters = scopeFilters,
            NightDifferential = NormalizeNightDifferential(source.NightDifferential)
        };
    }

    private static OrgAttendanceNightDifferentialDto NormalizeNightDifferential(OrgAttendanceNightDifferentialDto? input)
    {
        var source = input ?? OrgAttendanceOvertimeDefaults.BuildNightDifferential();
        var start = NormalizeTime(source.WindowStart, "Night differential start time");
        var end = NormalizeTime(source.WindowEnd, "Night differential end time");
        if (!IsValidWindow(start, end))
            throw new OrgAttendanceOvertimeValidationException("Night differential window must have a non-zero duration.");

        return new OrgAttendanceNightDifferentialDto
        {
            Enabled = source.Enabled,
            WindowStart = start,
            WindowEnd = end,
            MinimumMinutes = ClampInt(source.MinimumMinutes, 0, 720),
            ExcludeBreaks = source.ExcludeBreaks,
            ExcludeDepartments = NormalizeStringList(source.ExcludeDepartments),
            ExcludeSites = NormalizeStringList(source.ExcludeSites),
            ExcludeRoles = NormalizeStringList(source.ExcludeRoles),
            ScopedExclusions = NormalizeNdScopedExclusions(source.ScopedExclusions)
        };
    }

    private static List<OrgAttendanceNightDifferentialScopedExclusionDto> NormalizeNdScopedExclusions(IEnumerable<OrgAttendanceNightDifferentialScopedExclusionDto>? combos)
    {
        var normalized = new List<OrgAttendanceNightDifferentialScopedExclusionDto>();
        if (combos == null)
            return normalized;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var combo in combos)
        {
            var department = NormalizeScopeValue(combo?.Department, "department");
            var site = NormalizeScopeValue(combo?.Site, "site");
            var role = NormalizeScopeValue(combo?.Role, "role");
            var key = $"{department}|{site}|{role}";
            if (seen.Contains(key))
                continue;
            seen.Add(key);
            normalized.Add(new OrgAttendanceNightDifferentialScopedExclusionDto
            {
                Department = department,
                Site = site,
                Role = role
            });
        }

        return normalized;
    }

    private static string NormalizeScopeValue(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new OrgAttendanceOvertimeValidationException($"Select a {label} for each night differential exclusion.");

        return value.Trim();
    }

    private static string NormalizeBasis(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "SHIFT_END" : value.Trim().ToUpperInvariant();
        return AllowedBasis.Contains(normalized) ? normalized : "SHIFT_END";
    }

    private static decimal? NormalizeNullableHours(decimal? value, decimal min, decimal max, string field)
    {
        if (value == null)
            return null;
        if (value < min || value > max)
            throw new OrgAttendanceOvertimeValidationException($"{field} must be between {min} and {max} hours.");
        return Math.Round(value.Value, 2, MidpointRounding.AwayFromZero);
    }

    private static int ClampInt(int? value, int min, int max)
    {
        var target = value ?? 0;
        if (target < min) return min;
        if (target > max) return max;
        return target;
    }

    private static List<OrgAttendanceOvertimeBucketDto> NormalizeBuckets(IEnumerable<OrgAttendanceOvertimeBucketDto>? buckets)
    {
        var normalized = new List<OrgAttendanceOvertimeBucketDto>();
        var source = buckets?.Where(b => b != null).ToList() ?? new List<OrgAttendanceOvertimeBucketDto>();
        foreach (var code in OrgAttendanceOvertimeDefaults.BucketCodes)
        {
            var match = source.FirstOrDefault(b => string.Equals(b.Key, code, StringComparison.OrdinalIgnoreCase));
            normalized.Add(new OrgAttendanceOvertimeBucketDto
            {
                Key = code,
                Enabled = match?.Enabled ?? (code == "RegularDay"),
                ThresholdHours = NormalizeNullableHours(match?.ThresholdHours, 0, 24, $"Threshold for {code}"),
                MaxHours = NormalizeNullableHours(match?.MaxHours, 0, 24, $"Max hours for {code}"),
                MinimumBlockMinutes = match?.MinimumBlockMinutes is null
                    ? null
                    : ClampInt(match.MinimumBlockMinutes, 0, 480)
            });
        }
        return normalized;
    }

    private static List<OrgAttendanceOvertimeApprovalStepDto> NormalizeApprovalSteps(IEnumerable<OrgAttendanceOvertimeApprovalStepDto>? steps)
    {
        var list = steps?.Where(s => s != null).ToList() ?? new List<OrgAttendanceOvertimeApprovalStepDto>();
        if (list.Count == 0)
        {
            list.Add(new OrgAttendanceOvertimeApprovalStepDto { Role = "SUPERVISOR", Required = true });
        }

        var normalized = new List<OrgAttendanceOvertimeApprovalStepDto>();
        var order = 1;
        foreach (var step in list)
        {
            var role = string.IsNullOrWhiteSpace(step.Role) ? "SUPERVISOR" : step.Role.Trim().ToUpperInvariant();
            normalized.Add(new OrgAttendanceOvertimeApprovalStepDto
            {
                Order = order++,
                Role = role,
                Required = step.Required
            });
        }

        return normalized;
    }

    private static OrgAttendanceOvertimeScopeFiltersDto NormalizeScopeFilters(string scopeMode, OrgAttendanceOvertimeScopeFiltersDto? filters)
    {
        if (scopeMode != "SELECTED")
            return new OrgAttendanceOvertimeScopeFiltersDto();

        var normalized = new OrgAttendanceOvertimeScopeFiltersDto
        {
            Departments = NormalizeStringList(filters?.Departments),
            Sites = NormalizeStringList(filters?.Sites),
            EmploymentTypes = NormalizeStringList(filters?.EmploymentTypes),
            Roles = NormalizeStringList(filters?.Roles)
        };

        if (!normalized.Departments.Any() && !normalized.Sites.Any() && !normalized.EmploymentTypes.Any() && !normalized.Roles.Any())
            throw new OrgAttendanceOvertimeValidationException("Add at least one eligibility group when limiting scope.");

        return normalized;
    }

    private static List<string> NormalizeStringList(IEnumerable<string>? values)
    {
        return values == null
            ? new List<string>()
            : values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    private static string NormalizeTime(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new OrgAttendanceOvertimeValidationException($"{field} is required.");

        var trimmed = value.Trim();
        if (!TimeRegex.IsMatch(trimmed))
            throw new OrgAttendanceOvertimeValidationException($"{field} must be in HH:mm format.");

        return trimmed;
    }

    private static bool IsValidWindow(string start, string end)
    {
        var startTs = TimeSpan.ParseExact(start, @"hh\:mm", CultureInfo.InvariantCulture);
        var endTs = TimeSpan.ParseExact(end, @"hh\:mm", CultureInfo.InvariantCulture);
        return startTs != endTs;
    }
}

public sealed class OrgAttendanceOvertimeValidationException : Exception
{
    public OrgAttendanceOvertimeValidationException(string message) : base(message) { }
}
