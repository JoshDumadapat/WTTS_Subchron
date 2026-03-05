using System.Security.Claims;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SubchronDbContext _db;
    private readonly IAuditService _audit;

    public OrgShiftSettingsController(SubchronDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
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

        var templates = OrgShiftSettingsValidator.NormalizeTemplates(Deserialize<List<OrgShiftTemplateDto>>(settings.ShiftTemplatesJson) ?? new List<OrgShiftTemplateDto>());
        var overtime = OrgShiftSettingsValidator.NormalizeOvertime(Deserialize<OrgOvertimeSettingsDto>(settings.OvertimeSettingsJson) ?? BuildLegacyOvertime(settings));
        var nightDifferential = OrgShiftSettingsValidator.NormalizeNightDifferential(Deserialize<OrgNightDifferentialSettingsDto>(settings.NightDifferentialSettingsJson) ?? new OrgNightDifferentialSettingsDto());

        return Ok(new OrgShiftSettingsResponse
        {
            Templates = templates,
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

        settings.ShiftTemplatesJson = Serialize(normalizedTemplates);
        settings.OvertimeSettingsJson = Serialize(normalizedOvertime);
        settings.NightDifferentialSettingsJson = Serialize(normalizedNightDifferential);
        settings.OTEnabled = normalizedOvertime.Enabled;
        settings.OTThresholdHours = normalizedOvertime.MinHoursBeforeOvertime;
        settings.OTApprovalRequired = normalizedOvertime.PreApprovalRequired && !normalizedOvertime.AutoApprove;
        settings.OTMaxHoursPerDay = normalizedOvertime.MaxHoursPerDay;
        settings.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(settings.DefaultShiftTemplateCode))
        {
            var hasMatch = normalizedTemplates.Any(t => string.Equals(t.Code, settings.DefaultShiftTemplateCode, StringComparison.OrdinalIgnoreCase));
            if (!hasMatch)
                settings.DefaultShiftTemplateCode = null;
        }

        await _db.SaveChangesAsync(ct);

        var userId = GetUserId();
        if (IsSuperAdmin())
            await _audit.LogSuperAdminAsync(orgId, userId, "OrgShiftSettingsUpdated", nameof(OrganizationSettings), orgId, "Shift and overtime configuration updated.", ct: ct);
        else
            await _audit.LogTenantAsync(orgId, userId, "OrgShiftSettingsUpdated", nameof(OrganizationSettings), orgId, "Shift and overtime configuration updated.", ct: ct);

        return Ok(new { ok = true, templates = normalizedTemplates.Count });
    }

    private static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, JsonOptions);

    private static OrgOvertimeSettingsDto BuildLegacyOvertime(OrganizationSettings settings)
    {
        return new OrgOvertimeSettingsDto
        {
            Enabled = settings.OTEnabled,
            MinHoursBeforeOvertime = settings.OTThresholdHours,
            PreApprovalRequired = settings.OTApprovalRequired,
            MaxHoursPerDay = settings.OTMaxHoursPerDay,
            Basis = "AfterShiftEnd",
            ApproverRole = "Supervisor",
            RoundToMinutes = 15,
            MinimumBlockMinutes = 0,
            DayTypes = new OrgOvertimeDayTypeRules(),
            BucketRules = new List<OrgOvertimeBucketRuleDto>(),
            ScopeRules = new List<OrgOvertimeScopeRuleDto>(),
            ApprovalSteps = new List<OrgOvertimeApprovalStepDto>()
        };
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
