using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;

namespace Subchron.API.Controllers;

[ApiController]
[Authorize]
[Route("api/orgconfig/payroll/holiday-rules")]
public class OrgHolidayPayrollRulesController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly ILogger<OrgHolidayPayrollRulesController> _logger;

    public OrgHolidayPayrollRulesController(TenantDbContext db, ILogger<OrgHolidayPayrollRulesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAsync(CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        List<object> rules;
        try
        {
            rules = await _db.EarningRules.AsNoTracking()
                .Where(x => x.OrgID == orgId.Value && x.IsActive && (x.AppliesTo == "Holiday" || x.AppliesTo == "RestDay"))
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    id = x.EarningRuleID.ToString(),
                    name = x.Name,
                    classification = MapClassification(x)
                })
                .Cast<object>()
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            if (await TryRepairTenantSchemaAsync(ex, ct))
            {
                rules = await _db.EarningRules.AsNoTracking()
                    .Where(x => x.OrgID == orgId.Value && x.IsActive && (x.AppliesTo == "Holiday" || x.AppliesTo == "RestDay"))
                    .OrderBy(x => x.Name)
                    .Select(x => new
                    {
                        id = x.EarningRuleID.ToString(),
                        name = x.Name,
                        classification = MapClassification(x)
                    })
                    .Cast<object>()
                    .ToListAsync(ct);
            }
            else
            {
                _logger.LogWarning(ex, "Failed to load holiday payroll rules. Returning defaults.");
                rules = new List<object>();
            }
        }

        if (rules.Count == 0)
            rules = BuildDefaultRules();

        return Ok(rules);
    }

    private static string MapClassification(Models.Entities.EarningRule rule)
    {
        if (string.Equals(rule.AppliesTo, "RestDay", StringComparison.OrdinalIgnoreCase))
        {
            return rule.HolidayCombo switch
            {
                "RegularRestDay" => "RestDayRegularHoliday",
                "SpecialRestDay" => "RestDaySpecialNonWorkingHoliday",
                "DoubleHoliday" => "RestDayDoubleHoliday",
                "SpecialWorking" => "RestDaySpecialWorkingHoliday",
                _ => "RestDayRegularHoliday"
            };
        }

        return rule.HolidayCombo switch
        {
            "SpecialWorking" => "SpecialWorkingHoliday",
            "DoubleHoliday" => "DoubleHoliday",
            "SpecialRestDay" => "SpecialNonWorkingHoliday",
            _ => "RegularHoliday"
        };
    }

    private int? GetOrgId()
    {
        var claim = User.FindFirst("orgId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static List<object> BuildDefaultRules()
    {
        return new List<object>
        {
            new { id = "default-regular", name = "Regular Holiday Rule", classification = "RegularHoliday" },
            new { id = "default-special-nonworking", name = "Special Non-working Rule", classification = "SpecialNonWorkingHoliday" },
            new { id = "default-special-working", name = "Special Working Rule", classification = "SpecialWorkingHoliday" },
            new { id = "default-double", name = "Double Holiday Rule", classification = "DoubleHoliday" },
            new { id = "default-local", name = "Local Holiday Rule", classification = "LocalHoliday" },
            new { id = "default-company", name = "Company Holiday Rule", classification = "CompanyHoliday" },
            new { id = "default-restday-regular", name = "Rest Day + Regular Holiday Rule", classification = "RestDayRegularHoliday" },
            new { id = "default-restday-special-nonworking", name = "Rest Day + Special Non-working Rule", classification = "RestDaySpecialNonWorkingHoliday" },
            new { id = "default-restday-special-working", name = "Rest Day + Special Working Rule", classification = "RestDaySpecialWorkingHoliday" },
            new { id = "default-restday-double", name = "Rest Day + Double Holiday Rule", classification = "RestDayDoubleHoliday" }
        };
    }

    private async Task<bool> TryRepairTenantSchemaAsync(Exception ex, CancellationToken ct)
    {
        if (!LooksLikeSchemaIssue(ex)) return false;

        try
        {
            _logger.LogWarning(ex, "Detected tenant schema issue while loading holiday payroll rules. Attempting migration.");
            await _db.Database.MigrateAsync(ct);
            return true;
        }
        catch (Exception migrateEx)
        {
            _logger.LogError(migrateEx, "Tenant migration failed during holiday payroll rules recovery.");
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
                return true;
        }
        return false;
    }
}
