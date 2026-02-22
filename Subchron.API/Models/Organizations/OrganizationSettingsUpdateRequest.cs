namespace Subchron.API.Models.Organizations;

public class OrganizationSettingsUpdateRequest
{
    public string Timezone { get; set; } = "Asia/Manila";
    public string Currency { get; set; } = "PHP";
    public string AttendanceMode { get; set; } = "QR";

    public bool AllowManualEntry { get; set; }
    public bool RequireGeo { get; set; }
    public bool EnforceGeofence { get; set; }

    public int DefaultGraceMinutes { get; set; }
    public string RoundRule { get; set; } = "None";

    public bool OTEnabled { get; set; }
    public decimal OTThresholdHours { get; set; }
    public bool OTApprovalRequired { get; set; }
    public decimal? OTMaxHoursPerDay { get; set; }

    public bool LeaveEnabled { get; set; }
    public bool LeaveApprovalRequired { get; set; }
}
