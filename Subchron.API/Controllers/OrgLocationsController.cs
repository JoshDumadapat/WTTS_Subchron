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
[Route("api/org-locations")]
[Authorize]
public class OrgLocationsController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly IAuditService _audit;

    public OrgLocationsController(TenantDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
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

    [HttpGet("current")]
    public async Task<ActionResult<List<OrgLocationResponse>>> GetForCurrentOrg(CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var sites = await _db.Locations.AsNoTracking()
            .Where(l => l.OrgID == orgId.Value)
            .OrderByDescending(l => l.IsActive)
            .ThenBy(l => l.LocationName)
            .Select(l => new OrgLocationResponse
            {
                LocationId = l.LocationID,
                LocationName = l.LocationName,
                Latitude = l.GeoLat,
                Longitude = l.GeoLong,
                RadiusMeters = l.RadiusMeters,
                IsActive = l.IsActive,
                DeactivationReason = l.DeactivationReason,
                PinColor = l.PinColor
            })
            .ToListAsync(ct);

        return Ok(sites);
    }

    [HttpPost("current")]
    public async Task<ActionResult<OrgLocationResponse>> AddForCurrentOrg([FromBody] OrgLocationCreateRequest req, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        if (!TryValidateRequest(req, out var normalizedName, out var errMessage))
            return BadRequest(new { ok = false, message = errMessage });

        var exists = await _db.Locations.AnyAsync(l => l.OrgID == orgId.Value && l.LocationName.ToLower() == normalizedName.ToLower(), ct);
        if (exists)
            return Conflict(new { ok = false, message = "A site with this name already exists." });

        var location = new Location
        {
            OrgID = orgId.Value,
            LocationName = normalizedName,
            GeoLat = Math.Round(req.Latitude, 6),
            GeoLong = Math.Round(req.Longitude, 6),
            RadiusMeters = req.RadiusMeters,
            IsActive = true,
            PinColor = (req.PinColor ?? "emerald").Trim().ToLowerInvariant()
        };

        _db.Locations.Add(location);
        await _db.SaveChangesAsync(ct);

        await _audit.LogTenantAsync(orgId.Value, userId, "LocationCreated", nameof(Location), location.LocationID,
            details: $"Added site '{location.LocationName}'.", ct: ct);

        var response = new OrgLocationResponse
        {
            LocationId = location.LocationID,
            LocationName = location.LocationName,
            Latitude = location.GeoLat,
            Longitude = location.GeoLong,
            RadiusMeters = location.RadiusMeters,
            IsActive = location.IsActive,
            DeactivationReason = location.DeactivationReason,
            PinColor = location.PinColor
        };

        return Ok(response);
    }

    [HttpPut("current/{locationId:int}")]
    public async Task<ActionResult<OrgLocationResponse>> UpdateForCurrentOrg(int locationId, [FromBody] OrgLocationUpdateRequest req, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        if (!TryValidateRequest(req, out var normalizedName, out var errMessage))
            return BadRequest(new { ok = false, message = errMessage });

        var location = await _db.Locations.FirstOrDefaultAsync(l => l.LocationID == locationId && l.OrgID == orgId.Value, ct);
        if (location == null)
            return NotFound(new { ok = false, message = "Location not found." });

        var exists = await _db.Locations.AnyAsync(l => l.OrgID == orgId.Value && l.LocationID != locationId && l.LocationName.ToLower() == normalizedName.ToLower(), ct);
        if (exists)
            return Conflict(new { ok = false, message = "A site with this name already exists." });

        location.LocationName = normalizedName;
        location.GeoLat = Math.Round(req.Latitude, 6);
        location.GeoLong = Math.Round(req.Longitude, 6);
        location.RadiusMeters = req.RadiusMeters;
        location.PinColor = (req.PinColor ?? location.PinColor).Trim().ToLowerInvariant();

        await _db.SaveChangesAsync(ct);

        await _audit.LogTenantAsync(orgId.Value, userId, "LocationUpdated", nameof(Location), location.LocationID,
            details: $"Updated site '{location.LocationName}'.", ct: ct);

        return Ok(new OrgLocationResponse
        {
            LocationId = location.LocationID,
            LocationName = location.LocationName,
            Latitude = location.GeoLat,
            Longitude = location.GeoLong,
            RadiusMeters = location.RadiusMeters,
            IsActive = location.IsActive,
            DeactivationReason = location.DeactivationReason,
            PinColor = location.PinColor
        });
    }

    [HttpPatch("current/{locationId:int}/status")]
    public async Task<ActionResult<OrgLocationResponse>> UpdateStatusForCurrentOrg(int locationId, [FromBody] OrgLocationStatusRequest req, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var location = await _db.Locations.FirstOrDefaultAsync(l => l.LocationID == locationId && l.OrgID == orgId.Value, ct);
        if (location == null)
            return NotFound(new { ok = false, message = "Location not found." });

        var makeActive = req?.IsActive ?? false;
        string? trimmedReason = (req?.Reason ?? string.Empty).Trim();

        if (!makeActive)
        {
            if (string.IsNullOrWhiteSpace(trimmedReason))
                return BadRequest(new { ok = false, message = "Provide a reason before deactivating a site." });
            if (trimmedReason.Length > 200)
                trimmedReason = trimmedReason[..200];
            location.IsActive = false;
            location.DeactivationReason = trimmedReason;
        }
        else
        {
            location.IsActive = true;
            location.DeactivationReason = null;
        }

        await _db.SaveChangesAsync(ct);

        var action = location.IsActive ? "LocationActivated" : "LocationDeactivated";
        await _audit.LogTenantAsync(orgId.Value, userId, action, nameof(Location), location.LocationID,
            details: location.IsActive ? "Site reactivated." : location.DeactivationReason, ct: ct);

        return Ok(new OrgLocationResponse
        {
            LocationId = location.LocationID,
            LocationName = location.LocationName,
            Latitude = location.GeoLat,
            Longitude = location.GeoLong,
            RadiusMeters = location.RadiusMeters,
            IsActive = location.IsActive,
            DeactivationReason = location.DeactivationReason
        });
    }

    private static bool TryValidateRequest(OrgLocationCreateRequest? req, out string normalizedName, out string error)
        => TryValidateShared(req?.LocationName, req?.Latitude, req?.Longitude, req?.RadiusMeters, req?.PinColor, out normalizedName, out error);

    private static bool TryValidateRequest(OrgLocationUpdateRequest? req, out string normalizedName, out string error)
        => TryValidateShared(req?.LocationName, req?.Latitude, req?.Longitude, req?.RadiusMeters, req?.PinColor, out normalizedName, out error);

    private static bool TryValidateShared(string? nameRaw, decimal? lat, decimal? lng, int? radius, string? pinColor, out string normalizedName, out string error)
    {
        normalizedName = string.Empty;
        error = string.Empty;
        var name = (nameRaw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Location name is required.";
            return false;
        }

        if (name.Length > 50)
            name = name[..50];

        if (!lat.HasValue || !lng.HasValue || lat < -90 || lat > 90 || lng < -180 || lng > 180)
        {
            error = "Invalid coordinates.";
            return false;
        }

        var radiusMeters = radius ?? 0;
        if (radiusMeters < 10 || radiusMeters > 5000)
        {
            error = "Radius must be between 10 and 5000 meters.";
            return false;
        }

        normalizedName = name;
        if (pinColor != null)
        {
            var trimmed = pinColor.Trim().ToLowerInvariant();
            if (!AllowedPinColors.Contains(trimmed))
            {
                error = "Invalid pin color.";
                return false;
            }
        }
        return true;
    }

    private static readonly HashSet<string> AllowedPinColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "blue", "purple", "pink", "cyan", "emerald"
    };
}
