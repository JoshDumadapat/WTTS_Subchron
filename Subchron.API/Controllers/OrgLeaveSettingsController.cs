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
    private readonly TenantDbContext _tenantDb;
    private readonly IAuditService _audit;
    private readonly ILogger<OrgLeaveSettingsController> _logger;

    public OrgLeaveSettingsController(SubchronDbContext db, TenantDbContext tenantDb, IAuditService audit, ILogger<OrgLeaveSettingsController> logger)
    {
        _db = db;
        _tenantDb = tenantDb;
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
        var settings = await _db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId, ct);

        if (settings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        var config = await _tenantDb.OrgLeaveConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId, ct);

        if (config == null)
        {
            config = new OrgLeaveConfig { OrgID = orgId };
            _tenantDb.OrgLeaveConfigs.Add(config);
            await _tenantDb.SaveChangesAsync(ct);
        }

        return Ok(new OrgLeaveSettingsResponse
        {
            OrgId = orgId,
            FiscalYearStart = config.FiscalYearStart,
            BalanceResetRule = config.BalanceResetRule,
            ProratedForNewHires = config.ProratedForNewHires
        });
    }

    private async Task<IActionResult> UpdateSettingsInternalAsync(int orgId, OrgLeaveSettingsUpdateRequest req, CancellationToken ct)
    {
        if (req == null)
            return BadRequest(new { ok = false, message = "Invalid payload." });

        if (!Enum.IsDefined(typeof(LeaveFiscalYearStart), req.FiscalYearStart))
            return BadRequest(new { ok = false, message = "Invalid fiscal year start value." });
        if (!Enum.IsDefined(typeof(LeaveBalanceResetRule), req.BalanceResetRule))
            return BadRequest(new { ok = false, message = "Invalid balance reset rule value." });
        if (req.FiscalYearStart == LeaveFiscalYearStart.EmployeeHireDate && req.BalanceResetRule == LeaveBalanceResetRule.FiscalYearStart)
            return BadRequest(new { ok = false, message = "Fiscal year start reset cannot be used when fiscal year mode is Employee Hire Date." });

        try
        {
            var settings = await _db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
            if (settings == null)
                return NotFound(new { ok = false, message = "Organization settings not found." });

            var config = await _tenantDb.OrgLeaveConfigs.FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
            if (config == null)
            {
                config = new OrgLeaveConfig { OrgID = orgId };
                _tenantDb.OrgLeaveConfigs.Add(config);
            }

            config.FiscalYearStart = req.FiscalYearStart;
            config.BalanceResetRule = req.BalanceResetRule;
            config.ProratedForNewHires = req.ProratedForNewHires;
            config.UpdatedAt = DateTime.UtcNow;

            settings.UpdatedAt = DateTime.UtcNow;

            await _tenantDb.SaveChangesAsync(ct);
            await _db.SaveChangesAsync(ct);

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
            return StatusCode(StatusCodes.Status500InternalServerError, new { ok = false, message = "Unable to update leave settings right now." });
        }
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
