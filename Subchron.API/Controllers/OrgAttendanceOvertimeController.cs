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
    private readonly TenantDbContext _tenantDb;
    private readonly IAuditService _audit;
    private readonly ILogger<OrgAttendanceOvertimeController> _logger;

    public OrgAttendanceOvertimeController(SubchronDbContext db, TenantDbContext tenantDb, IAuditService audit, ILogger<OrgAttendanceOvertimeController> logger)
    {
        _db = db;
        _tenantDb = tenantDb;
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

        var policy = await _tenantDb.OrgAttendanceOvertimePolicies
            .Include(p => p.Buckets)
            .Include(p => p.ApprovalSteps)
            .Include(p => p.ScopeFilters)
            .Include(p => p.NightDiffExclusions)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrgID == orgId, ct);

        if (policy == null)
            return Ok(OrgAttendanceOvertimeDefaults.BuildSettings());

        return Ok(MapToDto(policy));
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

        var policy = await _tenantDb.OrgAttendanceOvertimePolicies
            .Include(p => p.Buckets)
            .Include(p => p.ApprovalSteps)
            .Include(p => p.ScopeFilters)
            .Include(p => p.NightDiffExclusions)
            .FirstOrDefaultAsync(p => p.OrgID == orgId, ct);

        if (policy == null)
        {
            policy = new OrgAttendanceOvertimePolicy { OrgID = orgId };
            _tenantDb.OrgAttendanceOvertimePolicies.Add(policy);
        }

        ApplyDtoToPolicy(policy, normalized);
        policy.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedAt = DateTime.UtcNow;

        await _tenantDb.SaveChangesAsync(ct);
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

    private static OrgAttendanceOvertimeDto MapToDto(OrgAttendanceOvertimePolicy policy)
    {
        var dto = new OrgAttendanceOvertimeDto
        {
            Enabled = policy.Enabled,
            Basis = policy.Basis,
            RestHolidayOverride = policy.RestHolidayOverride,
            DailyThresholdHours = policy.DailyThresholdHours,
            WeeklyThresholdHours = policy.WeeklyThresholdHours,
            EarlyOtAllowed = policy.EarlyOtAllowed,
            MicroOtBufferMinutes = policy.MicroOtBufferMinutes,
            RequireHoursMet = policy.RequireHoursMet,
            FilingMode = policy.FilingMode,
            PreApprovalRequired = policy.PreApprovalRequired,
            AllowPostFiling = policy.AllowPostFiling,
            ApprovalFlowType = policy.ApprovalFlowType,
            AutoApprove = policy.AutoApprove,
            RoundingMinutes = policy.RoundingMinutes,
            RoundingDirection = policy.RoundingDirection,
            MinimumBlockMinutes = policy.MinimumBlockMinutes,
            MaxPerDayHours = policy.MaxPerDayHours,
            MaxPerWeekHours = policy.MaxPerWeekHours,
            LimitMode = policy.LimitMode,
            OverrideRole = policy.OverrideRole,
            ScopeMode = policy.ScopeMode
        };

        dto.Buckets = policy.Buckets
            .OrderBy(b => b.Key)
            .Select(b => new OrgAttendanceOvertimeBucketDto
            {
                Key = b.Key,
                Enabled = b.Enabled,
                ThresholdHours = b.ThresholdHours,
                MaxHours = b.MaxHours,
                MinimumBlockMinutes = b.MinimumBlockMinutes
            }).ToList();

        if (dto.Buckets.Count == 0)
            dto.Buckets = OrgAttendanceOvertimeDefaults.BuildSettings().Buckets;

        dto.ApprovalSteps = policy.ApprovalSteps
            .OrderBy(s => s.Order)
            .Select(s => new OrgAttendanceOvertimeApprovalStepDto
            {
                Order = s.Order,
                Role = s.Role,
                Required = s.Required
            }).ToList();

        var scopeFilters = new OrgAttendanceOvertimeScopeFiltersDto
        {
            Departments = policy.ScopeFilters.Where(f => f.FilterType == "Department").Select(f => f.Value).ToList(),
            Sites = policy.ScopeFilters.Where(f => f.FilterType == "Site").Select(f => f.Value).ToList(),
            EmploymentTypes = policy.ScopeFilters.Where(f => f.FilterType == "EmploymentType").Select(f => f.Value).ToList(),
            Roles = policy.ScopeFilters.Where(f => f.FilterType == "Role").Select(f => f.Value).ToList()
        };

        if (string.Equals(dto.ScopeMode, "SELECTED", StringComparison.OrdinalIgnoreCase))
            dto.ScopeFilters = scopeFilters;
        else
            dto.ScopeFilters = new OrgAttendanceOvertimeScopeFiltersDto();

        var nightDiff = new OrgAttendanceNightDifferentialDto
        {
            Enabled = policy.NightDiffEnabled,
            WindowStart = policy.NightDiffWindowStart,
            WindowEnd = policy.NightDiffWindowEnd,
            MinimumMinutes = policy.NightDiffMinimumMinutes,
            ExcludeBreaks = policy.NightDiffExcludeBreaks
        };

        var scoped = new List<OrgAttendanceNightDifferentialScopedExclusionDto>();
        foreach (var exclusion in policy.NightDiffExclusions)
        {
            var hasDept = !string.IsNullOrWhiteSpace(exclusion.Department);
            var hasSite = !string.IsNullOrWhiteSpace(exclusion.Site);
            var hasRole = !string.IsNullOrWhiteSpace(exclusion.Role);
            var filled = (hasDept ? 1 : 0) + (hasSite ? 1 : 0) + (hasRole ? 1 : 0);

            if (filled <= 1)
            {
                if (hasDept)
                    nightDiff.ExcludeDepartments.Add(exclusion.Department!);
                else if (hasSite)
                    nightDiff.ExcludeSites.Add(exclusion.Site!);
                else if (hasRole)
                    nightDiff.ExcludeRoles.Add(exclusion.Role!);
            }
            else
            {
                scoped.Add(new OrgAttendanceNightDifferentialScopedExclusionDto
                {
                    Department = exclusion.Department ?? string.Empty,
                    Site = exclusion.Site ?? string.Empty,
                    Role = exclusion.Role ?? string.Empty
                });
            }
        }

        nightDiff.ScopedExclusions = scoped;
        dto.NightDifferential = nightDiff;

        return dto;
    }

    private void ApplyDtoToPolicy(OrgAttendanceOvertimePolicy policy, OrgAttendanceOvertimeDto dto)
    {
        var nightDiff = dto.NightDifferential ?? OrgAttendanceOvertimeDefaults.BuildNightDifferential();

        policy.Enabled = dto.Enabled;
        policy.Basis = dto.Basis;
        policy.RestHolidayOverride = dto.RestHolidayOverride;
        policy.DailyThresholdHours = dto.DailyThresholdHours;
        policy.WeeklyThresholdHours = dto.WeeklyThresholdHours;
        policy.EarlyOtAllowed = dto.EarlyOtAllowed;
        policy.MicroOtBufferMinutes = dto.MicroOtBufferMinutes;
        policy.RequireHoursMet = dto.RequireHoursMet;
        policy.FilingMode = dto.FilingMode;
        policy.PreApprovalRequired = dto.PreApprovalRequired;
        policy.AllowPostFiling = dto.AllowPostFiling;
        policy.ApprovalFlowType = dto.ApprovalFlowType;
        policy.AutoApprove = dto.AutoApprove;
        policy.RoundingMinutes = dto.RoundingMinutes;
        policy.RoundingDirection = dto.RoundingDirection;
        policy.MinimumBlockMinutes = dto.MinimumBlockMinutes;
        policy.MaxPerDayHours = dto.MaxPerDayHours;
        policy.MaxPerWeekHours = dto.MaxPerWeekHours;
        policy.LimitMode = dto.LimitMode;
        policy.OverrideRole = dto.OverrideRole;
        policy.ScopeMode = dto.ScopeMode;
        policy.NightDiffEnabled = nightDiff.Enabled;
        policy.NightDiffWindowStart = nightDiff.WindowStart;
        policy.NightDiffWindowEnd = nightDiff.WindowEnd;
        policy.NightDiffMinimumMinutes = nightDiff.MinimumMinutes;
        policy.NightDiffExcludeBreaks = nightDiff.ExcludeBreaks;

        _tenantDb.OrgAttendanceOvertimeBuckets.RemoveRange(policy.Buckets);
        policy.Buckets.Clear();
        foreach (var bucket in dto.Buckets)
        {
            policy.Buckets.Add(new OrgAttendanceOvertimeBucket
            {
                Key = bucket.Key,
                Enabled = bucket.Enabled,
                ThresholdHours = bucket.ThresholdHours,
                MaxHours = bucket.MaxHours,
                MinimumBlockMinutes = bucket.MinimumBlockMinutes
            });
        }

        _tenantDb.OrgAttendanceOvertimeApprovalSteps.RemoveRange(policy.ApprovalSteps);
        policy.ApprovalSteps.Clear();
        foreach (var step in dto.ApprovalSteps.OrderBy(s => s.Order))
        {
            policy.ApprovalSteps.Add(new OrgAttendanceOvertimeApprovalStep
            {
                Order = step.Order,
                Role = step.Role,
                Required = step.Required
            });
        }

        _tenantDb.OrgAttendanceOvertimeScopeFilters.RemoveRange(policy.ScopeFilters);
        policy.ScopeFilters.Clear();
        if (string.Equals(dto.ScopeMode, "SELECTED", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var value in dto.ScopeFilters.Departments ?? new List<string>())
                policy.ScopeFilters.Add(new OrgAttendanceOvertimeScopeFilter { FilterType = "Department", Value = value });
            foreach (var value in dto.ScopeFilters.Sites ?? new List<string>())
                policy.ScopeFilters.Add(new OrgAttendanceOvertimeScopeFilter { FilterType = "Site", Value = value });
            foreach (var value in dto.ScopeFilters.EmploymentTypes ?? new List<string>())
                policy.ScopeFilters.Add(new OrgAttendanceOvertimeScopeFilter { FilterType = "EmploymentType", Value = value });
            foreach (var value in dto.ScopeFilters.Roles ?? new List<string>())
                policy.ScopeFilters.Add(new OrgAttendanceOvertimeScopeFilter { FilterType = "Role", Value = value });
        }

        _tenantDb.OrgAttendanceNightDiffExclusions.RemoveRange(policy.NightDiffExclusions);
        policy.NightDiffExclusions.Clear();

        foreach (var value in nightDiff.ExcludeDepartments ?? new List<string>())
        {
            if (!string.IsNullOrWhiteSpace(value))
                policy.NightDiffExclusions.Add(new OrgAttendanceNightDiffExclusion { Department = value.Trim() });
        }

        foreach (var value in nightDiff.ExcludeSites ?? new List<string>())
        {
            if (!string.IsNullOrWhiteSpace(value))
                policy.NightDiffExclusions.Add(new OrgAttendanceNightDiffExclusion { Site = value.Trim() });
        }

        foreach (var value in nightDiff.ExcludeRoles ?? new List<string>())
        {
            if (!string.IsNullOrWhiteSpace(value))
                policy.NightDiffExclusions.Add(new OrgAttendanceNightDiffExclusion { Role = value.Trim() });
        }

        foreach (var combo in nightDiff.ScopedExclusions ?? new List<OrgAttendanceNightDifferentialScopedExclusionDto>())
        {
            var dept = string.IsNullOrWhiteSpace(combo?.Department) ? null : combo!.Department.Trim();
            var site = string.IsNullOrWhiteSpace(combo?.Site) ? null : combo!.Site.Trim();
            var role = string.IsNullOrWhiteSpace(combo?.Role) ? null : combo!.Role.Trim();
            var filled = (dept is null ? 0 : 1) + (site is null ? 0 : 1) + (role is null ? 0 : 1);
            if (filled <= 1)
            {
                if (!string.IsNullOrEmpty(dept))
                    policy.NightDiffExclusions.Add(new OrgAttendanceNightDiffExclusion { Department = dept });
                else if (!string.IsNullOrEmpty(site))
                    policy.NightDiffExclusions.Add(new OrgAttendanceNightDiffExclusion { Site = site });
                else if (!string.IsNullOrEmpty(role))
                    policy.NightDiffExclusions.Add(new OrgAttendanceNightDiffExclusion { Role = role });
                continue;
            }

            policy.NightDiffExclusions.Add(new OrgAttendanceNightDiffExclusion
            {
                Department = dept,
                Site = site,
                Role = role
            });
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
