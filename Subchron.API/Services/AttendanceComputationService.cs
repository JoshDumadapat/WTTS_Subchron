using Subchron.API.Models.Organizations;

namespace Subchron.API.Services;

public interface IAttendanceComputationService
{
    AttendanceComputationResult ComputeDaily(AttendanceComputationInput input);
}

public class AttendanceComputationService : IAttendanceComputationService
{
    private static readonly string[] KnownBuckets =
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

    public AttendanceComputationResult ComputeDaily(AttendanceComputationInput input)
    {
        var result = new AttendanceComputationResult
        {
            DetectedAttendanceDayType = NormalizeBucket(input.AttendanceDayType)
        };

        foreach (var bucket in KnownBuckets)
            result.OvertimeMinutesByBucket[bucket] = 0;

        if (!input.TimeIn.HasValue || !input.TimeOut.HasValue || input.TimeOut <= input.TimeIn)
            return result;

        var workedMinutes = (int)Math.Floor((input.TimeOut.Value - input.TimeIn.Value).TotalMinutes);
        if (workedMinutes <= 0)
            return result;

        workedMinutes -= ResolveBreakMinutes(input.ShiftTemplate);
        workedMinutes = Math.Max(workedMinutes, 0);

        var scheduleRange = ResolveScheduleWindow(input, input.TimeIn.Value);
        var scheduledMinutes = scheduleRange.ScheduledMinutes;
        var actualStart = input.TimeIn.Value;
        var actualEnd = input.TimeOut.Value;

        if (scheduleRange.Start.HasValue)
            result.LateMinutes = Math.Max(0, (int)Math.Floor((actualStart - scheduleRange.Start.Value).TotalMinutes));

        if (scheduleRange.End.HasValue)
            result.UndertimeMinutes = Math.Max(0, (int)Math.Floor((scheduleRange.End.Value - actualEnd).TotalMinutes));

        result.WorkedMinutes = workedMinutes;

        var overtimeLimitResult = ComputeOvertimeMinutes(workedMinutes, scheduledMinutes, input);
        var overtimeMinutes = overtimeLimitResult.Minutes;
        var dayBucket = result.DetectedAttendanceDayType;
        result.OvertimeMinutesByBucket[dayBucket] = overtimeMinutes;

        if (overtimeLimitResult.Warnings.Count > 0)
            result.Warnings.AddRange(overtimeLimitResult.Warnings);
        result.HardStopTriggered = overtimeLimitResult.HardStop;

        result.NightDifferentialMinutes = ComputeNightDifferentialMinutes(input, actualStart, actualEnd);
        return result;
    }

    private static OvertimeLimitComputationResult ComputeOvertimeMinutes(int workedMinutes, int scheduledMinutes, AttendanceComputationInput input)
    {
        var overtime = input.OvertimeSettings ?? new OrgOvertimeSettingsDto();
        if (!overtime.Enabled)
            return new OvertimeLimitComputationResult();

        var thresholdMinutes = (int)Math.Round(overtime.MinHoursBeforeOvertime * 60m);
        var overtimeMinutes = overtime.Basis switch
        {
            "AfterTotalHoursDay" => Math.Max(0, workedMinutes - thresholdMinutes),
            "AfterTotalHoursWeek" => Math.Max(0, workedMinutes - thresholdMinutes),
            _ => Math.Max(0, workedMinutes - Math.Max(thresholdMinutes, scheduledMinutes))
        };

        if (overtime.MinimumBlockMinutes > 0 && overtimeMinutes < overtime.MinimumBlockMinutes)
            overtimeMinutes = 0;

        if (overtime.RoundToMinutes > 0)
            overtimeMinutes = (overtimeMinutes / overtime.RoundToMinutes) * overtime.RoundToMinutes;

        return ApplyCaps(overtimeMinutes, overtime, input.WeekToDateOvertimeMinutes);
    }

    private static OvertimeLimitComputationResult ApplyCaps(int overtimeMinutes, OrgOvertimeSettingsDto overtime, int weekToDateMinutes)
    {
        var result = new OvertimeLimitComputationResult
        {
            Minutes = Math.Max(overtimeMinutes, 0)
        };

        var limitMode = string.Equals(overtime.LimitMode, "HARD", StringComparison.OrdinalIgnoreCase) ? "HARD" : "SOFT";
        var overrideRole = FormatRole(overtime.OverrideRole);

        if (overtime.MaxHoursPerDay.HasValue)
        {
            var capMinutes = (int)Math.Round(Math.Max(0, overtime.MaxHoursPerDay.Value) * 60m);
            if (capMinutes >= 0 && result.Minutes > capMinutes)
            {
                result.Minutes = capMinutes;
                var message = BuildLimitMessage("daily", limitMode, overrideRole);
                if (!string.IsNullOrEmpty(message))
                    result.Warnings.Add(message);
                result.HardStop |= limitMode == "HARD";
            }
        }

        if (overtime.MaxHoursPerWeek.HasValue)
        {
            var capMinutes = (int)Math.Round(Math.Max(0, overtime.MaxHoursPerWeek.Value) * 60m);
            if (capMinutes >= 0)
            {
                var prior = Math.Max(0, weekToDateMinutes);
                var remaining = Math.Max(0, capMinutes - prior);
                if (result.Minutes > remaining)
                {
                    result.Minutes = remaining;
                    var message = BuildLimitMessage("weekly", limitMode, overrideRole);
                    if (!string.IsNullOrEmpty(message))
                        result.Warnings.Add(message);
                    result.HardStop |= limitMode == "HARD";
                }
            }
        }

        return result;
    }

    private static int ComputeNightDifferentialMinutes(AttendanceComputationInput input, DateTime actualStart, DateTime actualEnd)
    {
        var settings = input.NightDifferentialSettings ?? new OrgNightDifferentialSettingsDto();
        if (!settings.Enabled)
            return 0;

        if (actualEnd <= actualStart)
            return 0;

        var excluded = new HashSet<string>(settings.ExcludedAttendanceBuckets ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        if (excluded.Contains(NormalizeBucket(input.AttendanceDayType)))
            return 0;

        var ndStart = ParseOrDefault(settings.StartTime, new TimeSpan(22, 0, 0));
        var ndEnd = ParseOrDefault(settings.EndTime, new TimeSpan(6, 0, 0));
        var minBlock = Math.Max(0, settings.MinimumMinutes);

        var startDay = actualStart.Date.AddDays(-1);
        var finalDay = actualEnd.Date.AddDays(1);
        var minutes = 0;

        for (var day = startDay; day <= finalDay; day = day.AddDays(1))
        {
            var window = BuildNightWindow(day, ndStart, ndEnd);
            if (window.End <= window.Start)
                continue;

            var overlapStart = actualStart > window.Start ? actualStart : window.Start;
            var overlapEnd = actualEnd < window.End ? actualEnd : window.End;
            if (overlapEnd <= overlapStart)
                continue;

            var segmentMinutes = (int)Math.Floor((overlapEnd - overlapStart).TotalMinutes);
            if (segmentMinutes < minBlock)
                continue;

            minutes += segmentMinutes;
        }

        return Math.Max(minutes, 0);
    }

    private static (DateTime? Start, DateTime? End, int ScheduledMinutes) ResolveScheduleWindow(AttendanceComputationInput input, DateTime baseline)
    {
        var shift = input.ShiftTemplate;
        if (shift == null)
            return (null, null, 0);

        if (string.Equals(shift.Type, "Open", StringComparison.OrdinalIgnoreCase))
            return (null, null, 0);

        string? startTime = null;
        string? endTime = null;

        if (string.Equals(shift.Type, "Flexible", StringComparison.OrdinalIgnoreCase))
        {
            startTime = shift.Flexible?.EarliestStart;
            endTime = shift.Flexible?.LatestEnd;
        }
        else
        {
            startTime = shift.Fixed?.StartTime;
            endTime = shift.Fixed?.EndTime;
        }

        if (!TimeSpan.TryParse(startTime, out var startTs) || !TimeSpan.TryParse(endTime, out var endTs))
            return (null, null, 0);

        var scheduleStart = baseline.Date.Add(startTs);
        var scheduleEnd = endTs > startTs ? baseline.Date.Add(endTs) : baseline.Date.AddDays(1).Add(endTs);
        var scheduledMinutes = (int)Math.Max(0, Math.Floor((scheduleEnd - scheduleStart).TotalMinutes));
        return (scheduleStart, scheduleEnd, scheduledMinutes);
    }

    private static int ResolveBreakMinutes(OrgShiftTemplateDto? shift)
    {
        if (shift == null)
            return 0;

        if (shift.Breaks != null && shift.Breaks.Count > 0)
        {
            var sum = 0;
            foreach (var b in shift.Breaks)
            {
                if (!TimeSpan.TryParse(b.StartTime, out var start) || !TimeSpan.TryParse(b.EndTime, out var end))
                    continue;
                var diff = end > start ? end - start : (TimeSpan.FromHours(24) - start) + end;
                sum += Math.Max(0, (int)Math.Floor(diff.TotalMinutes));
            }
            return Math.Max(sum, 0);
        }

        return shift.Fixed?.BreakMinutes ?? 0;
    }

    private static string NormalizeBucket(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "RegularDay";

        var normalized = value.Trim();
        return KnownBuckets.FirstOrDefault(v => string.Equals(v, normalized, StringComparison.OrdinalIgnoreCase)) ?? "RegularDay";
    }

    private static TimeSpan ParseOrDefault(string value, TimeSpan fallback)
    {
        return TimeSpan.TryParse(value, out var ts) ? ts : fallback;
    }

    private static (DateTime Start, DateTime End) BuildNightWindow(DateTime day, TimeSpan ndStart, TimeSpan ndEnd)
    {
        var start = day.Add(ndStart);
        var end = ndEnd > ndStart ? day.Add(ndEnd) : day.AddDays(1).Add(ndEnd);
        return (start, end);
    }

    private static string BuildLimitMessage(string scope, string mode, string? overrideRole)
    {
        var scopeLabel = scope == "weekly" ? "weekly" : "daily";
        if (mode == "HARD")
            return $"Overtime {scopeLabel} cap reached. Additional OT is blocked.";

        if (!string.IsNullOrWhiteSpace(overrideRole))
            return $"Overtime {scopeLabel} cap reached. Contact {overrideRole} to override.";

        return $"Overtime {scopeLabel} cap reached. Supervisor override required.";
    }

    private static string? FormatRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;
        return role.Replace('_', ' ');
    }

    private sealed class OvertimeLimitComputationResult
    {
        public int Minutes { get; set; }
        public List<string> Warnings { get; } = new();
        public bool HardStop { get; set; }
    }
}
