using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Subchron.API.Models.Organizations;

namespace Subchron.API.Services;

public interface ILegacyOrgSettingsStore
{
    OrgAttendanceSettingsResponse GetAttendanceSettings(int orgId);
    void SetAttendanceSettings(int orgId, OrgAttendanceSettingsResponse response);

    OrgShiftSettingsSnapshot GetShiftSettings(int orgId);
    void SetShiftSettings(int orgId, OrgShiftSettingsSnapshot snapshot);

    OrgAttendanceOvertimeDto GetAttendanceOvertimeSettings(int orgId);
    void SetAttendanceOvertimeSettings(int orgId, OrgAttendanceOvertimeDto dto);
}

public sealed class LegacyOrgSettingsStore : ILegacyOrgSettingsStore
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ConcurrentDictionary<int, OrgAttendanceSettingsResponse> _attendance = new();
    private readonly ConcurrentDictionary<int, OrgShiftSettingsSnapshot> _shift = new();
    private readonly ConcurrentDictionary<int, OrgAttendanceOvertimeDto> _attendanceOvertime = new();

    public OrgAttendanceSettingsResponse GetAttendanceSettings(int orgId)
    {
        var snapshot = _attendance.GetOrAdd(orgId, BuildDefaultAttendance);
        return Clone(snapshot);
    }

    public void SetAttendanceSettings(int orgId, OrgAttendanceSettingsResponse response)
    {
        response.OrgId = orgId;
        _attendance[orgId] = Clone(response);
    }

    public OrgShiftSettingsSnapshot GetShiftSettings(int orgId)
    {
        var snapshot = _shift.GetOrAdd(orgId, _ => new OrgShiftSettingsSnapshot());
        return Clone(snapshot);
    }

    public void SetShiftSettings(int orgId, OrgShiftSettingsSnapshot snapshot)
    {
        _shift[orgId] = Clone(snapshot);
    }

    public OrgAttendanceOvertimeDto GetAttendanceOvertimeSettings(int orgId)
    {
        var dto = _attendanceOvertime.GetOrAdd(orgId, _ => OrgAttendanceOvertimeDefaults.BuildSettings());
        return Clone(dto);
    }

    public void SetAttendanceOvertimeSettings(int orgId, OrgAttendanceOvertimeDto dto)
    {
        _attendanceOvertime[orgId] = Clone(dto);
    }

    private static OrgAttendanceSettingsResponse BuildDefaultAttendance(int orgId)
        => new()
        {
            OrgId = orgId,
            PrimaryMode = "QR",
            AllowManualEntry = false,
            RequireGeo = false,
            EnforceGeofence = false,
            RestrictByIp = false,
            PreventDoubleClockIn = true,
            AutoClockOutEnabled = false,
            AutoClockOutMaxHours = null,
            DefaultShiftTemplateCode = null
        };

    private static T Clone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, CloneOptions);
        return JsonSerializer.Deserialize<T>(json, CloneOptions)!;
    }
}

public class OrgShiftSettingsSnapshot
{
    public List<OrgShiftTemplateDto> Templates { get; set; } = new();
    public OrgOvertimeSettingsDto Overtime { get; set; } = new();
    public OrgNightDifferentialSettingsDto NightDifferential { get; set; } = new();
}
