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
    private readonly ILogger<OrgHolidaysController> _logger;

    public OrgHolidaysController(TenantDbContext tenantDb, IAuditService auditService, IHolidayApiService holidayApiService, ILogger<OrgHolidaysController> logger)
    {
        _tenantDb = tenantDb;
        _auditService = auditService;
        _holidayApiService = holidayApiService;
        _logger = logger;
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

        List<OrgHolidayConfig> items;
        try
        {
            items = await query
                .OrderBy(h => h.HolidayDate)
                .ThenByDescending(h => h.Precedence)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            if (await TryRepairTenantSchemaAsync(ex, ct))
            {
                items = await query
                    .OrderBy(h => h.HolidayDate)
                    .ThenByDescending(h => h.Precedence)
                    .ToListAsync(ct);
            }
            else
            {
                _logger.LogWarning(ex, "Holiday config query failed; returning empty set to keep UI responsive.");
                return Ok(Array.Empty<OrgHolidayResponse>());
            }
        }

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

        if (!TryNormalizeDate(request, out var normalizedDate))
            return BadRequest(new { ok = false, message = "Holiday date is invalid. Expected format yyyy-MM-dd." });

        request.Date = normalizedDate;

        try
        {
            await NormalizeHolidayRulesAsync(request, orgId.Value, null, ct);

            var entity = MapToEntity(request, orgId.Value);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = entity.CreatedAt;

            _tenantDb.OrgHolidayConfigs.Add(entity);
            await _tenantDb.SaveChangesAsync(ct);

            await TryLogAuditAsync(orgId.Value, "OrgHolidayCreated", $"Created holiday {entity.Name} on {entity.HolidayDate:yyyy-MM-dd}", ct);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = entity.OrgHolidayConfigID }, MapToResponse(entity));
        }
        catch (DbUpdateException ex)
        {
            if (await TryRepairTenantSchemaAsync(ex, ct))
            {
                await NormalizeHolidayRulesAsync(request, orgId.Value, null, ct);
                var entity = MapToEntity(request, orgId.Value);
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = entity.CreatedAt;

                _tenantDb.OrgHolidayConfigs.Add(entity);
                await _tenantDb.SaveChangesAsync(ct);

                await TryLogAuditAsync(orgId.Value, "OrgHolidayCreated", $"Created holiday {entity.Name} on {entity.HolidayDate:yyyy-MM-dd}", ct);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = entity.OrgHolidayConfigID }, MapToResponse(entity));
            }

            _logger.LogError(ex, "Holiday create failed for org {OrgId}", orgId.Value);
            return StatusCode(500, new { ok = false, message = "Failed to save holiday rule." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holiday create failed for org {OrgId}", orgId.Value);
            return StatusCode(500, new { ok = false, message = "Failed to save holiday rule." });
        }
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

        if (!TryNormalizeDate(request, out var normalizedDate))
            return BadRequest(new { ok = false, message = "Holiday date is invalid. Expected format yyyy-MM-dd." });

        request.Date = normalizedDate;

        try
        {
            await NormalizeHolidayRulesAsync(request, orgId.Value, id, ct);

            MapToEntity(request, orgId.Value, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _tenantDb.SaveChangesAsync(ct);
            await TryLogAuditAsync(orgId.Value, "OrgHolidayUpdated", $"Updated holiday {entity.Name} on {entity.HolidayDate:yyyy-MM-dd}", ct);

            return Ok(MapToResponse(entity));
        }
        catch (DbUpdateException ex)
        {
            if (await TryRepairTenantSchemaAsync(ex, ct))
            {
                await NormalizeHolidayRulesAsync(request, orgId.Value, id, ct);
                MapToEntity(request, orgId.Value, entity);
                entity.UpdatedAt = DateTime.UtcNow;
                await _tenantDb.SaveChangesAsync(ct);
                await TryLogAuditAsync(orgId.Value, "OrgHolidayUpdated", $"Updated holiday {entity.Name} on {entity.HolidayDate:yyyy-MM-dd}", ct);
                return Ok(MapToResponse(entity));
            }

            _logger.LogError(ex, "Holiday update failed for org {OrgId}, holiday {HolidayId}", orgId.Value, id);
            return StatusCode(500, new { ok = false, message = "Failed to save holiday rule." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holiday update failed for org {OrgId}, holiday {HolidayId}", orgId.Value, id);
            return StatusCode(500, new { ok = false, message = "Failed to save holiday rule." });
        }
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

        await TryLogAuditAsync(orgId.Value, "OrgHolidayDeleted", $"Deleted holiday {entity.Name} on {entity.HolidayDate:yyyy-MM-dd}", ct);

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

        if ((request.ReferenceNo?.Length ?? 0) > 100)
            return "Reference number must be 100 characters or fewer.";
        if ((request.ReferenceUrl?.Length ?? 0) > 200)
            return "Reference URL must be 200 characters or fewer.";
        if ((request.OfficialTag?.Length ?? 0) > 80)
            return "Official tag must be 80 characters or fewer.";
        if ((request.PayrollRuleId?.Length ?? 0) > 80)
            return "Payroll rule id must be 80 characters or fewer.";
        if ((request.RestDayPayrollRuleId?.Length ?? 0) > 80)
            return "Rest day payroll rule id must be 80 characters or fewer.";
        if ((request.Name?.Length ?? 0) > 150)
            return "Holiday name must be 150 characters or fewer.";

        return null;
    }

    private static bool TryNormalizeDate(OrgHolidayRequest request, out string normalizedDate)
    {
        normalizedDate = string.Empty;
        if (request == null || string.IsNullOrWhiteSpace(request.Date))
            return false;

        if (DateTime.TryParseExact(request.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            || DateTime.TryParse(request.Date.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
            || DateTime.TryParse(request.Date.Trim(), out dt))
        {
            normalizedDate = dt.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return true;
        }

        return false;
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
        entity.Name = Truncate(request.Name.Trim(), 150);
        entity.Type = Truncate(request.Type.Trim(), 40);
        if (request.IsActive.HasValue)
            entity.Status = request.IsActive.Value ? "Active" : "Inactive";
        else
            entity.Status = Truncate(string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(), 40);
        entity.ScopeType = Truncate(string.IsNullOrWhiteSpace(request.ScopeType) ? "Nationwide" : request.ScopeType.Trim(), 40);
        entity.ScopeValuesJson = SerializeList(request.ScopeValues);
        entity.SourceTag = Truncate(request.SourceTag ?? "ManualEntry", 40);
        entity.OverlapStrategy = Truncate(request.OverlapStrategy ?? "HighestPrecedence", 40);
        entity.Precedence = request.Precedence;
        entity.IncludeAttendance = request.IncludeAttendance;
        entity.NonWorkingDay = request.NonWorkingDay;
        entity.AllowWork = request.AllowWork;
        entity.ApplyRestDayRules = request.ApplyRestDayRules;
        entity.AttendanceClassification = Truncate(request.AttendanceClassification ?? "Holiday", 60);
        entity.RestDayAttendanceClassification = Truncate(request.RestDayAttendanceClassification, 60);
        entity.EmployeeGroupScopeJson = SerializeList(request.EmployeeGroupScope);
        entity.IncludePayroll = request.IncludePayroll;
        entity.UsePayRules = request.UsePayRules;
        entity.PaidWhenUnworked = request.PaidWhenUnworked;
        entity.PayrollClassification = Truncate(request.PayrollClassification ?? request.Type ?? "RegularHoliday", 60);
        entity.RestDayPayrollClassification = Truncate(request.RestDayPayrollClassification, 60);
        entity.PayrollRuleId = Truncate(request.PayrollRuleId, 80);
        entity.RestDayPayrollRuleId = Truncate(request.RestDayPayrollRuleId, 80);
        entity.ReferenceNo = Truncate(request.ReferenceNo ?? string.Empty, 100);
        entity.ReferenceUrl = Truncate(request.ReferenceUrl ?? string.Empty, 200);
        entity.OfficialTag = Truncate(request.OfficialTag ?? string.Empty, 80);
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

    private async Task TryLogAuditAsync(int orgId, string action, string description, CancellationToken ct)
    {
        try
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
        catch
        {
            // Do not block holiday save if audit logging fails.
        }
    }

    private static string? Truncate(string? value, int max)
    {
        if (value == null) return null;
        return value.Length <= max ? value : value[..max];
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

    private async Task<bool> TryRepairTenantSchemaAsync(Exception ex, CancellationToken ct)
    {
        if (!LooksLikeSchemaIssue(ex)) return false;

        try
        {
            _logger.LogWarning(ex, "Detected possible tenant schema mismatch for holiday config. Attempting tenant migration and retry.");
            await _tenantDb.Database.MigrateAsync(ct);
            _logger.LogInformation("Tenant migration completed during holiday config recovery.");
            return true;
        }
        catch (Exception migrateEx)
        {
            _logger.LogError(migrateEx, "Tenant migration retry failed during holiday config recovery.");
            return false;
        }
    }

    private static bool LooksLikeSchemaIssue(Exception ex)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            var msg = cur.Message ?? string.Empty;
            if (msg.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("Could not find table", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
