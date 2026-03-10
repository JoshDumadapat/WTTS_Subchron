using System.Text.RegularExpressions;
using Subchron.API.Models.Organizations;

namespace Subchron.API.Services;

public static class OrgShiftSettingsValidator
{
    private static readonly string[] OrderedDays = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    private static readonly HashSet<string> AllowedBucketCodes = new(StringComparer.OrdinalIgnoreCase)
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

    private static readonly HashSet<string> AllowedScopeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Department",
        "Site",
        "EmploymentType",
        "Role"
    };

    public static List<OrgShiftTemplateDto> NormalizeTemplates(List<OrgShiftTemplateDto>? templates)
    {
        if (templates == null)
            return new List<OrgShiftTemplateDto>();

        var normalized = new List<OrgShiftTemplateDto>();
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var template in templates)
        {
            if (template == null)
                continue;

            if (string.IsNullOrWhiteSpace(template.Name))
                throw new ShiftSettingsValidationException("Shift name is required for every template.");

            var type = NormalizeType(template.Type);
            var code = NormalizeCode(template.Code, template.Name, usedCodes);
            var workDays = NormalizeWorkDays(template.WorkDays);
            var breaks = NormalizeBreaks(template.Breaks);
            var dayOverrides = NormalizeDayOverrides(template.DayOverrides);
            var disabledReason = NormalizeDisabledReason(template.DisabledReason, template.IsActive, template.Name);

            OrgShiftFixedSettings? fixedSettings = null;
            OrgShiftFlexibleSettings? flexibleSettings = null;
            OrgShiftOpenSettings? openSettings = null;

            switch (type)
            {
                case "Fixed":
                    fixedSettings = ValidateFixed(template.Fixed);
                    break;
                case "Flexible":
                    flexibleSettings = ValidateFlexible(template.Flexible);
                    break;
                case "Open":
                    openSettings = ValidateOpen(template.Open);
                    break;
            }

            EnsureBreaksInsideTemplateWindow(type, fixedSettings, flexibleSettings, breaks);

            normalized.Add(new OrgShiftTemplateDto
            {
                Code = code,
                Name = template.Name.Trim(),
                Type = type,
                WorkDays = workDays,
                Fixed = fixedSettings,
                Flexible = flexibleSettings,
                Open = openSettings,
                Breaks = breaks,
                DayOverrides = dayOverrides,
                IsActive = template.IsActive,
                DisabledReason = disabledReason
            });
        }

        return normalized;
    }

    public static OrgOvertimeSettingsDto NormalizeOvertime(OrgOvertimeSettingsDto? overtime)
    {
        if (overtime == null)
            return new OrgOvertimeSettingsDto();

        var dto = new OrgOvertimeSettingsDto
        {
            Enabled = overtime.Enabled,
            MinHoursBeforeOvertime = ClampDecimal(overtime.MinHoursBeforeOvertime, 0m, 24m, "Minimum hours before overtime must be between 0 and 24."),
            Basis = NormalizeBasis(overtime.Basis),
            PreApprovalRequired = overtime.PreApprovalRequired,
            ApproverRole = NormalizeApprover(overtime.ApproverRole),
            AutoApprove = overtime.AutoApprove,
            RoundToMinutes = NormalizeRoundIncrement(Clamp(overtime.RoundToMinutes, 0, 60, "Round to minutes must be between 0 and 60.")),
            MinimumBlockMinutes = Clamp(overtime.MinimumBlockMinutes, 0, 240, "Minimum OT block must be between 0 and 240."),
            MaxHoursPerDay = NormalizeNullableHours(overtime.MaxHoursPerDay, 0m, 24m, "Max OT per day must be between 0 and 24."),
            MaxHoursPerWeek = NormalizeNullableHours(overtime.MaxHoursPerWeek, 0m, 168m, "Max OT per week must be between 0 and 168."),
            HardStopEnabled = overtime.HardStopEnabled,
            DayTypes = new OrgOvertimeDayTypeRules
            {
                RegularDayBehavior = NormalizeNonEmpty(overtime.DayTypes?.RegularDayBehavior, "Default"),
                RestDayBehavior = NormalizeNonEmpty(overtime.DayTypes?.RestDayBehavior, "SeparateBucket"),
                HolidayBehavior = NormalizeNonEmpty(overtime.DayTypes?.HolidayBehavior, "SeparateBucket")
            },
            BucketRules = NormalizeBucketRules(overtime.BucketRules),
            ScopeRules = NormalizeScopeRules(overtime.ScopeRules),
            ApprovalSteps = NormalizeApprovalSteps(overtime.ApprovalSteps)
        };

        if (dto.AutoApprove)
            dto.PreApprovalRequired = false;

        return dto;
    }

    public static OrgNightDifferentialSettingsDto NormalizeNightDifferential(OrgNightDifferentialSettingsDto? settings)
    {
        if (settings == null)
            return new OrgNightDifferentialSettingsDto();

        var start = NormalizeTime(settings.StartTime, "Night differential start time");
        var end = NormalizeTime(settings.EndTime, "Night differential end time");
        if (!IsValidOvernightWindow(TimeSpan.Parse(start), TimeSpan.Parse(end)))
            throw new ShiftSettingsValidationException("Night differential window must either be same-day ascending or valid PM-to-AM cross-midnight range.");

        return new OrgNightDifferentialSettingsDto
        {
            Enabled = settings.Enabled,
            StartTime = start,
            EndTime = end,
            MinimumMinutes = Clamp(settings.MinimumMinutes, 0, 720, "Night differential minimum minutes must be between 0 and 720."),
            ExcludedAttendanceBuckets = (settings.ExcludedAttendanceBuckets ?? new List<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static List<OrgOvertimeBucketRuleDto> NormalizeBucketRules(List<OrgOvertimeBucketRuleDto>? rules)
    {
        if (rules == null || rules.Count == 0)
            return BuildDefaultBucketRules();

        var normalized = new List<OrgOvertimeBucketRuleDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules)
        {
            var code = NormalizeNonEmpty(rule.BucketCode, "RegularDay");
            if (!AllowedBucketCodes.Contains(code))
                throw new ShiftSettingsValidationException($"Unsupported OT bucket code: {code}.");
            if (!seen.Add(code))
                throw new ShiftSettingsValidationException($"Duplicate OT bucket rule: {code}.");

            var threshold = NormalizeNullableHours(rule.ThresholdHours, 0m, 24m, "Bucket threshold hours must be between 0 and 24.");
            var maxHours = NormalizeNullableHours(rule.MaxHours, 0m, 24m, "Bucket max hours must be between 0 and 24.");
            if (threshold.HasValue && maxHours.HasValue && maxHours < threshold)
                throw new ShiftSettingsValidationException($"Bucket max hours must be greater than or equal to threshold hours for {code}.");

            normalized.Add(new OrgOvertimeBucketRuleDto
            {
                BucketCode = code,
                Enabled = rule.Enabled,
                ThresholdHours = threshold,
                MaxHours = maxHours,
                MinimumBlockMinutes = Clamp(rule.MinimumBlockMinutes, 0, 240, "Bucket minimum block minutes must be between 0 and 240.")
            });
        }

        return normalized;
    }

    private static List<OrgOvertimeScopeRuleDto> NormalizeScopeRules(List<OrgOvertimeScopeRuleDto>? rules)
    {
        if (rules == null)
            return new List<OrgOvertimeScopeRuleDto>();

        var normalized = new List<OrgOvertimeScopeRuleDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules)
        {
            var scopeType = NormalizeNonEmpty(rule.ScopeType, "Department");
            if (!AllowedScopeTypes.Contains(scopeType))
                throw new ShiftSettingsValidationException($"Invalid overtime scope type: {scopeType}.");

            var values = (rule.Values ?? new List<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (values.Count == 0)
                throw new ShiftSettingsValidationException($"Overtime scope '{scopeType}' must include at least one value.");

            var key = scopeType + "|" + (rule.Include ? "I" : "E");
            if (!seen.Add(key))
                throw new ShiftSettingsValidationException($"Duplicate overtime scope rule for {scopeType} and include/exclude type.");

            normalized.Add(new OrgOvertimeScopeRuleDto
            {
                ScopeType = scopeType,
                Include = rule.Include,
                Values = values
            });
        }

        return normalized;
    }

    private static List<OrgOvertimeApprovalStepDto> NormalizeApprovalSteps(List<OrgOvertimeApprovalStepDto>? steps)
    {
        if (steps == null || steps.Count == 0)
            return new List<OrgOvertimeApprovalStepDto>
            {
                new() { Order = 1, Role = "Supervisor", Required = true }
            };

        var ordered = steps
            .Select(s => new OrgOvertimeApprovalStepDto
            {
                Order = s.Order,
                Role = NormalizeApprover(s.Role),
                Required = s.Required
            })
            .OrderBy(s => s.Order)
            .ToList();

        if (ordered.Any(s => s.Order <= 0))
            throw new ShiftSettingsValidationException("Approval step order must start at 1 and be positive.");

        var orderSet = new HashSet<int>();
        foreach (var step in ordered)
        {
            if (!orderSet.Add(step.Order))
                throw new ShiftSettingsValidationException("Approval step order must be unique.");
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Order != i + 1)
                throw new ShiftSettingsValidationException("Approval step order must be sequential (1..N).");
        }

        return ordered;
    }

    private static List<OrgShiftBreakDto> NormalizeBreaks(List<OrgShiftBreakDto>? breaks)
    {
        if (breaks == null || breaks.Count == 0)
            return new List<OrgShiftBreakDto>();

        var normalized = breaks.Select((item, index) =>
        {
            var start = NormalizeTime(item.StartTime, $"Break #{index + 1} start");
            var end = NormalizeTime(item.EndTime, $"Break #{index + 1} end");

            var startTs = TimeSpan.Parse(start);
            var endTs = TimeSpan.Parse(end);
            if (!IsValidOvernightWindow(startTs, endTs))
                throw new ShiftSettingsValidationException($"Break #{index + 1} has invalid time window.");

            return new OrgShiftBreakDto
            {
                Name = string.IsNullOrWhiteSpace(item.Name) ? $"Break {index + 1}" : item.Name.Trim(),
                StartTime = start,
                EndTime = end,
                IsPaid = item.IsPaid
            };
        }).ToList();

        EnsureNoOverlaps(normalized.Select(b => new OrgShiftWindowDto { StartTime = b.StartTime, EndTime = b.EndTime }).ToList(), "Break windows overlap.");
        return normalized;
    }

    private static List<OrgShiftDayOverrideDto> NormalizeDayOverrides(List<OrgShiftDayOverrideDto>? overrides)
    {
        if (overrides == null || overrides.Count == 0)
            return new List<OrgShiftDayOverrideDto>();

        var seenDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var output = new List<OrgShiftDayOverrideDto>();

        foreach (var dayOverride in overrides)
        {
            var day = NormalizeDay(dayOverride.Day);
            if (!seenDays.Add(day))
                throw new ShiftSettingsValidationException($"Duplicate day override found for {day}.");

            var windows = new List<OrgShiftWindowDto>();
            if (!dayOverride.IsOffDay)
            {
                windows = (dayOverride.WorkWindows ?? new List<OrgShiftWindowDto>())
                    .Select((window, index) =>
                    {
                        var start = NormalizeTime(window.StartTime, $"{day} override window #{index + 1} start");
                        var end = NormalizeTime(window.EndTime, $"{day} override window #{index + 1} end");
                        if (!IsValidOvernightWindow(TimeSpan.Parse(start), TimeSpan.Parse(end)))
                            throw new ShiftSettingsValidationException($"{day} override window #{index + 1} has invalid time range.");
                        return new OrgShiftWindowDto { StartTime = start, EndTime = end };
                    })
                    .ToList();

                EnsureNoOverlaps(windows, $"{day} override windows overlap.");
            }

            output.Add(new OrgShiftDayOverrideDto
            {
                Day = day,
                IsOffDay = dayOverride.IsOffDay,
                WorkWindows = windows
            });
        }

        return output.OrderBy(d => Array.IndexOf(OrderedDays, d.Day)).ToList();
    }

    private static void EnsureBreaksInsideTemplateWindow(string type, OrgShiftFixedSettings? fixedSettings, OrgShiftFlexibleSettings? flexibleSettings, List<OrgShiftBreakDto> breaks)
    {
        if (breaks.Count == 0 || string.Equals(type, "Open", StringComparison.OrdinalIgnoreCase))
            return;

        string? start = null;
        string? end = null;
        if (string.Equals(type, "Fixed", StringComparison.OrdinalIgnoreCase))
        {
            start = fixedSettings?.StartTime;
            end = fixedSettings?.EndTime;
        }
        else if (string.Equals(type, "Flexible", StringComparison.OrdinalIgnoreCase))
        {
            start = flexibleSettings?.EarliestStart;
            end = flexibleSettings?.LatestEnd;
        }

        if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
            return;

        var shiftSegments = GetSegments(start, end);
        foreach (var item in breaks)
        {
            var breakSegments = GetSegments(item.StartTime!, item.EndTime!);
            foreach (var segment in breakSegments)
            {
                if (!shiftSegments.Any(s => segment.Start >= s.Start && segment.End <= s.End))
                    throw new ShiftSettingsValidationException($"Break '{item.Name}' must fall within the configured shift window.");
            }
        }
    }

    private static void EnsureNoOverlaps(List<OrgShiftWindowDto> windows, string message)
    {
        var segments = windows
            .SelectMany(w => GetSegments(w.StartTime!, w.EndTime!))
            .OrderBy(s => s.Start)
            .ToList();

        for (var i = 1; i < segments.Count; i++)
        {
            if (segments[i].Start < segments[i - 1].End)
                throw new ShiftSettingsValidationException(message);
        }
    }

    private static List<(int Start, int End)> GetSegments(string startTime, string endTime)
    {
        var start = ToMinutes(startTime);
        var end = ToMinutes(endTime);
        if (end > start)
            return new List<(int Start, int End)> { (start, end) };
        return new List<(int Start, int End)> { (start, 1440), (0, end) };
    }

    private static int ToMinutes(string value)
    {
        var ts = TimeSpan.Parse(value);
        return (int)ts.TotalMinutes;
    }

    private static string NormalizeType(string? type)
    {
        var value = (type ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "flex" or "flexible" => "Flexible",
            "open" => "Open",
            _ => "Fixed"
        };
    }

    private static List<string> NormalizeWorkDays(List<string>? workDays)
    {
        if (workDays == null || workDays.Count == 0)
            return new List<string>();

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var day in workDays)
        {
            var value = NormalizeDay(day);
            if (!seen.Add(value))
                continue;
            normalized.Add(value);
        }

        normalized.Sort((a, b) => Array.IndexOf(OrderedDays, a).CompareTo(Array.IndexOf(OrderedDays, b)));
        return normalized;
    }

    private static string NormalizeDay(string? day)
    {
        if (string.IsNullOrWhiteSpace(day))
            throw new ShiftSettingsValidationException("Day value is required.");

        var key = day.Trim().ToLowerInvariant();
        return key switch
        {
            "m" or "mon" or "monday" => "Mon",
            "t" or "tue" or "tues" or "tuesday" => "Tue",
            "w" or "wed" or "wednesday" => "Wed",
            "th" or "thu" or "thur" or "thurs" or "thursday" => "Thu",
            "f" or "fri" or "friday" => "Fri",
            "sat" or "saturday" => "Sat",
            "sun" or "sunday" => "Sun",
            _ => throw new ShiftSettingsValidationException($"Invalid day value: {day}.")
        };
    }

    private static OrgShiftFixedSettings ValidateFixed(OrgShiftFixedSettings? settings)
    {
        if (settings == null)
            throw new ShiftSettingsValidationException("Fixed shift requires start, end, break, and grace settings.");

        var start = NormalizeTime(settings.StartTime, "start time");
        var end = NormalizeTime(settings.EndTime, "end time");

        if (!TimeSpan.TryParse(start, out var startTs) || !TimeSpan.TryParse(end, out var endTs))
            throw new ShiftSettingsValidationException("Invalid fixed shift time window.");

        if (!IsValidOvernightWindow(startTs, endTs))
            throw new ShiftSettingsValidationException("Fixed shift end time must be later than start time unless it is a PM-to-AM overnight shift.");

        return new OrgShiftFixedSettings
        {
            StartTime = start,
            EndTime = end,
            BreakMinutes = Clamp(settings.BreakMinutes, 0, 240, "Break minutes must be between 0 and 240."),
            GraceMinutes = Clamp(settings.GraceMinutes, 0, 120, "Grace minutes must be between 0 and 120.")
        };
    }

    private static OrgShiftFlexibleSettings ValidateFlexible(OrgShiftFlexibleSettings? settings)
    {
        if (settings == null)
            throw new ShiftSettingsValidationException("Flexible shift requires window and hour settings.");

        var earliest = NormalizeTime(settings.EarliestStart, "earliest start");
        var latest = NormalizeTime(settings.LatestEnd, "latest end");

        if (!TimeSpan.TryParse(earliest, out var earliestTs) || !TimeSpan.TryParse(latest, out var latestTs))
            throw new ShiftSettingsValidationException("Invalid flexible shift time window.");

        if (!IsValidOvernightWindow(earliestTs, latestTs))
            throw new ShiftSettingsValidationException("Flexible shift latest end must be later than earliest start unless it is a PM-to-AM overnight shift.");

        var requiredDaily = ClampDecimal(settings.RequiredDailyHours, 1m, 24m, "Required daily hours must be between 1 and 24.");
        var maxDaily = ClampDecimal(settings.MaxDailyHours, requiredDaily, 24m, "Max daily hours must be at least required hours and no more than 24.");

        return new OrgShiftFlexibleSettings
        {
            EarliestStart = earliest,
            LatestEnd = latest,
            RequiredDailyHours = requiredDaily,
            MaxDailyHours = maxDaily
        };
    }

    private static OrgShiftOpenSettings ValidateOpen(OrgShiftOpenSettings? settings)
    {
        if (settings == null)
            throw new ShiftSettingsValidationException("Open shift requires weekly hours.");

        return new OrgShiftOpenSettings
        {
            RequiredWeeklyHours = ClampDecimal(settings.RequiredWeeklyHours, 1m, 168m, "Required weekly hours must be between 1 and 168.")
        };
    }

    private static List<OrgOvertimeBucketRuleDto> BuildDefaultBucketRules()
    {
        return AllowedBucketCodes
            .Select(code => new OrgOvertimeBucketRuleDto
            {
                BucketCode = code,
                Enabled = true,
                ThresholdHours = null,
                MaxHours = null,
                MinimumBlockMinutes = 0
            })
            .ToList();
    }

    private static string NormalizeCode(string? code, string name, HashSet<string> used)
    {
        var baseCode = string.IsNullOrWhiteSpace(code) ? Slugify(name) : code.Trim();
        if (string.IsNullOrWhiteSpace(baseCode))
            baseCode = "SHIFT";

        baseCode = baseCode.Replace(' ', '_');
        if (!Regex.IsMatch(baseCode, @"^[A-Za-z0-9_\-]+$"))
            baseCode = Regex.Replace(baseCode, @"[^A-Za-z0-9_\-]", string.Empty);
        if (string.IsNullOrWhiteSpace(baseCode))
            baseCode = "SHIFT";
        baseCode = baseCode.ToUpperInvariant();

        var unique = baseCode;
        var suffix = 1;
        while (!used.Add(unique))
            unique = baseCode + "_" + suffix++;
        return unique;
    }

    private static string Slugify(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return string.Empty;
        var slug = Regex.Replace(trimmed, "[^A-Za-z0-9]+", "_");
        return slug.Trim('_');
    }

    private static string NormalizeTime(string? input, string label)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ShiftSettingsValidationException($"Provide {label}.");
        if (!TimeSpan.TryParse(input, out var ts))
            throw new ShiftSettingsValidationException($"{label} must be in HH:mm format.");
        if (ts.TotalHours < 0 || ts.TotalHours >= 24)
            throw new ShiftSettingsValidationException($"{label} must be between 00:00 and 23:59.");
        return ts.ToString(@"hh\:mm");
    }

    private static string NormalizeBasis(string? basis)
    {
        var value = (basis ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "after_x_hours_day" or "aftertotalhoursday" or "afterhoursday" => "AfterTotalHoursDay",
            "after_x_hours_week" or "aftertotalhoursweek" or "afterhoursweek" => "AfterTotalHoursWeek",
            _ => "AfterShiftEnd"
        };
    }

    private static string NormalizeApprover(string? approver)
    {
        var value = (approver ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "hr" => "HR",
            "manager" => "Manager",
            _ => "Supervisor"
        };
    }

    private static string NormalizeNonEmpty(string? value, string fallback)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }

    private static string? NormalizeDisabledReason(string? value, bool isActive, string templateName)
    {
        if (isActive)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            throw new ShiftSettingsValidationException($"Provide a reason when disabling shift template '{templateName}'.");

        var reason = value.Trim();
        if (reason.Length > 60)
            throw new ShiftSettingsValidationException("Disable reason must be 60 characters or fewer.");

        return reason;
    }

    private static decimal ClampDecimal(decimal value, decimal min, decimal max, string error)
    {
        if (value < min || value > max)
            throw new ShiftSettingsValidationException(error);
        return Math.Round(value, 2);
    }

    private static decimal? NormalizeNullableHours(decimal? value, decimal min, decimal max, string error)
    {
        if (!value.HasValue)
            return null;
        var val = value.Value;
        if (val < min || val > max)
            throw new ShiftSettingsValidationException(error);
        return Math.Round(val, 2);
    }

    private static int Clamp(int value, int min, int max, string error)
    {
        if (value < min || value > max)
            throw new ShiftSettingsValidationException(error);
        return value;
    }

    private static int NormalizeRoundIncrement(int value)
    {
        if (value == 0 || value == 5 || value == 10 || value == 15 || value == 30 || value == 60)
            return value;
        if (value < 0)
            return 0;
        if (value > 60)
            return 60;
        return 15;
    }

    private static bool IsValidOvernightWindow(TimeSpan start, TimeSpan end)
    {
        if (end > start)
            return true;
        return start.TotalHours >= 12 && end.TotalHours < 12;
    }
}

public sealed class ShiftSettingsValidationException : Exception
{
    public ShiftSettingsValidationException(string message) : base(message)
    {
    }
}
