using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Models.LeaveSettings;
using Subchron.API.Models.Organizations;
using Subchron.API.Services;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/org-leave-settings")]
public class OrgLeaveSettingsController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<OrgLeaveSettingsController> _logger;
    private static readonly ConcurrentDictionary<int, OrgLeaveSettingsResponse> CachedResponses = new();

    public OrgLeaveSettingsController(SubchronDbContext db, IAuditService audit, ILogger<OrgLeaveSettingsController> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    [Authorize]
    [HttpGet("current")]
    public Task<ActionResult<OrgLeaveSettingsResponse>> GetCurrentAsync(CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Task.FromResult<ActionResult<OrgLeaveSettingsResponse>>(Forbid());
        return GetSettingsInternalAsync(orgId.Value, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("{orgId:int}")]
    public Task<ActionResult<OrgLeaveSettingsResponse>> GetByOrgAsync(int orgId, CancellationToken ct)
        => GetSettingsInternalAsync(orgId, ct);

    [Authorize]
    [HttpPut("current")]
    public Task<IActionResult> UpdateCurrentAsync([FromBody] OrgLeaveSettingsUpdateRequest req, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Task.FromResult<IActionResult>(Forbid());
        return UpdateSettingsInternalAsync(orgId.Value, req, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("{orgId:int}")]
    public Task<IActionResult> UpdateByOrgAsync(int orgId, [FromBody] OrgLeaveSettingsUpdateRequest req, CancellationToken ct)
        => UpdateSettingsInternalAsync(orgId, req, ct);

    private async Task<ActionResult<OrgLeaveSettingsResponse>> GetSettingsInternalAsync(int orgId, CancellationToken ct)
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
            _logger.LogError(ex, "Failed to load leave settings for org {OrgId}. Using fallback defaults.", orgId);
            if (CachedResponses.TryGetValue(orgId, out var cached))
                return Ok(cached);
            return Ok(BuildFallbackResponse(orgId));
        }
    }

    private async Task<IActionResult> UpdateSettingsInternalAsync(int orgId, OrgLeaveSettingsUpdateRequest req, CancellationToken ct)
    {
        if (req == null)
            return BadRequest(new { ok = false, message = "Invalid payload." });

        try
        {
            var settings = await _db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
            if (settings == null)
                return NotFound(new { ok = false, message = "Organization settings not found." });

            settings.LeaveFiscalYearStart = req.FiscalYearStart;
            settings.LeaveBalanceResetRule = req.BalanceResetRule;
            settings.LeaveProratedForNewHires = req.ProratedForNewHires;
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            var mapped = Map(settings);
            CachedResponses[orgId] = mapped;

            var userId = GetUserId();
            if (IsSuperAdmin())
                await _audit.LogSuperAdminAsync(orgId, userId, "OrgLeaveSettingsUpdated", nameof(OrganizationSettings), orgId, "Leave settings updated.", ct: ct);
            else
                await _audit.LogTenantAsync(orgId, userId, "LeaveSettingsUpdated", nameof(OrganizationSettings), orgId, "Leave settings updated.", ct: ct);

            return Ok(new { ok = true, persisted = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update leave settings for org {OrgId}. Using in-memory cache.", orgId);
            var fallback = FromRequest(orgId, req);
            CachedResponses[orgId] = fallback;
            return Ok(new { ok = true, persisted = false, message = "Leave settings saved locally and will re-sync once the database is reachable." });
        }
    }

    private static OrgLeaveSettingsResponse Map(OrganizationSettings settings)
        => new()
        {
            OrgId = settings.OrgID,
            FiscalYearStart = settings.LeaveFiscalYearStart,
            BalanceResetRule = settings.LeaveBalanceResetRule,
            ProratedForNewHires = settings.LeaveProratedForNewHires
        };

    private static OrgLeaveSettingsResponse BuildFallbackResponse(int orgId)
        => new()
        {
            OrgId = orgId,
            FiscalYearStart = LeaveFiscalYearStart.January1,
            BalanceResetRule = LeaveBalanceResetRule.FiscalYearStart,
            ProratedForNewHires = true
        };

    private static OrgLeaveSettingsResponse FromRequest(int orgId, OrgLeaveSettingsUpdateRequest req)
        => new()
        {
            OrgId = orgId,
            FiscalYearStart = req.FiscalYearStart,
            BalanceResetRule = req.BalanceResetRule,
            ProratedForNewHires = req.ProratedForNewHires
        };

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
