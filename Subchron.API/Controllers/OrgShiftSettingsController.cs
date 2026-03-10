using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Models.Organizations;
using Subchron.API.Services;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/org-shift-settings")]
public class OrgShiftSettingsController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly TenantDbContext _tenantDb;
    private readonly IAuditService _audit;
    private readonly ILegacyOrgSettingsStore _store;

    public OrgShiftSettingsController(SubchronDbContext db, TenantDbContext tenantDb, IAuditService audit, ILegacyOrgSettingsStore store)
    {
        _db = db;
        _tenantDb = tenantDb;
        _audit = audit;
        _store = store;
    }

    [Authorize]
    [HttpGet("current")]
    public Task<ActionResult<OrgShiftSettingsResponse>> GetCurrentAsync(CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Task.FromResult<ActionResult<OrgShiftSettingsResponse>>(Forbid());
        return GetSettingsInternalAsync(orgId.Value, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("{orgId:int}")]
    public Task<ActionResult<OrgShiftSettingsResponse>> GetByOrgAsync(int orgId, CancellationToken ct)
        => GetSettingsInternalAsync(orgId, ct);

    [Authorize]
    [HttpPut("current")]
    public Task<IActionResult> UpdateCurrentAsync([FromBody] OrgShiftSettingsUpdateRequest req, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Task.FromResult<IActionResult>(Forbid());
        return UpdateSettingsInternalAsync(orgId.Value, req, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("{orgId:int}")]
    public Task<IActionResult> UpdateByOrgAsync(int orgId, [FromBody] OrgShiftSettingsUpdateRequest req, CancellationToken ct)
        => UpdateSettingsInternalAsync(orgId, req, ct);

    private async Task<ActionResult<OrgShiftSettingsResponse>> GetSettingsInternalAsync(int orgId, CancellationToken ct)
    {
        var settings = await _db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId, ct);

        if (settings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        var templates = await _tenantDb.OrgShiftTemplates
            .Include(t => t.WorkDays)
            .Include(t => t.Breaks)
            .Include(t => t.DayOverrides)
                .ThenInclude(o => o.WorkWindows)
            .Where(t => t.OrgID == orgId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var templateDtos = templates.Select(OrgShiftTemplateMapper.ToDto).ToList();

        var snapshot = _store.GetShiftSettings(orgId);
        var overtime = OrgShiftSettingsValidator.NormalizeOvertime(snapshot.Overtime);
        var nightDifferential = OrgShiftSettingsValidator.NormalizeNightDifferential(snapshot.NightDifferential);

        return Ok(new OrgShiftSettingsResponse
        {
            Templates = templateDtos,
            Overtime = overtime,
            NightDifferential = nightDifferential
        });
    }

    private async Task<IActionResult> UpdateSettingsInternalAsync(int orgId, OrgShiftSettingsUpdateRequest req, CancellationToken ct)
    {
        if (req == null)
            return BadRequest(new { ok = false, message = "Invalid payload." });

        var settings = await _db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
        if (settings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        List<OrgShiftTemplateDto> normalizedTemplates;
        OrgOvertimeSettingsDto normalizedOvertime;
        OrgNightDifferentialSettingsDto normalizedNightDifferential;

        try
        {
            normalizedTemplates = OrgShiftSettingsValidator.NormalizeTemplates(req.Templates);
            normalizedOvertime = OrgShiftSettingsValidator.NormalizeOvertime(req.Overtime);
            normalizedNightDifferential = OrgShiftSettingsValidator.NormalizeNightDifferential(req.NightDifferential);
        }
        catch (ShiftSettingsValidationException ex)
        {
            return BadRequest(new { ok = false, message = ex.Message });
        }

        await ReplaceTemplatesAsync(orgId, normalizedTemplates, ct);

        _store.SetShiftSettings(orgId, new OrgShiftSettingsSnapshot
        {
            Templates = normalizedTemplates,
            Overtime = normalizedOvertime,
            NightDifferential = normalizedNightDifferential
        });

        if (!string.IsNullOrWhiteSpace(settings.DefaultShiftTemplateCode))
        {
            var hasMatch = normalizedTemplates.Any(t => string.Equals(t.Code, settings.DefaultShiftTemplateCode, StringComparison.OrdinalIgnoreCase));
            if (!hasMatch)
                settings.DefaultShiftTemplateCode = null;
        }

        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var userId = GetUserId();
        if (IsSuperAdmin())
            await _audit.LogSuperAdminAsync(orgId, userId, "OrgShiftSettingsUpdated", nameof(OrganizationSettings), orgId, "Shift and overtime configuration updated.", ct: ct);
        else
            await _audit.LogTenantAsync(orgId, userId, "OrgShiftSettingsUpdated", nameof(OrganizationSettings), orgId, "Shift and overtime configuration updated.", ct: ct);

        return Ok(new { ok = true, templates = normalizedTemplates.Count });
    }

    private async Task ReplaceTemplatesAsync(int orgId, List<OrgShiftTemplateDto> templates, CancellationToken ct)
    {
        var existing = await _tenantDb.OrgShiftTemplates
            .Where(t => t.OrgID == orgId)
            .ToListAsync(ct);

        _tenantDb.OrgShiftTemplates.RemoveRange(existing);

        foreach (var dto in templates)
        {
            var entity = OrgShiftTemplateMapper.CreateEntity(orgId, dto);
            _tenantDb.OrgShiftTemplates.Add(entity);
        }

        await _tenantDb.SaveChangesAsync(ct);
    }

    private int? GetUserOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private bool IsSuperAdmin()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
}
