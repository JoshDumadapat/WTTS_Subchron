using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Controllers;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Models.Organizations;
using Subchron.API.Services;

namespace Subchron.API.Tests;

public class OrgShiftSettingsControllerTests
{
    [Fact]
    public async Task GetCurrent_ReturnsExpandedGraphIncludingNightDifferential()
    {
        await using var db = BuildDb();
        await using var tenantDb = BuildTenantDb();
        db.OrganizationSettings.Add(new OrganizationSettings { OrgID = 77 });
        await db.SaveChangesAsync();

        var store = new LegacyOrgSettingsStore();
        store.SetShiftSettings(77, new OrgShiftSettingsSnapshot
        {
            Templates = new List<OrgShiftTemplateDto>(),
            Overtime = new OrgOvertimeSettingsDto { Enabled = true },
            NightDifferential = new OrgNightDifferentialSettingsDto
            {
                Enabled = true,
                StartTime = "22:00",
                EndTime = "06:00",
                MinimumMinutes = 0
            }
        });

        var controller = BuildController(db, tenantDb, store, 77);
        var result = await controller.GetCurrentAsync(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<OrgShiftSettingsResponse>(ok.Value);

        Assert.NotNull(payload.Overtime);
        Assert.NotNull(payload.NightDifferential);
        Assert.Equal("22:00", payload.NightDifferential.StartTime);
    }

    [Fact]
    public async Task UpdateCurrent_PersistsNightDifferentialSeparately()
    {
        await using var db = BuildDb();
        await using var tenantDb = BuildTenantDb();
        db.OrganizationSettings.Add(new OrganizationSettings { OrgID = 88 });
        await db.SaveChangesAsync();

        var store = new LegacyOrgSettingsStore();
        var controller = BuildController(db, tenantDb, store, 88);
        var req = new OrgShiftSettingsUpdateRequest
        {
            Templates = new List<OrgShiftTemplateDto>(),
            Overtime = new OrgOvertimeSettingsDto { Enabled = true },
            NightDifferential = new OrgNightDifferentialSettingsDto
            {
                Enabled = true,
                StartTime = "22:00",
                EndTime = "06:00",
                MinimumMinutes = 15
            }
        };

        var action = await controller.UpdateCurrentAsync(req, CancellationToken.None);
        Assert.IsType<OkObjectResult>(action);
        var snapshot = store.GetShiftSettings(88);
        Assert.Equal(15, snapshot.NightDifferential.MinimumMinutes);
        Assert.True(snapshot.Overtime.Enabled);
        Assert.Empty(tenantDb.OrgShiftTemplates.Where(t => t.OrgID == 88));
    }

    private static SubchronDbContext BuildDb()
    {
        var options = new DbContextOptionsBuilder<SubchronDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new SubchronDbContext(options);
    }

    private static TenantDbContext BuildTenantDb()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new TenantDbContext(options);
    }

    private static OrgShiftSettingsController BuildController(SubchronDbContext db, TenantDbContext tenantDb, ILegacyOrgSettingsStore store, int orgId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("orgId", orgId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, "5"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

        var controller = new OrgShiftSettingsController(db, tenantDb, new NoopAuditService(), store)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };
        return controller;
    }

    private sealed class NoopAuditService : IAuditService
    {
        public Task LogTenantAsync(int orgId, int? userId, string action, string? entityName, int? entityId, string? details, string? ipAddress = null, string? userAgent = null, string? meta = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task LogSuperAdminAsync(int? orgId, int? userId, string action, string? entityName, int? entityId, string? details, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default) => Task.CompletedTask;
    }
}
