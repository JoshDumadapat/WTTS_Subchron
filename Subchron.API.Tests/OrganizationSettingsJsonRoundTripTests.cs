using Subchron.API.Models.Entities;

namespace Subchron.API.Tests;

public class OrganizationSettingsJsonRoundTripTests
{
    [Fact]
    public void StoresOnlyCoreFieldsNow()
    {
        var entity = new OrganizationSettings
        {
            OrgID = 42,
            Timezone = "Asia/Singapore",
            Currency = "SGD",
            AttendanceMode = "Hybrid",
            DefaultShiftTemplateCode = "NIGHT-A",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        Assert.Equal(42, entity.OrgID);
        Assert.Equal("Asia/Singapore", entity.Timezone);
        Assert.Equal("SGD", entity.Currency);
        Assert.Equal("Hybrid", entity.AttendanceMode);
        Assert.Equal("NIGHT-A", entity.DefaultShiftTemplateCode);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), entity.CreatedAt);
        Assert.Equal(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), entity.UpdatedAt);
    }
}
