namespace Subchron.API.Models.Organizations;

public class OrgAttendanceSettingsUpdateRequest
{
    public string PrimaryMode { get; set; } = "QR";

    public bool AllowManualEntry { get; set; }
    public string ManualEntryAccessMode { get; set; } = "SUPERVISOR";
    public bool RequireGeo { get; set; }
    public bool EnforceGeofence { get; set; }
    public bool RestrictByIp { get; set; }
    public bool PreventDoubleClockIn { get; set; }

    public int? EarliestClockInMinutes { get; set; }
    public int? LatestClockInMinutes { get; set; }

    public bool AllowIncompleteLogs { get; set; }
    public bool AutoFlagMissingPunch { get; set; }
    public string DefaultMissingPunchAction { get; set; } = "IGNORE";

    public bool UseGracePeriodForLate { get; set; }
    public bool MarkUndertimeBasedOnSchedule { get; set; }
    public bool AutoAbsentWithoutLog { get; set; }

    public bool AutoClockOutEnabled { get; set; }
    public decimal? AutoClockOutMaxHours { get; set; }

    public string? DefaultShiftTemplateCode { get; set; }
}
