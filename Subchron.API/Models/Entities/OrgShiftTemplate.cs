namespace Subchron.API.Models.Entities;

public class OrgShiftTemplate
{
    public int OrgShiftTemplateID { get; set; }
    public int OrgID { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Fixed";
    public bool IsActive { get; set; } = true;
    public string? DisabledReason { get; set; }

    public string? FixedStartTime { get; set; }
    public string? FixedEndTime { get; set; }
    public int? FixedBreakMinutes { get; set; }
    public int? FixedGraceMinutes { get; set; }

    public string? FlexibleEarliestStart { get; set; }
    public string? FlexibleLatestEnd { get; set; }
    public decimal? FlexibleRequiredDailyHours { get; set; }
    public decimal? FlexibleMaxDailyHours { get; set; }

    public decimal? OpenRequiredWeeklyHours { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrgShiftTemplateWorkDay> WorkDays { get; set; } = new List<OrgShiftTemplateWorkDay>();
    public ICollection<OrgShiftTemplateBreak> Breaks { get; set; } = new List<OrgShiftTemplateBreak>();
    public ICollection<OrgShiftTemplateDayOverride> DayOverrides { get; set; } = new List<OrgShiftTemplateDayOverride>();
}

public class OrgShiftTemplateWorkDay
{
    public int OrgShiftTemplateWorkDayID { get; set; }
    public int OrgShiftTemplateID { get; set; }
    public string DayCode { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class OrgShiftTemplateBreak
{
    public int OrgShiftTemplateBreakID { get; set; }
    public int OrgShiftTemplateID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public int SortOrder { get; set; }
}

public class OrgShiftTemplateDayOverride
{
    public int OrgShiftTemplateDayOverrideID { get; set; }
    public int OrgShiftTemplateID { get; set; }
    public string Day { get; set; } = string.Empty;
    public bool IsOffDay { get; set; }
    public int SortOrder { get; set; }

    public ICollection<OrgShiftTemplateOverrideWindow> WorkWindows { get; set; } = new List<OrgShiftTemplateOverrideWindow>();
}

public class OrgShiftTemplateOverrideWindow
{
    public int OrgShiftTemplateOverrideWindowID { get; set; }
    public int OrgShiftTemplateDayOverrideID { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
