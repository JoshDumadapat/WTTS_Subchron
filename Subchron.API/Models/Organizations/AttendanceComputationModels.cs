using System.Text.Json.Serialization;

namespace Subchron.API.Models.Organizations;

public class AttendanceComputationInput
{
    public int OrgId { get; set; }
    public int EmpId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime? TimeIn { get; set; }
    public DateTime? TimeOut { get; set; }
    public string AttendanceDayType { get; set; } = "RegularDay";
    public OrgShiftTemplateDto? ShiftTemplate { get; set; }
    public OrgOvertimeSettingsDto OvertimeSettings { get; set; } = new();
    public OrgNightDifferentialSettingsDto NightDifferentialSettings { get; set; } = new();
    public decimal? WeeklyThresholdHours { get; set; }
    public int WeekWorkedMinutes { get; set; }
    public int WeekToDateOvertimeMinutes { get; set; }
}

public class AttendanceComputationResult
{
    [JsonPropertyName("worked_minutes")]
    public int WorkedMinutes { get; set; }

    [JsonPropertyName("late_minutes")]
    public int LateMinutes { get; set; }

    [JsonPropertyName("undertime_minutes")]
    public int UndertimeMinutes { get; set; }

    [JsonPropertyName("overtime_minutes_by_bucket")]
    public Dictionary<string, int> OvertimeMinutesByBucket { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("night_differential_minutes")]
    public int NightDifferentialMinutes { get; set; }

    [JsonPropertyName("detected_attendance_day_type")]
    public string DetectedAttendanceDayType { get; set; } = "RegularDay";

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();

    [JsonPropertyName("hard_stop_triggered")]
    public bool HardStopTriggered { get; set; }
}

public class OvertimeApprovalEvaluationContext
{
    public int OrgId { get; set; }
    public int EmpId { get; set; }
    public string EmployeeRole { get; set; } = "Employee";
    public string EmploymentType { get; set; } = "Regular";
    public int? DepartmentId { get; set; }
    public string? SiteCode { get; set; }
    public OrgOvertimeSettingsDto OvertimeSettings { get; set; } = new();
    public string FilingMode { get; set; } = "AUTO";
}

public class OvertimeApprovalEvaluationResult
{
    public bool Eligible { get; set; }
    public string? RejectionReason { get; set; }
    public List<OrgOvertimeApprovalStepDto> Steps { get; set; } = new();
}
