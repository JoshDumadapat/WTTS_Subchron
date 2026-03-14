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
[Authorize]
[Route("api/orgconfig/pay")]
public class OrgPayConfigController : ControllerBase
{
    private readonly TenantDbContext _tenantDb;
    private readonly IAuditService _audit;

    public OrgPayConfigController(TenantDbContext tenantDb, IAuditService audit)
    {
        _tenantDb = tenantDb;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<OrgPayConfigResponse>> GetAsync(CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue) return Forbid();

        var entity = await _tenantDb.OrgPayConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value, ct);
        if (entity == null)
        {
            entity = new OrgPayConfig { OrgID = orgId.Value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _tenantDb.OrgPayConfigs.Add(entity);
            await _tenantDb.SaveChangesAsync(ct);
        }

        return Ok(MapToResponse(entity));
    }

    [HttpPut]
    public async Task<IActionResult> UpsertAsync([FromBody] OrgPayConfigRequest request, CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue) return Forbid();

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var compensationBasis = (request.CompensationBasis ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(compensationBasis))
            return BadRequest(new { ok = false, message = "Compensation basis is required." });

        if (string.Equals(compensationBasis, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.CustomUnitLabel))
                return BadRequest(new { ok = false, message = "Custom unit label is required for custom compensation basis." });

            if (!request.CustomWorkHours.HasValue || request.CustomWorkHours.Value <= 0)
                return BadRequest(new { ok = false, message = "Custom work hours must be greater than zero for custom compensation basis." });
        }

        var entity = await _tenantDb.OrgPayConfigs.FirstOrDefaultAsync(x => x.OrgID == orgId.Value, ct);
        if (entity == null)
        {
            entity = new OrgPayConfig { OrgID = orgId.Value, CreatedAt = DateTime.UtcNow };
            _tenantDb.OrgPayConfigs.Add(entity);
        }

        entity.Currency = request.Currency;
        entity.PayCycle = request.PayCycle;
        entity.CompensationBasis = compensationBasis;
        entity.CustomUnitLabel = string.Equals(compensationBasis, "Custom", StringComparison.OrdinalIgnoreCase)
            ? (request.CustomUnitLabel ?? string.Empty).Trim()
            : string.Empty;
        entity.CustomWorkHours = string.Equals(compensationBasis, "Custom", StringComparison.OrdinalIgnoreCase)
            ? request.CustomWorkHours
            : null;
        entity.HoursPerDay = request.HoursPerDay;
        entity.CutoffWindowsJson = request.CutoffWindowsJson ?? "[]";
        entity.LockAttendanceAfterCutoff = request.LockAttendanceAfterCutoff;
        entity.ThirteenthMonthBasis = request.ThirteenthMonthBasis;
        entity.ThirteenthMonthNotes = request.ThirteenthMonthNotes ?? string.Empty;
        entity.EnableBIR = request.EnableBIR;
        entity.BIRPeriod = request.BIRPeriod;
        entity.BIRTableVersion = request.BIRTableVersion;
        entity.EnableSSS = request.EnableSSS;
        entity.SSSEmployerPercent = request.SSSEmployerPercent;
        entity.EnablePhilHealth = request.EnablePhilHealth;
        entity.PhilHealthRate = request.PhilHealthRate;
        entity.EnablePagIbig = request.EnablePagIbig;
        entity.PagIbigRate = request.PagIbigRate;
        entity.EnableIncomeTax = request.EnableIncomeTax;
        entity.ProrateNewHires = request.ProrateNewHires;
        entity.ApplyTaxThreshold = request.ApplyTaxThreshold;
        entity.UpdatedAt = DateTime.UtcNow;

        await _tenantDb.SaveChangesAsync(ct);
        await TryAuditTenantAsync(orgId.Value, GetUserId(), "OrgPayConfigUpdated", nameof(OrgPayConfig), orgId.Value,
            "Organization pay configuration updated.", ct);
        return Ok(new { ok = true });
    }

    private static OrgPayConfigResponse MapToResponse(OrgPayConfig entity) => new()
    {
        Currency = entity.Currency,
        PayCycle = entity.PayCycle,
        CompensationBasis = entity.CompensationBasis,
        CustomUnitLabel = entity.CustomUnitLabel,
        CustomWorkHours = entity.CustomWorkHours,
        HoursPerDay = entity.HoursPerDay,
        CutoffWindowsJson = entity.CutoffWindowsJson,
        LockAttendanceAfterCutoff = entity.LockAttendanceAfterCutoff,
        ThirteenthMonthBasis = entity.ThirteenthMonthBasis,
        ThirteenthMonthNotes = entity.ThirteenthMonthNotes,
        EnableBIR = entity.EnableBIR,
        BIRPeriod = entity.BIRPeriod,
        BIRTableVersion = entity.BIRTableVersion,
        EnableSSS = entity.EnableSSS,
        SSSEmployerPercent = entity.SSSEmployerPercent,
        EnablePhilHealth = entity.EnablePhilHealth,
        PhilHealthRate = entity.PhilHealthRate,
        EnablePagIbig = entity.EnablePagIbig,
        PagIbigRate = entity.PagIbigRate,
        EnableIncomeTax = entity.EnableIncomeTax,
        ProrateNewHires = entity.ProrateNewHires,
        ApplyTaxThreshold = entity.ApplyTaxThreshold
    };

    private int? GetOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private async Task TryAuditTenantAsync(int orgId, int? userId, string action, string? entityName, int? entityId, string? details, CancellationToken ct)
    {
        try
        {
            await _audit.LogTenantAsync(orgId, userId, action, entityName, entityId, details,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                ct: ct);
        }
        catch
        {
            // do not fail business flow on audit issues
        }
    }
}
