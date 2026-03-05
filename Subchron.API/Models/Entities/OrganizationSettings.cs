using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Subchron.API.Models.LeaveSettings;
using Subchron.API.Models.Organizations;

namespace Subchron.API.Models.Entities;

public class OrganizationSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
    public string? ShiftTemplatesJson { get; set; }
    public string? OvertimeSettingsJson { get; set; }
    public string? NightDifferentialSettingsJson { get; set; }
    public decimal? WeeklyOtThresholdHours { get; set; }

    public bool OTEnabled { get; set; } = false;
    public decimal OTThresholdHours { get; set; } = 0m;
    public bool OTApprovalRequired { get; set; } = true;
    public decimal? OTMaxHoursPerDay { get; set; }

    public LeaveFiscalYearStart LeaveFiscalYearStart { get; set; } = LeaveFiscalYearStart.January1;
    public LeaveBalanceResetRule LeaveBalanceResetRule { get; set; } = LeaveBalanceResetRule.FiscalYearStart;
    public bool LeaveProratedForNewHires { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [NotMapped]
    public List<OrgShiftTemplateDto> ShiftTemplates
    {
        get => DeserializeOrDefault(ShiftTemplatesJson, new List<OrgShiftTemplateDto>());
        set => ShiftTemplatesJson = SerializeOrDefault(value, new List<OrgShiftTemplateDto>());
    }

    [NotMapped]
    public OrgOvertimeSettingsDto OvertimeSettings
    {
        get => DeserializeOrDefault(OvertimeSettingsJson, new OrgOvertimeSettingsDto());
        set => OvertimeSettingsJson = SerializeOrDefault(value, new OrgOvertimeSettingsDto());
    }

    [NotMapped]
    public OrgNightDifferentialSettingsDto NightDifferentialSettings
    {
        get => DeserializeOrDefault(NightDifferentialSettingsJson, new OrgNightDifferentialSettingsDto());
        set => NightDifferentialSettingsJson = SerializeOrDefault(value, new OrgNightDifferentialSettingsDto());
    }

    private static T DeserializeOrDefault<T>(string? json, T fallback)
    {
        if (string.IsNullOrWhiteSpace(json))
            return fallback;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static string SerializeOrDefault<T>(T? value, T fallback)
    {
        var materialized = value ?? fallback;
        return JsonSerializer.Serialize(materialized, JsonOptions);
    }
}
