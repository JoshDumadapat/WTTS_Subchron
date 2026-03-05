using Subchron.API.Models.Entities;
using Subchron.API.Models.Organizations;

namespace Subchron.API.Tests;

public class OrganizationSettingsJsonRoundTripTests
{
    [Fact]
    public void TypedAccessors_RoundTripJsonPayloads()
    {
        var entity = new OrganizationSettings();

        entity.ShiftTemplates = new List<OrgShiftTemplateDto>
        {
            new()
            {
                Name = "Night Shift",
                Type = "Fixed",
                WorkDays = new List<string> { "Mon", "Tue" },
                Fixed = new OrgShiftFixedSettings { StartTime = "21:00", EndTime = "06:00", BreakMinutes = 60, GraceMinutes = 10 },
                Breaks = new List<OrgShiftBreakDto>
                {
                    new() { Name = "Meal", StartTime = "01:00", EndTime = "02:00", IsPaid = false }
                },
                DayOverrides = new List<OrgShiftDayOverrideDto>
                {
                    new() { Day = "Tue", IsOffDay = true }
                }
            }
        };

        entity.OvertimeSettings = new OrgOvertimeSettingsDto
        {
            Enabled = true,
            MinHoursBeforeOvertime = 8,
            BucketRules = new List<OrgOvertimeBucketRuleDto>
            {
                new() { BucketCode = "RegularDay", Enabled = true, ThresholdHours = 8, MaxHours = 4, MinimumBlockMinutes = 30 }
            }
        };

        entity.NightDifferentialSettings = new OrgNightDifferentialSettingsDto
        {
            Enabled = true,
            StartTime = "22:00",
            EndTime = "06:00",
            MinimumMinutes = 30
        };

        Assert.Contains("night shift", entity.ShiftTemplatesJson!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bucketRules", entity.OvertimeSettingsJson!, StringComparison.Ordinal);
        Assert.Contains("startTime", entity.NightDifferentialSettingsJson!, StringComparison.Ordinal);

        var loadedTemplates = entity.ShiftTemplates;
        var loadedOvertime = entity.OvertimeSettings;
        var loadedNd = entity.NightDifferentialSettings;

        Assert.Single(loadedTemplates);
        Assert.Equal("Night Shift", loadedTemplates[0].Name);
        Assert.Single(loadedTemplates[0].Breaks);
        Assert.Single(loadedOvertime.BucketRules);
        Assert.Equal("22:00", loadedNd.StartTime);
    }
}
