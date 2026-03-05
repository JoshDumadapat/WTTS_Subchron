using Subchron.API.Models.Organizations;
using Subchron.API.Services;

namespace Subchron.API.Tests;

public class AttendanceComputationServiceTests
{
    [Fact]
    public void ComputeDaily_ReturnsRequiredMinuteBuckets()
    {
        var svc = new AttendanceComputationService();
        var input = new AttendanceComputationInput
        {
            OrgId = 1,
            EmpId = 9,
            WorkDate = new DateOnly(2026, 3, 4),
            TimeIn = new DateTime(2026, 3, 4, 21, 0, 0, DateTimeKind.Utc),
            TimeOut = new DateTime(2026, 3, 5, 7, 0, 0, DateTimeKind.Utc),
            AttendanceDayType = "RegularDay",
            ShiftTemplate = new OrgShiftTemplateDto
            {
                Type = "Fixed",
                Fixed = new OrgShiftFixedSettings { StartTime = "21:00", EndTime = "06:00", BreakMinutes = 60, GraceMinutes = 0 }
            },
            OvertimeSettings = new OrgOvertimeSettingsDto { Enabled = true, MinHoursBeforeOvertime = 8, Basis = "AfterShiftEnd" },
            NightDifferentialSettings = new OrgNightDifferentialSettingsDto { Enabled = true, StartTime = "22:00", EndTime = "06:00", MinimumMinutes = 0 }
        };

        var result = svc.ComputeDaily(input);

        Assert.True(result.WorkedMinutes > 0);
        Assert.True(result.OvertimeMinutesByBucket.ContainsKey("RegularDay"));
        Assert.True(result.NightDifferentialMinutes > 0);
        Assert.Equal("RegularDay", result.DetectedAttendanceDayType);
        Assert.False(result.HardStopTriggered);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ComputeDaily_HardLimitBlocksExcess()
    {
        var svc = new AttendanceComputationService();
        var input = new AttendanceComputationInput
        {
            OrgId = 1,
            EmpId = 9,
            WorkDate = new DateOnly(2026, 3, 4),
            TimeIn = new DateTime(2026, 3, 4, 8, 0, 0, DateTimeKind.Utc),
            TimeOut = new DateTime(2026, 3, 4, 20, 0, 0, DateTimeKind.Utc),
            AttendanceDayType = "RegularDay",
            ShiftTemplate = new OrgShiftTemplateDto
            {
                Type = "Fixed",
                Fixed = new OrgShiftFixedSettings { StartTime = "08:00", EndTime = "17:00", BreakMinutes = 60, GraceMinutes = 0 }
            },
            OvertimeSettings = new OrgOvertimeSettingsDto
            {
                Enabled = true,
                MinHoursBeforeOvertime = 8,
                Basis = "AfterShiftEnd",
                MaxHoursPerDay = 1,
                LimitMode = "HARD"
            }
        };

        var result = svc.ComputeDaily(input);

        Assert.Equal(60, result.OvertimeMinutesByBucket["RegularDay"]);
        Assert.True(result.HardStopTriggered);
        Assert.Contains(result.Warnings, m => m.Contains("daily", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ComputeDaily_SoftLimitWarnsForWeekly()
    {
        var svc = new AttendanceComputationService();
        var input = new AttendanceComputationInput
        {
            OrgId = 1,
            EmpId = 9,
            WorkDate = new DateOnly(2026, 3, 6),
            TimeIn = new DateTime(2026, 3, 6, 8, 0, 0, DateTimeKind.Utc),
            TimeOut = new DateTime(2026, 3, 6, 20, 0, 0, DateTimeKind.Utc),
            AttendanceDayType = "RegularDay",
            ShiftTemplate = new OrgShiftTemplateDto
            {
                Type = "Fixed",
                Fixed = new OrgShiftFixedSettings { StartTime = "08:00", EndTime = "17:00", BreakMinutes = 60, GraceMinutes = 0 }
            },
            WeekToDateOvertimeMinutes = 180,
            OvertimeSettings = new OrgOvertimeSettingsDto
            {
                Enabled = true,
                MinHoursBeforeOvertime = 8,
                Basis = "AfterShiftEnd",
                MaxHoursPerWeek = 4,
                LimitMode = "SOFT",
                OverrideRole = "HR_MANAGER"
            }
        };

        var result = svc.ComputeDaily(input);

        Assert.Equal(60, result.OvertimeMinutesByBucket["RegularDay"]);
        Assert.False(result.HardStopTriggered);
        Assert.Contains(result.Warnings, m => m.Contains("weekly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, m => m.Contains("HR MANAGER", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ComputeDaily_NightDiffCountsCrossMidnightStarts()
    {
        var svc = new AttendanceComputationService();
        var input = new AttendanceComputationInput
        {
            OrgId = 1,
            EmpId = 2,
            WorkDate = new DateOnly(2026, 3, 5),
            TimeIn = new DateTime(2026, 3, 5, 1, 0, 0, DateTimeKind.Utc),
            TimeOut = new DateTime(2026, 3, 5, 5, 0, 0, DateTimeKind.Utc),
            AttendanceDayType = "RegularDay",
            ShiftTemplate = new OrgShiftTemplateDto
            {
                Type = "Fixed",
                Fixed = new OrgShiftFixedSettings { StartTime = "00:30", EndTime = "05:00", BreakMinutes = 0, GraceMinutes = 0 }
            },
            NightDifferentialSettings = new OrgNightDifferentialSettingsDto
            {
                Enabled = true,
                StartTime = "22:00",
                EndTime = "06:00",
                MinimumMinutes = 0
            }
        };

        var result = svc.ComputeDaily(input);

        Assert.Equal(240, result.NightDifferentialMinutes);
    }

    [Fact]
    public void ComputeDaily_NightDiffHandlesMultiDayShifts()
    {
        var svc = new AttendanceComputationService();
        var input = new AttendanceComputationInput
        {
            OrgId = 1,
            EmpId = 3,
            WorkDate = new DateOnly(2026, 3, 4),
            TimeIn = new DateTime(2026, 3, 4, 21, 0, 0, DateTimeKind.Utc),
            TimeOut = new DateTime(2026, 3, 6, 7, 0, 0, DateTimeKind.Utc),
            AttendanceDayType = "RegularDay",
            ShiftTemplate = new OrgShiftTemplateDto
            {
                Type = "Fixed",
                Fixed = new OrgShiftFixedSettings { StartTime = "21:00", EndTime = "07:00", BreakMinutes = 0, GraceMinutes = 0 }
            },
            NightDifferentialSettings = new OrgNightDifferentialSettingsDto
            {
                Enabled = true,
                StartTime = "22:00",
                EndTime = "06:00",
                MinimumMinutes = 0
            }
        };

        var result = svc.ComputeDaily(input);

        Assert.Equal(960, result.NightDifferentialMinutes);
    }

    [Fact]
    public void ComputeDaily_NightDiffHonorsConsecutiveMinuteMinimum()
    {
        var svc = new AttendanceComputationService();
        var input = new AttendanceComputationInput
        {
            OrgId = 1,
            EmpId = 4,
            WorkDate = new DateOnly(2026, 3, 4),
            TimeIn = new DateTime(2026, 3, 4, 22, 0, 0, DateTimeKind.Utc),
            TimeOut = new DateTime(2026, 3, 4, 22, 45, 0, DateTimeKind.Utc),
            AttendanceDayType = "RegularDay",
            ShiftTemplate = new OrgShiftTemplateDto
            {
                Type = "Fixed",
                Fixed = new OrgShiftFixedSettings { StartTime = "22:00", EndTime = "22:45", BreakMinutes = 0, GraceMinutes = 0 }
            },
            NightDifferentialSettings = new OrgNightDifferentialSettingsDto
            {
                Enabled = true,
                StartTime = "22:00",
                EndTime = "06:00",
                MinimumMinutes = 60
            }
        };

        var result = svc.ComputeDaily(input);

        Assert.Equal(0, result.NightDifferentialMinutes);
    }
}
