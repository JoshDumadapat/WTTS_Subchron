namespace Subchron.API.Models.Entities;

public class OrganizationSettings
{
    public int OrgID { get; set; }                 // PK + FK
    public Organization Organization { get; set; } = null!;

    public string Timezone { get; set; } = "Asia/Manila";
    public string Currency { get; set; } = "PHP";
    public string AttendanceMode { get; set; } = "QR"; // QR/BioGeo/Hybrid

    public bool AllowManualEntry { get; set; } = false;
    public bool RequireGeo { get; set; } = false;
    public bool EnforceGeofence { get; set; } = false;
    public bool RestrictByIp { get; set; } = false;
    public bool PreventDoubleClockIn { get; set; } = true;

    public int DefaultGraceMinutes { get; set; } = 0;
    public string RoundRule { get; set; } = "None";

    public bool AutoClockOutEnabled { get; set; } = false;
    public decimal? AutoClockOutMaxHours { get; set; }
    public string? DefaultShiftTemplateCode { get; set; }

    public bool OTEnabled { get; set; } = false;
    public decimal OTThresholdHours { get; set; } = 0m;
    public bool OTApprovalRequired { get; set; } = true;
    public decimal? OTMaxHoursPerDay { get; set; }

    public bool LeaveEnabled { get; set; } = false;
    public bool LeaveApprovalRequired { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
