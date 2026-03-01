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
[Route("api/org-profile")]
public class OrgProfileController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly ICloudinaryService _cloudinary;
    private readonly IAuditService _audit;

    public OrgProfileController(SubchronDbContext db, ICloudinaryService cloudinary, IAuditService audit)
    {
        _db = db;
        _cloudinary = cloudinary;
        _audit = audit;
    }

    private int? GetUserOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    private bool IsSuperAdmin()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    [Authorize]
    [HttpGet("current")]
    public Task<ActionResult<OrgProfileResponse>> GetCurrentAsync(CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Task.FromResult<ActionResult<OrgProfileResponse>>(Forbid());
        return GetProfileAsync(orgId.Value, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("{orgId:int}")]
    public Task<ActionResult<OrgProfileResponse>> GetByOrgIdAsync(int orgId, CancellationToken ct) => GetProfileAsync(orgId, ct);

    private async Task<ActionResult<OrgProfileResponse>> GetProfileAsync(int orgId, CancellationToken ct)
    {
        var org = await _db.Organizations.AsNoTracking()
            .Include(o => o.Profile)
            .FirstOrDefaultAsync(o => o.OrgID == orgId, ct);

        if (org == null)
            return NotFound(new { ok = false, message = "Organization not found." });

        var profile = org.Profile;

        var primaryUserEmailRaw = await _db.Users.AsNoTracking()
            .Where(u => u.OrgID == orgId)
            .OrderBy(u => u.Role == UserRoleType.OrgAdmin ? 0 : 1)
            .ThenBy(u => u.UserID)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);

        var billingContact = await _db.BillingRecords.AsNoTracking()
            .Where(b => b.OrgID == orgId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BillingContactProjection
            {
                Email = b.BillingEmail,
                Phone = b.BillingPhone
            })
            .FirstOrDefaultAsync(ct);

        var primaryUserEmail = Normalize(primaryUserEmailRaw);
        var billingEmail = Normalize(billingContact?.Email);
        var billingPhone = Normalize(billingContact?.Phone);

        var contactEmail = Normalize(profile?.ContactEmail) ?? billingEmail ?? primaryUserEmail;
        var contactPhone = Normalize(profile?.ContactPhone) ?? billingPhone;

        return Ok(new OrgProfileResponse
        {
            OrgId = org.OrgID,
            OrgName = org.OrgName,
            LogoUrl = profile?.LogoUrl,
            AddressLine1 = profile?.AddressLine1,
            AddressLine2 = profile?.AddressLine2,
            City = profile?.City,
            StateProvince = profile?.StateProvince,
            PostalCode = profile?.PostalCode,
            Country = profile?.Country,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            PrimaryUserEmail = primaryUserEmail,
            BillingEmail = billingEmail,
            BillingPhone = billingPhone,
            UpdatedAt = profile?.UpdatedAt
        });
    }

    [Authorize]
    [HttpPut("current")]
    public Task<IActionResult> UpdateCurrentAsync([FromBody] OrgProfileUpdateRequest req, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Task.FromResult<IActionResult>(Forbid());
        return UpdateProfileInternalAsync(orgId.Value, req, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("{orgId:int}")]
    public Task<IActionResult> UpdateByOrgIdAsync(int orgId, [FromBody] OrgProfileUpdateRequest req, CancellationToken ct)
        => UpdateProfileInternalAsync(orgId, req, ct);

    private async Task<IActionResult> UpdateProfileInternalAsync(int orgId, OrgProfileUpdateRequest req, CancellationToken ct)
    {
        if (req == null)
            return BadRequest(new { ok = false, message = "Invalid payload." });

        var trimmedName = (req.OrgName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            return BadRequest(new { ok = false, message = "Organization name is required." });

        var org = await _db.Organizations
            .Include(o => o.Profile)
            .FirstOrDefaultAsync(o => o.OrgID == orgId, ct);

        if (org == null)
            return NotFound(new { ok = false, message = "Organization not found." });

        org.OrgName = trimmedName;

        var profile = org.Profile;
        if (profile == null)
        {
            profile = new OrganizationProfile { OrgID = org.OrgID };
            _db.OrganizationProfiles.Add(profile);
            org.Profile = profile;
        }

        profile.AddressLine1 = Normalize(req.AddressLine1);
        profile.AddressLine2 = Normalize(req.AddressLine2);
        profile.City = Normalize(req.City);
        profile.StateProvince = Normalize(req.StateProvince);
        profile.PostalCode = Normalize(req.PostalCode);
        profile.Country = Normalize(req.Country);
        profile.ContactEmail = Normalize(req.ContactEmail);
        profile.ContactPhone = Normalize(req.ContactPhone);
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _audit.LogSuperAdminAsync(orgId, GetUserId(), "OrgProfileUpdated", "OrganizationProfile", org.OrgID,
            details: "Organization profile updated.");

        return Ok(new { ok = true });
    }

    [Authorize]
    [HttpPost("current/logo")]
    public Task<IActionResult> UploadLogoForCurrentAsync([FromForm] IFormFile file, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Task.FromResult<IActionResult>(Forbid());
        return UploadLogoInternalAsync(orgId.Value, file, ct);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("{orgId:int}/logo")]
    public Task<IActionResult> UploadLogoForOrgAsync(int orgId, [FromForm] IFormFile file, CancellationToken ct)
        => UploadLogoInternalAsync(orgId, file, ct);

    private async Task<IActionResult> UploadLogoInternalAsync(int orgId, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { ok = false, message = "No file uploaded." });

        var contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();
        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/svg+xml" };
        if (!allowed.Contains(contentType))
            return BadRequest(new { ok = false, message = "Only JPEG, PNG, WEBP, or SVG files are allowed." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { ok = false, message = "File size must be 5 MB or less." });

        var org = await _db.Organizations.Include(o => o.Profile)
            .FirstOrDefaultAsync(o => o.OrgID == orgId, ct);
        if (org == null)
            return NotFound(new { ok = false, message = "Organization not found." });

        var folder = "organizations/logos";
        var publicId = $"org_{orgId}_{Guid.NewGuid():N}";
        string url;

        try
        {
            await using var stream = file.OpenReadStream();
            url = await _cloudinary.UploadImageAsync(stream, file.FileName ?? $"org-{orgId}-logo", folder, publicId, ct);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, message = "Upload failed.", detail = ex.Message });
        }

        var profile = org.Profile;
        if (profile == null)
        {
            profile = new OrganizationProfile { OrgID = org.OrgID };
            _db.OrganizationProfiles.Add(profile);
            org.Profile = profile;
        }

        profile.LogoUrl = url;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogSuperAdminAsync(orgId, GetUserId(), "OrgLogoUpdated", "OrganizationProfile", org.OrgID,
            details: "Organization logo updated.");

        return Ok(new { ok = true, logoUrl = url });
    }

    private sealed class BillingContactProjection
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
