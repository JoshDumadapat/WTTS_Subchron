namespace Subchron.API.Models.Organizations;

public class OrgAttendanceSettingsUpdateRequest
{
    public string PrimaryMode { get; set; } = "QR";

    public bool AllowManualEntry { get; set; }
    public bool RequireGeo { get; set; }
    public bool EnforceGeofence { get; set; }
    public bool RestrictByIp { get; set; }
    public bool PreventDoubleClockIn { get; set; }

    public bool AutoClockOutEnabled { get; set; }
    public decimal? AutoClockOutMaxHours { get; set; }

    public string? DefaultShiftTemplateCode { get; set; }
}
