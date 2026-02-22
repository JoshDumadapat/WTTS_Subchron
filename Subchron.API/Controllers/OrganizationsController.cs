using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Organizations;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/organizations")]
public class OrganizationsController : ControllerBase
{
    private readonly SubchronDbContext _db;

    public OrganizationsController(SubchronDbContext db)
    {
        _db = db;
    }

    // Returns the display name of the authenticated user's organization (for sidebar).
    [Authorize]
    [HttpGet("current/name")]
    public async Task<IActionResult> GetCurrentOrgName()
    {
        var orgIdClaim = User.FindFirstValue("orgId");
        if (string.IsNullOrEmpty(orgIdClaim) || !int.TryParse(orgIdClaim, out var orgId))
            return Ok(new { orgName = (string?)null });

        var orgName = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.OrgID == orgId)
            .Select(o => o.OrgName)
            .FirstOrDefaultAsync();

        return Ok(new { orgName = orgName ?? (string?)null });
    }

    [Authorize]
    [HttpPut("{orgId:int}/settings")]
    public async Task<IActionResult> UpdateSettings(int orgId, [FromBody] OrganizationSettingsUpdateRequest req)
    {
        var userOrgIdClaim = User.FindFirstValue("orgId");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (role != "SuperAdmin" && !string.IsNullOrEmpty(userOrgIdClaim) && int.TryParse(userOrgIdClaim, out var userOrgId) && userOrgId != orgId)
            return Forbid();

        var settings = await _db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrgID == orgId);
        if (settings is null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        settings.Timezone = req.Timezone;
        settings.Currency = req.Currency;
        settings.AttendanceMode = req.AttendanceMode;

        settings.AllowManualEntry = req.AllowManualEntry;
        settings.RequireGeo = req.RequireGeo;
        settings.EnforceGeofence = req.EnforceGeofence;

        settings.DefaultGraceMinutes = req.DefaultGraceMinutes;
        settings.RoundRule = req.RoundRule;

        settings.OTEnabled = req.OTEnabled;
        settings.OTThresholdHours = req.OTThresholdHours;
        settings.OTApprovalRequired = req.OTApprovalRequired;
        settings.OTMaxHoursPerDay = req.OTMaxHoursPerDay;

        settings.LeaveEnabled = req.LeaveEnabled;
        settings.LeaveApprovalRequired = req.LeaveApprovalRequired;

        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
