using System.Collections.Generic;
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
[Route("api/org-attendance-settings")]
public class OrgAttendanceSettingsController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string> AllowedModes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["qr"] = "QR",
        ["qr-code"] = "QR",
        ["qr/kiosk"] = "QR",
        ["kiosk"] = "QR",
        ["bio"] = "Biometric",
        ["biometric"] = "Biometric",
        ["bio+geo"] = "Biometric",
        ["biogeo"] = "Biometric",
        ["hybrid"] = "Hybrid"
    };

    private static readonly HashSet<string> AllowedManualEntryModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN",
        "SUPERVISOR"
    };

    private static readonly HashSet<string> AllowedMissingActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "IGNORE",
        "EXCEPTION",
        "CORRECTION"
    };

    private readonly SubchronDbContext _db;
    private readonly TenantDbContext _tenantDb;
    private readonly IAuditService _audit;

    public OrgAttendanceSettingsController(SubchronDbContext db, TenantDbContext tenantDb, IAuditService audit)
    {
        _db = db;
        _tenantDb = tenantDb;
        _audit = audit;
    }

    [Authorize]
    [HttpGet("current")]
    public Task<ActionResult<OrgAttendanceSettingsResponse>> GetCurrentAsync(CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Task.FromResult<ActionResult<OrgAttendanceSettingsResponse>>(Forbid());
        return GetSettingsInternalAsync(orgId.Value, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("{orgId:int}")]
    public Task<ActionResult<OrgAttendanceSettingsResponse>> GetByOrgAsync(int orgId, CancellationToken ct)
        => GetSettingsInternalAsync(orgId, ct);

    [Authorize]
    [HttpPut("current")]
    public Task<IActionResult> UpdateCurrentAsync([FromBody] OrgAttendanceSettingsUpdateRequest req, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Task.FromResult<IActionResult>(Forbid());
        return UpdateSettingsInternalAsync(orgId.Value, req, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("{orgId:int}")]
    public Task<IActionResult> UpdateByOrgAsync(int orgId, [FromBody] OrgAttendanceSettingsUpdateRequest req, CancellationToken ct)
        => UpdateSettingsInternalAsync(orgId, req, ct);

    private async Task<ActionResult<OrgAttendanceSettingsResponse>> GetSettingsInternalAsync(int orgId, CancellationToken ct)
    {
        var orgSettings = await _db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId, ct);

        if (orgSettings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        var config = await _tenantDb.OrgAttendanceConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId, ct);

        if (config == null)
        {
            config = new OrgAttendanceConfig
            {
                OrgID = orgId,
                PrimaryMode = NormalizeForResponse(orgSettings.AttendanceMode),
                DefaultShiftTemplateCode = orgSettings.DefaultShiftTemplateCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _tenantDb.OrgAttendanceConfigs.Add(config);
            await _tenantDb.SaveChangesAsync(ct);
        }

        return Ok(MapToResponse(config, orgSettings));
    }

    private async Task<IActionResult> UpdateSettingsInternalAsync(int orgId, OrgAttendanceSettingsUpdateRequest req, CancellationToken ct)
    {
        if (req == null)
            return BadRequest(new { ok = false, message = "Invalid payload." });

        if (!TryNormalizePrimaryMode(req.PrimaryMode, out var normalizedMode))
            return BadRequest(new { ok = false, message = "Primary mode must be QR, Biometric, or Hybrid." });

        if (!TryNormalizeManualEntryMode(req.ManualEntryAccessMode, out var normalizedManualMode))
            return BadRequest(new { ok = false, message = "Manual entry access mode must be Admin or Supervisor." });

        if (!TryNormalizeMissingAction(req.DefaultMissingPunchAction, out var normalizedMissingAction))
            return BadRequest(new { ok = false, message = "Invalid default action for missing punches." });

        if (req.EarliestClockInMinutes.HasValue && (req.EarliestClockInMinutes < 0 || req.EarliestClockInMinutes > 720))
            return BadRequest(new { ok = false, message = "Earliest clock-in must be between 0 and 720 minutes." });

        if (req.LatestClockInMinutes.HasValue && (req.LatestClockInMinutes < 0 || req.LatestClockInMinutes > 720))
            return BadRequest(new { ok = false, message = "Latest clock-in grace must be between 0 and 720 minutes." });

        if (req.AutoClockOutEnabled)
        {
            if (!req.AutoClockOutMaxHours.HasValue)
                return BadRequest(new { ok = false, message = "Specify the number of hours before auto clock-out." });

            if (req.AutoClockOutMaxHours <= 0 || req.AutoClockOutMaxHours > 24)
                return BadRequest(new { ok = false, message = "Auto clock-out hours must be between 1 and 24." });
        }

        var orgSettings = await _db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
        if (orgSettings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        var config = await _tenantDb.OrgAttendanceConfigs.FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
        if (config == null)
        {
            config = new OrgAttendanceConfig { OrgID = orgId };
            _tenantDb.OrgAttendanceConfigs.Add(config);
        }

        config.PrimaryMode = normalizedMode;
        config.AllowManualEntry = req.AllowManualEntry;
        config.ManualEntryAccessMode = normalizedManualMode;
        config.RequireGeo = req.RequireGeo;
        config.EnforceGeofence = req.EnforceGeofence;
        config.RestrictByIp = req.RestrictByIp;
        config.PreventDoubleClockIn = req.PreventDoubleClockIn;
        config.EarliestClockInMinutes = req.EarliestClockInMinutes;
        config.LatestClockInMinutes = req.LatestClockInMinutes;
        config.AllowIncompleteLogs = req.AllowIncompleteLogs;
        config.AutoFlagMissingPunch = req.AutoFlagMissingPunch;
        config.DefaultMissingPunchAction = normalizedMissingAction;
        config.UseGracePeriodForLate = req.UseGracePeriodForLate;
        config.MarkUndertimeBasedOnSchedule = req.MarkUndertimeBasedOnSchedule;
        config.AutoAbsentWithoutLog = req.AutoAbsentWithoutLog;
        config.AutoClockOutEnabled = req.AutoClockOutEnabled;
        config.AutoClockOutMaxHours = req.AutoClockOutEnabled ? req.AutoClockOutMaxHours : null;
        config.DefaultShiftTemplateCode = Normalize(req.DefaultShiftTemplateCode);
        config.UpdatedAt = DateTime.UtcNow;

        orgSettings.AttendanceMode = normalizedMode;
        orgSettings.DefaultShiftTemplateCode = config.DefaultShiftTemplateCode;
        orgSettings.UpdatedAt = DateTime.UtcNow;

        await _tenantDb.SaveChangesAsync(ct);
        await _db.SaveChangesAsync(ct);

        var userId = GetUserId();
        if (IsSuperAdmin())
            await _audit.LogSuperAdminAsync(orgId, userId, "OrgAttendanceSettingsUpdated", nameof(OrganizationSettings), orgId, "Attendance capture settings updated.", ct: ct);
        else
            await _audit.LogTenantAsync(orgId, userId, "AttendanceSettingsUpdated", nameof(OrganizationSettings), orgId, "Attendance capture settings updated.", ct: ct);

        return Ok(new { ok = true, persisted = true });
    }

    private static OrgAttendanceSettingsResponse MapToResponse(OrgAttendanceConfig config, OrganizationSettings orgSettings)
        => new()
        {
            OrgId = config.OrgID,
            PrimaryMode = NormalizeForResponse(config.PrimaryMode ?? orgSettings.AttendanceMode),
            AllowManualEntry = config.AllowManualEntry,
            ManualEntryAccessMode = NormalizeManualModeForResponse(config.ManualEntryAccessMode),
            RequireGeo = config.RequireGeo,
            EnforceGeofence = config.EnforceGeofence,
            RestrictByIp = config.RestrictByIp,
            PreventDoubleClockIn = config.PreventDoubleClockIn,
            EarliestClockInMinutes = config.EarliestClockInMinutes,
            LatestClockInMinutes = config.LatestClockInMinutes,
            AllowIncompleteLogs = config.AllowIncompleteLogs,
            AutoFlagMissingPunch = config.AutoFlagMissingPunch,
            DefaultMissingPunchAction = NormalizeMissingActionForResponse(config.DefaultMissingPunchAction),
            UseGracePeriodForLate = config.UseGracePeriodForLate,
            MarkUndertimeBasedOnSchedule = config.MarkUndertimeBasedOnSchedule,
            AutoAbsentWithoutLog = config.AutoAbsentWithoutLog,
            AutoClockOutEnabled = config.AutoClockOutEnabled,
            AutoClockOutMaxHours = config.AutoClockOutMaxHours,
            DefaultShiftTemplateCode = config.DefaultShiftTemplateCode ?? orgSettings.DefaultShiftTemplateCode
        };

    private static bool TryNormalizePrimaryMode(string? value, out string normalized)
    {
        normalized = "QR";
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        if (AllowedModes.TryGetValue(trimmed, out var mapped))
        {
            normalized = mapped;
            return true;
        }

        return false;
    }

    private static string NormalizeForResponse(string? value)
    {
        return TryNormalizePrimaryMode(value, out var normalized) ? normalized : "QR";
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryNormalizeManualEntryMode(string? value, out string normalized)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? "SUPERVISOR" : value.Trim();
        var upper = candidate.ToUpperInvariant();
        if (AllowedManualEntryModes.Contains(upper))
        {
            normalized = upper;
            return true;
        }
        normalized = "SUPERVISOR";
        return false;
    }

    private static string NormalizeManualModeForResponse(string? value)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? "SUPERVISOR" : value.Trim();
        var upper = candidate.ToUpperInvariant();
        return AllowedManualEntryModes.Contains(upper) ? upper : "SUPERVISOR";
    }

    private static bool TryNormalizeMissingAction(string? value, out string normalized)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? "IGNORE" : value.Trim();
        var upper = candidate.ToUpperInvariant();
        if (AllowedMissingActions.Contains(upper))
        {
            normalized = upper;
            return true;
        }
        normalized = "IGNORE";
        return false;
    }

    private static string NormalizeMissingActionForResponse(string? value)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? "IGNORE" : value.Trim();
        var upper = candidate.ToUpperInvariant();
        return AllowedMissingActions.Contains(upper) ? upper : "IGNORE";
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
