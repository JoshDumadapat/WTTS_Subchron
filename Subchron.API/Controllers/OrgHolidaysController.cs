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

        var existing = await _tenantDb.OrgHolidayConfigs
            .Where(h => h.OrgID == orgId.Value && h.HolidayDate.Year == targetYear && h.SourceTag == "HolidayApi")
            .ToListAsync(ct);

        foreach (var holiday in externalHolidays)
        {
            var match = existing.FirstOrDefault(x => x.HolidayDate.Date == holiday.Date.Date && string.Equals(x.Name, holiday.Name, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                match = new OrgHolidayConfig
                {
                    OrgID = orgId.Value,
                    HolidayDate = holiday.Date.Date,
                    Name = holiday.Name,
                    Type = holiday.Type,
                    Status = "Active",
                    ScopeType = "Nationwide",
                    ScopeValuesJson = "[]",
                    SourceTag = "HolidayApi",
                    OverlapStrategy = "HighestPrecedence",
                    Precedence = 100,
                    IncludeAttendance = true,
                    NonWorkingDay = !string.Equals(holiday.Type, "SpecialWorkingHoliday", StringComparison.OrdinalIgnoreCase),
                    AllowWork = true,
                    ApplyRestDayRules = true,
                    AttendanceClassification = holiday.Type,
                    RestDayAttendanceClassification = string.Empty,
                    IncludePayroll = true,
                    UsePayRules = true,
                    PaidWhenUnworked = holiday.IsPublic,
                    PayrollClassification = holiday.Type,
                    RestDayPayrollClassification = string.Empty,
                    PayrollRuleId = string.Empty,
                    RestDayPayrollRuleId = string.Empty,
                    ReferenceNo = string.Empty,
                    ReferenceUrl = string.Empty,
                    OfficialTag = "HolidayApi",
                    PayrollNotes = string.Empty,
                    Notes = string.Empty,
                    IsSynced = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _tenantDb.OrgHolidayConfigs.Add(match);
            }
            else
            {
                match.Type = holiday.Type;
                match.Status = "Active";
                match.SourceTag = "HolidayApi";
                match.AttendanceClassification = holiday.Type;
                match.PayrollClassification = holiday.Type;
                match.PaidWhenUnworked = holiday.IsPublic;
                match.IsSynced = true;
                match.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _tenantDb.SaveChangesAsync(ct);

        var items = await _tenantDb.OrgHolidayConfigs.AsNoTracking()
            .Where(h => h.OrgID == orgId.Value && h.HolidayDate.Year == targetYear)
            .OrderBy(h => h.HolidayDate)
            .ThenByDescending(h => h.Precedence)
            .ToListAsync(ct);

        return Ok(new
        {
            synced = true,
            year = targetYear,
            importedFromYear,
            items = items.Select(MapToResponse).ToList()
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
            IsSynced = entity.IsSynced,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
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
