using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
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
[Route("api/orgconfig/holidays")]
public class OrgHolidaysController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly TenantDbContext _tenantDb;
    private readonly IAuditService _auditService;
    private readonly IHolidayApiService _holidayApiService;

    public OrgHolidaysController(TenantDbContext tenantDb, IAuditService auditService, IHolidayApiService holidayApiService)
    {
        _tenantDb = tenantDb;
        _auditService = auditService;
        _holidayApiService = holidayApiService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrgHolidayResponse>>> GetAllAsync([FromQuery] int? year, CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var query = _tenantDb.OrgHolidayConfigs.AsNoTracking()
            .Where(h => h.OrgID == orgId.Value);

        if (year.HasValue)
        {
            query = query.Where(h => h.HolidayDate.Year == year.Value);
        }

        var items = await query
            .OrderBy(h => h.HolidayDate)
            .ThenByDescending(h => h.Precedence)
            .ToListAsync(ct);

        return Ok(items.Select(MapToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrgHolidayResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var entity = await _tenantDb.OrgHolidayConfigs.AsNoTracking()
            .FirstOrDefaultAsync(h => h.OrgHolidayConfigID == id && h.OrgID == orgId.Value, ct);

        if (entity == null)
            return NotFound();

        return Ok(MapToResponse(entity));
    }

    [HttpPost]
    public async Task<ActionResult<OrgHolidayResponse>> CreateAsync([FromBody] OrgHolidayRequest request, CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var validationError = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validationError))
            return BadRequest(new { ok = false, message = validationError });

        request.Date = DateTime.Parse(request.Date, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
        await NormalizeHolidayRulesAsync(request, orgId.Value, null, ct);

        var entity = MapToEntity(request, orgId.Value);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = entity.CreatedAt;

        _tenantDb.OrgHolidayConfigs.Add(entity);
        await _tenantDb.SaveChangesAsync(ct);

        await LogAuditAsync(orgId.Value, "OrgHolidayCreated", $"Created holiday {entity.Name} on {entity.HolidayDate:yyyy-MM-dd}", ct);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = entity.OrgHolidayConfigID }, MapToResponse(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrgHolidayResponse>> UpdateAsync(int id, [FromBody] OrgHolidayRequest request, CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var entity = await _tenantDb.OrgHolidayConfigs
            .FirstOrDefaultAsync(h => h.OrgHolidayConfigID == id && h.OrgID == orgId.Value, ct);

        if (entity == null)
            return NotFound();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var validationError = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validationError))
            return BadRequest(new { ok = false, message = validationError });

        request.Date = DateTime.Parse(request.Date, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
        await NormalizeHolidayRulesAsync(request, orgId.Value, id, ct);

        MapToEntity(request, orgId.Value, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        await _tenantDb.SaveChangesAsync(ct);
        await LogAuditAsync(orgId.Value, "OrgHolidayUpdated", $"Updated holiday {entity.Name} on {entity.HolidayDate:yyyy-MM-dd}", ct);

        return Ok(MapToResponse(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var entity = await _tenantDb.OrgHolidayConfigs
            .FirstOrDefaultAsync(h => h.OrgHolidayConfigID == id && h.OrgID == orgId.Value, ct);

        if (entity == null)
            return NotFound();

        _tenantDb.OrgHolidayConfigs.Remove(entity);
        await _tenantDb.SaveChangesAsync(ct);

        await LogAuditAsync(orgId.Value, "OrgHolidayDeleted", $"Deleted holiday {entity.Name} on {entity.HolidayDate:yyyy-MM-dd}", ct);

        return Ok(new { ok = true });
    }

    [HttpPost("sync")]
    public async Task<ActionResult<object>> SyncAsync([FromQuery] int? year, CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var targetYear = year ?? DateTime.UtcNow.Year;

        var externalHolidays = await _holidayApiService.GetPhilippinesHolidaysAsync(targetYear, ct);
        var importedFromYear = targetYear;

        if (externalHolidays.Count == 0)
        {
            var fallbackYear = targetYear - 1;
            if (fallbackYear >= 1900)
            {
                var fallback = await _holidayApiService.GetPhilippinesHolidaysAsync(fallbackYear, ct);
                if (fallback.Count > 0)
                {
                    importedFromYear = fallbackYear;
                    externalHolidays = fallback
                        .Select(h =>
                        {
                            var day = h.Date.Day;
                            var maxDay = DateTime.DaysInMonth(targetYear, h.Date.Month);
                            if (day > maxDay) day = maxDay;
                            return new HolidayApiHoliday(h.Name, new DateTime(targetYear, h.Date.Month, day), h.Type, h.IsPublic);
                        })
                        .ToList();
                }
            }
        }

        if (externalHolidays.Count == 0)
            return BadRequest(new { ok = false, message = "No holidays returned from Holiday API for the selected year or fallback year." });

        var items = externalHolidays
            .OrderBy(h => h.Date)
            .ThenBy(h => h.Name)
            .Select(h => new
            {
                id = (int?)null,
                date = h.Date.ToString("yyyy-MM-dd"),
                name = h.Name,
                type = h.Type,
                status = "Active",
                isActive = true,
                scopeType = "Nationwide",
                scopeValues = Array.Empty<string>(),
                sourceTag = "HolidayApi",
                overlapStrategy = "HighestPrecedence",
                precedence = 100,
                includeAttendance = true,
                nonWorkingDay = !string.Equals(h.Type, "SpecialWorkingHoliday", StringComparison.OrdinalIgnoreCase),
                allowWork = true,
                applyRestDayRules = true,
                attendanceClassification = h.Type,
                restDayAttendanceClassification = string.Empty,
                employeeGroupScope = Array.Empty<string>(),
                includePayroll = true,
                usePayRules = true,
                paidWhenUnworked = h.IsPublic,
                payrollClassification = h.Type,
                restDayPayrollClassification = string.Empty,
                payrollRuleId = string.Empty,
                restDayPayrollRuleId = string.Empty,
                referenceNo = string.Empty,
                referenceUrl = string.Empty,
                officialTag = "HolidayApi",
                payrollNotes = string.Empty,
                notes = string.Empty,
                hasRuleConfigured = false,
                isSynced = true
            })
            .ToList();

        return Ok(new
        {
            synced = true,
            year = targetYear,
            importedFromYear,
            items
        });
    }

    private static string? ValidateRequest(OrgHolidayRequest request)
    {
        if (!DateTime.TryParse(request.Date, out _))
            return "Holiday date is invalid. Expected format yyyy-MM-dd.";

        if (string.IsNullOrWhiteSpace(request.Name))
            return "Holiday name is required.";

        if (string.IsNullOrWhiteSpace(request.Type))
            return "Holiday classification is required.";

        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RegularHoliday", "SpecialNonWorkingHoliday", "SpecialWorkingHoliday", "DoubleHoliday", "LocalHoliday", "CompanyHoliday"
        };
        if (!allowedTypes.Contains(request.Type))
            return "Holiday classification is invalid.";

        var allowedScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Nationwide", "Region", "Province", "CityMunicipality", "Barangay", "Site", "Department", "EmployeeGroup", "CompanyWide"
        };
        if (!allowedScopes.Contains(request.ScopeType ?? string.Empty))
            return "Scope type is invalid.";

        var allowedOverlap = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "HighestPrecedence", "CombineAsDoubleHoliday", "ManualResolution"
        };
        if (!allowedOverlap.Contains(request.OverlapStrategy ?? string.Empty))
            return "Overlap strategy is invalid.";

        if (!string.Equals(request.ScopeType, "Nationwide", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.ScopeType, "CompanyWide", StringComparison.OrdinalIgnoreCase))
        {
            if (request.ScopeValues == null || request.ScopeValues.Count == 0)
                return "Scope values are required when scope type is not Nationwide or CompanyWide.";
        }

        if (request.IncludePayroll && request.UsePayRules && string.IsNullOrWhiteSpace(request.PayrollRuleId))
            return "Please select a payroll rule when payroll is included and pay rules are enabled.";

        return null;
    }

    private OrgHolidayResponse MapToResponse(OrgHolidayConfig entity)
    {
        return new OrgHolidayResponse
        {
            Id = entity.OrgHolidayConfigID,
            Date = entity.HolidayDate.ToString("yyyy-MM-dd"),
            Name = entity.Name,
            Type = entity.Type,
            Status = entity.Status,
            IsActive = !string.Equals(entity.Status, "Inactive", StringComparison.OrdinalIgnoreCase),
            ScopeType = entity.ScopeType,
            ScopeValues = DeserializeList(entity.ScopeValuesJson),
            SourceTag = entity.SourceTag,
            OverlapStrategy = entity.OverlapStrategy,
            Precedence = entity.Precedence,
            IncludeAttendance = entity.IncludeAttendance,
            NonWorkingDay = entity.NonWorkingDay,
            AllowWork = entity.AllowWork,
            ApplyRestDayRules = entity.ApplyRestDayRules,
            AttendanceClassification = entity.AttendanceClassification,
            RestDayAttendanceClassification = entity.RestDayAttendanceClassification,
            EmployeeGroupScope = DeserializeList(entity.EmployeeGroupScopeJson),
            IncludePayroll = entity.IncludePayroll,
            UsePayRules = entity.UsePayRules,
            PaidWhenUnworked = entity.PaidWhenUnworked,
            PayrollClassification = entity.PayrollClassification,
            RestDayPayrollClassification = entity.RestDayPayrollClassification,
            PayrollRuleId = entity.PayrollRuleId,
            RestDayPayrollRuleId = entity.RestDayPayrollRuleId,
            ReferenceNo = entity.ReferenceNo,
            ReferenceUrl = entity.ReferenceUrl,
            OfficialTag = entity.OfficialTag,
            PayrollNotes = entity.PayrollNotes,
            Notes = entity.Notes,
            HasRuleConfigured = ComputeHasRuleConfigured(entity),
            IsSynced = entity.IsSynced,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task NormalizeHolidayRulesAsync(OrgHolidayRequest request, int orgId, int? excludeId, CancellationToken ct)
    {
        var scopeType = string.IsNullOrWhiteSpace(request.ScopeType) ? "Nationwide" : request.ScopeType.Trim();
        var scopeValues = (request.ScopeValues ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        request.ScopeType = scopeType;
        request.ScopeValues = scopeValues;

        var holidayDate = DateTime.Parse(request.Date, CultureInfo.InvariantCulture).Date;
        var overlapExists = await _tenantDb.OrgHolidayConfigs.AsNoTracking()
            .Where(h => h.OrgID == orgId)
            .Where(h => !excludeId.HasValue || h.OrgHolidayConfigID != excludeId.Value)
            .Where(h => h.HolidayDate == holidayDate)
            .Where(h => h.ScopeType == scopeType)
            .Select(h => new { h.ScopeValuesJson })
            .ToListAsync(ct);

        var hasOverlapForScope = overlapExists.Any(x => ScopeValuesEqual(DeserializeList(x.ScopeValuesJson), scopeValues));

        var isCompanyWide = string.Equals(scopeType, "CompanyWide", StringComparison.OrdinalIgnoreCase);
        var isNationwide = string.Equals(scopeType, "Nationwide", StringComparison.OrdinalIgnoreCase);
        var isLocalScope = !isCompanyWide && !isNationwide;

        var finalType = "RegularHoliday";
        if (string.Equals(request.OverlapStrategy, "CombineAsDoubleHoliday", StringComparison.OrdinalIgnoreCase) && hasOverlapForScope)
            finalType = "DoubleHoliday";
        else if (isCompanyWide)
            finalType = "CompanyHoliday";
        else if (isLocalScope)
            finalType = "LocalHoliday";
        else if (!request.NonWorkingDay && request.AllowWork)
            finalType = "SpecialWorkingHoliday";
        else if (request.NonWorkingDay && !request.PaidWhenUnworked)
            finalType = "SpecialNonWorkingHoliday";

        request.Type = finalType;

        var defaults = GetDefaultClassificationConfig(finalType);
        request.NonWorkingDay = defaults.NonWorkingDay;
        request.AllowWork = defaults.AllowWork;
        request.AttendanceClassification = defaults.AttendanceClassification;
        request.PayrollClassification = defaults.PayrollClassification;
        request.PaidWhenUnworked = defaults.PaidWhenUnworked;

        if (request.ApplyRestDayRules)
        {
            request.RestDayAttendanceClassification = defaults.RestDayAttendanceClassification;
            request.RestDayPayrollClassification = defaults.RestDayPayrollClassification;
        }
        else
        {
            request.RestDayAttendanceClassification = string.Empty;
            request.RestDayPayrollClassification = string.Empty;
            request.RestDayPayrollRuleId = string.Empty;
        }

        if (!request.IncludePayroll)
        {
            request.UsePayRules = false;
            request.PayrollRuleId = string.Empty;
            request.RestDayPayrollRuleId = string.Empty;
        }
    }

    private static bool ScopeValuesEqual(List<string> a, List<string> b)
    {
        var aa = a.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        var bb = b.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        return aa.Length == bb.Length && aa.SequenceEqual(bb, StringComparer.OrdinalIgnoreCase);
    }

    private static bool ComputeHasRuleConfigured(OrgHolidayConfig entity)
    {
        var hasScopeValues = DeserializeList(entity.ScopeValuesJson).Count > 0;
        var hasGroupScope = DeserializeList(entity.EmployeeGroupScopeJson).Count > 0;

        return !string.Equals(entity.ScopeType, "Nationwide", StringComparison.OrdinalIgnoreCase)
            || hasScopeValues
            || hasGroupScope
            || !string.Equals(entity.OverlapStrategy, "HighestPrecedence", StringComparison.OrdinalIgnoreCase)
            || entity.Precedence != 100
            || !entity.IncludeAttendance
            || !entity.ApplyRestDayRules
            || !entity.IncludePayroll
            || !entity.UsePayRules
            || !string.IsNullOrWhiteSpace(entity.PayrollRuleId)
            || !string.IsNullOrWhiteSpace(entity.RestDayPayrollRuleId)
            || !string.IsNullOrWhiteSpace(entity.RestDayAttendanceClassification)
            || !string.IsNullOrWhiteSpace(entity.RestDayPayrollClassification);
    }

    private static (bool NonWorkingDay, bool AllowWork, bool PaidWhenUnworked, string AttendanceClassification, string RestDayAttendanceClassification, string PayrollClassification, string RestDayPayrollClassification) GetDefaultClassificationConfig(string type)
    {
        return type switch
        {
            "RegularHoliday" => (true, true, true, "RegularHoliday", "RestDayRegularHoliday", "RegularHoliday", "RestDayRegularHoliday"),
            "SpecialNonWorkingHoliday" => (true, true, false, "SpecialNonWorkingHoliday", "RestDaySpecialNonWorkingHoliday", "SpecialNonWorkingHoliday", "RestDaySpecialNonWorkingHoliday"),
            "SpecialWorkingHoliday" => (false, true, false, "SpecialWorkingHoliday", "RestDaySpecialWorkingHoliday", "SpecialWorkingHoliday", "RestDaySpecialWorkingHoliday"),
            "DoubleHoliday" => (true, true, true, "DoubleHoliday", "RestDayDoubleHoliday", "DoubleHoliday", "RestDayDoubleHoliday"),
            "LocalHoliday" => (true, true, false, "LocalHoliday", "RestDayLocalHoliday", "LocalHoliday", "RestDayLocalHoliday"),
            "CompanyHoliday" => (true, true, false, "CompanyHoliday", "RestDayCompanyHoliday", "CompanyHoliday", "RestDayCompanyHoliday"),
            _ => (true, true, false, "Holiday", string.Empty, "RegularHoliday", string.Empty)
        };
    }

    private OrgHolidayConfig MapToEntity(OrgHolidayRequest request, int orgId, OrgHolidayConfig? entity = null)
    {
        entity ??= new OrgHolidayConfig();

        entity.OrgID = orgId;
        entity.HolidayDate = DateTime.Parse(request.Date, CultureInfo.InvariantCulture).Date;
        entity.Name = request.Name.Trim();
        entity.Type = request.Type.Trim();
        if (request.IsActive.HasValue)
            entity.Status = request.IsActive.Value ? "Active" : "Inactive";
        else
            entity.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
        entity.ScopeType = string.IsNullOrWhiteSpace(request.ScopeType) ? "Nationwide" : request.ScopeType.Trim();
        entity.ScopeValuesJson = SerializeList(request.ScopeValues);
        entity.SourceTag = request.SourceTag ?? "ManualEntry";
        entity.OverlapStrategy = request.OverlapStrategy ?? "HighestPrecedence";
        entity.Precedence = request.Precedence;
        entity.IncludeAttendance = request.IncludeAttendance;
        entity.NonWorkingDay = request.NonWorkingDay;
        entity.AllowWork = request.AllowWork;
        entity.ApplyRestDayRules = request.ApplyRestDayRules;
        entity.AttendanceClassification = request.AttendanceClassification ?? "Holiday";
        entity.RestDayAttendanceClassification = request.RestDayAttendanceClassification;
        entity.EmployeeGroupScopeJson = SerializeList(request.EmployeeGroupScope);
        entity.IncludePayroll = request.IncludePayroll;
        entity.UsePayRules = request.UsePayRules;
        entity.PaidWhenUnworked = request.PaidWhenUnworked;
        entity.PayrollClassification = request.PayrollClassification ?? request.Type ?? "RegularHoliday";
        entity.RestDayPayrollClassification = request.RestDayPayrollClassification;
        entity.PayrollRuleId = request.PayrollRuleId;
        entity.RestDayPayrollRuleId = request.RestDayPayrollRuleId;
        entity.ReferenceNo = request.ReferenceNo ?? string.Empty;
        entity.ReferenceUrl = request.ReferenceUrl ?? string.Empty;
        entity.OfficialTag = request.OfficialTag ?? string.Empty;
        entity.PayrollNotes = request.PayrollNotes ?? string.Empty;
        entity.Notes = request.Notes ?? string.Empty;

        return entity;
    }

    private static string SerializeList(IEnumerable<string>? values)
    {
        var list = values?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        return JsonSerializer.Serialize(list, JsonOptions);
    }

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task LogAuditAsync(int orgId, string action, string description, CancellationToken ct)
    {
        var userId = GetUserId();
        if (IsSuperAdmin())
        {
            await _auditService.LogSuperAdminAsync(orgId, userId, action, nameof(OrgHolidayConfig), orgId, description, ct: ct);
        }
        else
        {
            await _auditService.LogTenantAsync(orgId, userId, action, nameof(OrgHolidayConfig), orgId, description, ct: ct);
        }
    }

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

    private bool IsSuperAdmin()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
}
