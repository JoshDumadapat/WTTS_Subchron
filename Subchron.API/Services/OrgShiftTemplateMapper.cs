using Subchron.API.Models.Entities;
using Subchron.API.Models.Organizations;

namespace Subchron.API.Services;

public static class OrgShiftTemplateMapper
{
    private static readonly string[] OrderedDays = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    private static readonly Dictionary<string, int> DayOrder = OrderedDays
        .Select((day, index) => (day, index))
        .ToDictionary(x => x.day, x => x.index, StringComparer.OrdinalIgnoreCase);

    public static OrgShiftTemplateDto ToDto(OrgShiftTemplate entity)
    {
        var dto = new OrgShiftTemplateDto
        {
            Code = entity.Code,
            Name = entity.Name,
            Type = entity.Type,
            IsActive = entity.IsActive,
            DisabledReason = entity.DisabledReason,
            WorkDays = entity.WorkDays
                .OrderBy(w => DayOrder.TryGetValue(w.DayCode, out var order) ? order : int.MaxValue)
                .Select(w => w.DayCode)
                .ToList(),
            Breaks = entity.Breaks
                .OrderBy(b => b.SortOrder)
                .Select(b => new OrgShiftBreakDto
                {
                    Name = b.Name,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    IsPaid = b.IsPaid
                }).ToList(),
            DayOverrides = entity.DayOverrides
                .OrderBy(d => DayOrder.TryGetValue(d.Day, out var order) ? order : int.MaxValue)
                .ThenBy(d => d.SortOrder)
                .Select(d => new OrgShiftDayOverrideDto
                {
                    Day = d.Day,
                    IsOffDay = d.IsOffDay,
                    WorkWindows = d.WorkWindows
                        .OrderBy(w => w.SortOrder)
                        .Select(w => new OrgShiftWindowDto
                        {
                            StartTime = w.StartTime,
                            EndTime = w.EndTime
                        }).ToList()
                }).ToList()
        };

        if (string.Equals(entity.Type, "Fixed", StringComparison.OrdinalIgnoreCase))
        {
            dto.Fixed = new OrgShiftFixedSettings
            {
                StartTime = entity.FixedStartTime,
                EndTime = entity.FixedEndTime,
                BreakMinutes = entity.FixedBreakMinutes ?? 0,
                GraceMinutes = entity.FixedGraceMinutes ?? 0
            };
        }
        else if (string.Equals(entity.Type, "Flexible", StringComparison.OrdinalIgnoreCase))
        {
            dto.Flexible = new OrgShiftFlexibleSettings
            {
                EarliestStart = entity.FlexibleEarliestStart,
                LatestEnd = entity.FlexibleLatestEnd,
                RequiredDailyHours = entity.FlexibleRequiredDailyHours ?? 0,
                MaxDailyHours = entity.FlexibleMaxDailyHours ?? 0
            };
        }
        else if (string.Equals(entity.Type, "Open", StringComparison.OrdinalIgnoreCase))
        {
            dto.Open = new OrgShiftOpenSettings
            {
                RequiredWeeklyHours = entity.OpenRequiredWeeklyHours ?? 0
            };
        }

        return dto;
    }

    public static OrgShiftTemplate CreateEntity(int orgId, OrgShiftTemplateDto dto)
    {
        var entity = new OrgShiftTemplate
        {
            OrgID = orgId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ApplyDto(entity, dto);
        return entity;
    }

    public static void ApplyDto(OrgShiftTemplate entity, OrgShiftTemplateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new InvalidOperationException("Shift template code is required.");

        entity.Code = dto.Code.Trim();
        entity.Name = dto.Name.Trim();
        entity.Type = dto.Type?.Trim() ?? "Fixed";
        entity.IsActive = dto.IsActive;
        entity.DisabledReason = dto.IsActive ? null : dto.DisabledReason?.Trim();
        entity.FixedStartTime = dto.Fixed?.StartTime;
        entity.FixedEndTime = dto.Fixed?.EndTime;
        entity.FixedBreakMinutes = dto.Fixed?.BreakMinutes;
        entity.FixedGraceMinutes = dto.Fixed?.GraceMinutes;
        entity.FlexibleEarliestStart = dto.Flexible?.EarliestStart;
        entity.FlexibleLatestEnd = dto.Flexible?.LatestEnd;
        entity.FlexibleRequiredDailyHours = dto.Flexible?.RequiredDailyHours;
        entity.FlexibleMaxDailyHours = dto.Flexible?.MaxDailyHours;
        entity.OpenRequiredWeeklyHours = dto.Open?.RequiredWeeklyHours;
        entity.UpdatedAt = DateTime.UtcNow;

        entity.WorkDays.Clear();
        var workDays = (dto.WorkDays ?? new List<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        for (var idx = 0; idx < workDays.Count; idx++)
        {
            var day = workDays[idx];
            entity.WorkDays.Add(new OrgShiftTemplateWorkDay
            {
                DayCode = day,
                SortOrder = idx
            });
        }

        entity.Breaks.Clear();
        var breaks = dto.Breaks ?? new List<OrgShiftBreakDto>();
        for (var idx = 0; idx < breaks.Count; idx++)
        {
            var item = breaks[idx];
            entity.Breaks.Add(new OrgShiftTemplateBreak
            {
                Name = string.IsNullOrWhiteSpace(item.Name) ? $"Break {idx + 1}" : item.Name.Trim(),
                StartTime = item.StartTime ?? string.Empty,
                EndTime = item.EndTime ?? string.Empty,
                IsPaid = item.IsPaid,
                SortOrder = idx
            });
        }

        entity.DayOverrides.Clear();
        var overrides = dto.DayOverrides ?? new List<OrgShiftDayOverrideDto>();
        for (var idx = 0; idx < overrides.Count; idx++)
        {
            var item = overrides[idx];
            var overrideEntity = new OrgShiftTemplateDayOverride
            {
                Day = item.Day,
                IsOffDay = item.IsOffDay,
                SortOrder = idx
            };

            var windows = item.WorkWindows ?? new List<OrgShiftWindowDto>();
            for (var wIdx = 0; wIdx < windows.Count; wIdx++)
            {
                var window = windows[wIdx];
                overrideEntity.WorkWindows.Add(new OrgShiftTemplateOverrideWindow
                {
                    StartTime = window.StartTime ?? string.Empty,
                    EndTime = window.EndTime ?? string.Empty,
                    SortOrder = wIdx
                });
            }

            entity.DayOverrides.Add(overrideEntity);
        }
    }
}
