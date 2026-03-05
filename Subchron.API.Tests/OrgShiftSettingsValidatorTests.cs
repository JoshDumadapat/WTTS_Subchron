using Subchron.API.Models.Organizations;
using Subchron.API.Services;

namespace Subchron.API.Tests;

public class OrgShiftSettingsValidatorTests
{
    [Fact]
    public void NormalizeTemplates_RejectsOverlappingBreakWindows()
    {
        var input = new List<OrgShiftTemplateDto>
        {
            new()
            {
                Name = "Shift A",
                Type = "Fixed",
                Fixed = new OrgShiftFixedSettings { StartTime = "09:00", EndTime = "18:00", BreakMinutes = 0, GraceMinutes = 0 },
                Breaks = new List<OrgShiftBreakDto>
                {
                    new() { Name = "B1", StartTime = "12:00", EndTime = "13:00" },
                    new() { Name = "B2", StartTime = "12:30", EndTime = "13:30" }
                }
            }
        };

        var ex = Assert.Throws<ShiftSettingsValidationException>(() => OrgShiftSettingsValidator.NormalizeTemplates(input));
        Assert.Contains("overlap", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalizeOvertime_RejectsDuplicateApprovalOrder()
    {
        var overtime = new OrgOvertimeSettingsDto
        {
            Enabled = true,
            ApprovalSteps = new List<OrgOvertimeApprovalStepDto>
            {
                new() { Order = 1, Role = "Supervisor", Required = true },
                new() { Order = 1, Role = "Manager", Required = true }
            }
        };

        var ex = Assert.Throws<ShiftSettingsValidationException>(() => OrgShiftSettingsValidator.NormalizeOvertime(overtime));
        Assert.Contains("unique", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
