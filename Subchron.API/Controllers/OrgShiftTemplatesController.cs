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
[Route("api/org-shift-templates")]
public class OrgShiftTemplatesController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly TenantDbContext _tenantDb;
    private readonly IAuditService _audit;

    public OrgShiftTemplatesController(SubchronDbContext db, TenantDbContext tenantDb, IAuditService audit)
    {
        _db = db;
        _tenantDb = tenantDb;
        _audit = audit;
    }

    [Authorize]
    [HttpGet("current")]
    public async Task<ActionResult<OrgShiftTemplateListResponse>> GetCurrentAsync(CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var settings = await _db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId.Value, ct);
        if (settings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        var templates = await LoadTemplateDtosAsync(orgId.Value, ct);

        return Ok(new OrgShiftTemplateListResponse
        {
            Templates = templates,
            DefaultShiftTemplateCode = settings.DefaultShiftTemplateCode
        });
    }

    [Authorize]
    [HttpPost("current")]
    public async Task<ActionResult<OrgShiftTemplateMutationResponse>> CreateAsync([FromBody] OrgShiftTemplateDto request, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        return await AddOrUpdateTemplateAsync(orgId.Value, request, null, ct, isUpdate: false);
    }

    [Authorize]
    [HttpPut("current/{code}")]
    public async Task<ActionResult<OrgShiftTemplateMutationResponse>> UpdateAsync(string code, [FromBody] OrgShiftTemplateDto request, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        return await AddOrUpdateTemplateAsync(orgId.Value, request, code, ct, isUpdate: true);
    }

    [Authorize]
    [HttpDelete("current/{code}")]
    public async Task<IActionResult> DeleteAsync(string code, CancellationToken ct)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var settings = await _db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrgID == orgId.Value, ct);
        if (settings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        var entity = await _tenantDb.OrgShiftTemplates
            .FirstOrDefaultAsync(t => t.OrgID == orgId.Value && t.Code == code, ct);

        if (entity == null)
            return NotFound(new { ok = false, message = "Shift template not found." });

        _tenantDb.OrgShiftTemplates.Remove(entity);
        await _tenantDb.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(settings.DefaultShiftTemplateCode) && string.Equals(settings.DefaultShiftTemplateCode, code, StringComparison.OrdinalIgnoreCase))
            settings.DefaultShiftTemplateCode = null;
        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await LogAuditAsync(orgId.Value, GetUserId(), "OrgShiftTemplateDeleted", $"Shift template '{code}' deleted.", ct);
        return Ok(new { ok = true });
    }

    private async Task<ActionResult<OrgShiftTemplateMutationResponse>> AddOrUpdateTemplateAsync(int orgId, OrgShiftTemplateDto request, string? code, CancellationToken ct, bool isUpdate)
    {
        if (request == null)
            return BadRequest(new { ok = false, message = "Invalid payload." });

        var settings = await _db.OrganizationSettings.FirstOrDefaultAsync(x => x.OrgID == orgId, ct);
        if (settings == null)
            return NotFound(new { ok = false, message = "Organization settings not found." });

        var existingTemplates = await LoadTemplateDtosAsync(orgId, ct);
        var normalizedExisting = OrgShiftSettingsValidator.NormalizeTemplates(existingTemplates);

        List<OrgShiftTemplateDto> updatedList;
        if (isUpdate)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { ok = false, message = "Template code is required." });

            var idx = normalizedExisting.FindIndex(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                return NotFound(new { ok = false, message = "Shift template not found." });

            request.Code = normalizedExisting[idx].Code;
            updatedList = new List<OrgShiftTemplateDto>(normalizedExisting)
            {
                [idx] = request
            };
        }
        else
        {
            updatedList = new List<OrgShiftTemplateDto>(normalizedExisting) { request };
        }

        List<OrgShiftTemplateDto> normalizedFinal;
        try
        {
            normalizedFinal = OrgShiftSettingsValidator.NormalizeTemplates(updatedList);
        }
        catch (ShiftSettingsValidationException ex)
        {
            return BadRequest(new { ok = false, message = ex.Message });
        }

        OrgShiftTemplateDto? target;
        if (isUpdate)
        {
            target = normalizedFinal.FirstOrDefault(t => string.Equals(t.Code, request.Code, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            var existingCodes = new HashSet<string>(normalizedExisting
                .Select(t => t.Code)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!), StringComparer.OrdinalIgnoreCase);
            target = normalizedFinal.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Code) && !existingCodes.Contains(t.Code!));
        }

        if (target == null)
            return StatusCode(500, new { ok = false, message = "Unable to determine saved template." });

        await PersistTemplateAsync(orgId, target, isUpdate, ct);

        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var action = isUpdate ? "OrgShiftTemplateUpdated" : "OrgShiftTemplateCreated";
        var message = isUpdate ? $"Shift template '{target.Name}' updated." : $"Shift template '{target.Name}' created.";
        await LogAuditAsync(orgId, GetUserId(), action, message, ct);

        return isUpdate
            ? Ok(new OrgShiftTemplateMutationResponse { Ok = true, Template = target })
            : StatusCode(StatusCodes.Status201Created, new OrgShiftTemplateMutationResponse { Ok = true, Template = target });
    }

    private async Task PersistTemplateAsync(int orgId, OrgShiftTemplateDto target, bool isUpdate, CancellationToken ct)
    {
        if (isUpdate)
        {
            var entity = await _tenantDb.OrgShiftTemplates
                .Include(t => t.WorkDays)
                .Include(t => t.Breaks)
                .Include(t => t.DayOverrides)
                    .ThenInclude(o => o.WorkWindows)
                .FirstOrDefaultAsync(t => t.OrgID == orgId && t.Code == target.Code, ct);

            if (entity == null)
                throw new InvalidOperationException("Shift template not found.");

            OrgShiftTemplateMapper.ApplyDto(entity, target);
        }
        else
        {
            var entity = OrgShiftTemplateMapper.CreateEntity(orgId, target);
            _tenantDb.OrgShiftTemplates.Add(entity);
        }

        await _tenantDb.SaveChangesAsync(ct);
    }

    private async Task<List<OrgShiftTemplateDto>> LoadTemplateDtosAsync(int orgId, CancellationToken ct)
    {
        var entities = await _tenantDb.OrgShiftTemplates
            .Include(t => t.WorkDays)
            .Include(t => t.Breaks)
            .Include(t => t.DayOverrides)
                .ThenInclude(o => o.WorkWindows)
            .Where(t => t.OrgID == orgId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return entities.Select(OrgShiftTemplateMapper.ToDto).ToList();
    }

    private async Task LogAuditAsync(int orgId, int? userId, string action, string description, CancellationToken ct)
    {
        if (IsSuperAdmin())
            await _audit.LogSuperAdminAsync(orgId, userId, action, nameof(OrganizationSettings), orgId, description, ct: ct);
        else
            await _audit.LogTenantAsync(orgId, userId, action, nameof(OrganizationSettings), orgId, description, ct: ct);
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
