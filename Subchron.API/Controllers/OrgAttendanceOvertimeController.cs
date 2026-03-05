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
[Route("api/org-attendance-overtime")]
public class OrgAttendanceOvertimeController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<OrgAttendanceOvertimeController> _logger;

    public OrgAttendanceOvertimeController(SubchronDbContext db, IAuditService audit, ILogger<OrgAttendanceOvertimeController> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    [Authorize]
    [HttpGet("current")]
    public async Task<ActionResult<OrgAttendanceOvertimeDto>> GetCurrentAsync(CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();
        return await GetSettingsInternalAsync(orgId.Value, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("{orgId:int}")]
    public Task<ActionResult<OrgAttendanceOvertimeDto>> GetByOrgAsync(int orgId, CancellationToken ct)
        => GetSettingsInternalAsync(orgId, ct);

    [Authorize]
    [HttpPut("current")]
    public async Task<IActionResult> UpdateCurrentAsync([FromBody] OrgAttendanceOvertimeDto request, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();
        return await UpdateSettingsInternalAsync(orgId.Value, request, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("{orgId:int}")]
    public Task<IActionResult> UpdateByOrgAsync(int orgId, [FromBody] OrgAttendanceOvertimeDto request, CancellationToken ct)
        => UpdateSettingsInternalAsync(orgId, request, ct);

    private async Task<ActionResult<OrgAttendanceOvertimeDto>> GetSettingsInternalAsync(int orgId, CancellationToken ct)
    {
        var settings = await _db.OrganizationSettings.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
        if (settings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        return Ok(settings.AttendanceOvertimeSettings);
    }

    private async Task<IActionResult> UpdateSettingsInternalAsync(int orgId, OrgAttendanceOvertimeDto request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { ok = false, message = "Invalid payload." });

        OrgAttendanceOvertimeDto normalized;
        try
        {
            normalized = OrgAttendanceOvertimeValidator.Normalize(request);
        }
        catch (OrgAttendanceOvertimeValidationException ex)
        {
            return BadRequest(new { ok = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate overtime payload for org {OrgId}.", orgId);
            return BadRequest(new { ok = false, message = "Overtime rules payload is invalid." });
        }

        var settings = await _db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
        if (settings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        settings.AttendanceOvertimeSettings = normalized;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var userId = GetUserId();
        if (IsSuperAdmin())
        {
            await _audit.LogSuperAdminAsync(orgId, userId, "OrgAttendanceOvertimeUpdated", nameof(OrganizationSettings), orgId, "Attendance overtime rules updated.", ct: ct);
        }
        else
        {
            await _audit.LogTenantAsync(orgId, userId, "OrgAttendanceOvertimeUpdated", nameof(OrganizationSettings), orgId, "Attendance overtime rules updated.", ct: ct);
        }

        return Ok(new { ok = true, persisted = true });
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
