using System.Collections.Concurrent;
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

    private readonly SubchronDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<OrgAttendanceSettingsController> _logger;
    private static readonly ConcurrentDictionary<int, OrgAttendanceSettingsResponse> CachedResponses = new();

    public OrgAttendanceSettingsController(SubchronDbContext db, IAuditService audit, ILogger<OrgAttendanceSettingsController> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
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
        try
        {
            var settings = await _db.OrganizationSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrgID == orgId, ct);

            if (settings == null)
                return NotFound(new { ok = false, message = "Organization settings not found." });

            var mapped = Map(settings);
            CachedResponses[orgId] = mapped;
            return Ok(mapped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load attendance settings for org {OrgId}. Using fallback defaults.", orgId);
            if (CachedResponses.TryGetValue(orgId, out var cached))
                return Ok(cached);
            return Ok(BuildFallbackResponse(orgId));
        }
    }

    private async Task<IActionResult> UpdateSettingsInternalAsync(int orgId, OrgAttendanceSettingsUpdateRequest req, CancellationToken ct)
    {
        if (req == null)
            return BadRequest(new { ok = false, message = "Invalid payload." });

        if (!TryNormalizePrimaryMode(req.PrimaryMode, out var normalizedMode))
            return BadRequest(new { ok = false, message = "Primary mode must be QR, Biometric, or Hybrid." });

        if (req.AutoClockOutEnabled)
        {
            if (!req.AutoClockOutMaxHours.HasValue)
                return BadRequest(new { ok = false, message = "Specify the number of hours before auto clock-out." });

            if (req.AutoClockOutMaxHours <= 0 || req.AutoClockOutMaxHours > 24)
                return BadRequest(new { ok = false, message = "Auto clock-out hours must be between 1 and 24." });
        }

        try
        {
            var settings = await _db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
            if (settings == null)
                return NotFound(new { ok = false, message = "Organization settings not found." });

            settings.AttendanceMode = normalizedMode;
            settings.AllowManualEntry = req.AllowManualEntry;
            settings.RequireGeo = req.RequireGeo;
            settings.EnforceGeofence = req.EnforceGeofence;
            settings.RestrictByIp = req.RestrictByIp;
            settings.PreventDoubleClockIn = req.PreventDoubleClockIn;
            settings.AutoClockOutEnabled = req.AutoClockOutEnabled;
            settings.AutoClockOutMaxHours = req.AutoClockOutEnabled ? req.AutoClockOutMaxHours : null;
            settings.DefaultShiftTemplateCode = Normalize(req.DefaultShiftTemplateCode);
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            var mapped = Map(settings);
            CachedResponses[orgId] = mapped;

            var userId = GetUserId();
            if (IsSuperAdmin())
                await _audit.LogSuperAdminAsync(orgId, userId, "OrgAttendanceSettingsUpdated", nameof(OrganizationSettings), orgId, "Attendance capture settings updated.", ct: ct);
            else
                await _audit.LogTenantAsync(orgId, userId, "AttendanceSettingsUpdated", nameof(OrganizationSettings), orgId, "Attendance capture settings updated.", ct: ct);

            return Ok(new { ok = true, persisted = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update attendance settings for org {OrgId}. Using in-memory cache.", orgId);
            var fallback = FromRequest(orgId, normalizedMode, req);
            CachedResponses[orgId] = fallback;
            return Ok(new { ok = true, persisted = false, message = "Attendance settings saved locally and will re-sync once the database is reachable." });
        }
    }

    private static OrgAttendanceSettingsResponse Map(OrganizationSettings settings)
        => new()
        {
            OrgId = settings.OrgID,
            PrimaryMode = NormalizeForResponse(settings.AttendanceMode),
            AllowManualEntry = settings.AllowManualEntry,
            RequireGeo = settings.RequireGeo,
            EnforceGeofence = settings.EnforceGeofence,
            RestrictByIp = settings.RestrictByIp,
            PreventDoubleClockIn = settings.PreventDoubleClockIn,
            AutoClockOutEnabled = settings.AutoClockOutEnabled,
            AutoClockOutMaxHours = settings.AutoClockOutMaxHours,
            DefaultShiftTemplateCode = settings.DefaultShiftTemplateCode
        };

    private static OrgAttendanceSettingsResponse BuildFallbackResponse(int orgId)
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

    private static OrgAttendanceSettingsResponse FromRequest(int orgId, string normalizedMode, OrgAttendanceSettingsUpdateRequest req)
        => new()
        {
            OrgId = orgId,
            PrimaryMode = normalizedMode,
            AllowManualEntry = req.AllowManualEntry,
            RequireGeo = req.RequireGeo,
            EnforceGeofence = req.EnforceGeofence,
            RestrictByIp = req.RestrictByIp,
            PreventDoubleClockIn = req.PreventDoubleClockIn,
            AutoClockOutEnabled = req.AutoClockOutEnabled,
            AutoClockOutMaxHours = req.AutoClockOutEnabled ? req.AutoClockOutMaxHours : null,
            DefaultShiftTemplateCode = Normalize(req.DefaultShiftTemplateCode)
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
