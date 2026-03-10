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
    private readonly SubchronDbContext _db;

    public OrgHolidayPayrollRulesController(SubchronDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAsync(CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var rules = await _db.EarningRules.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.IsActive && (x.AppliesTo == "Holiday" || x.AppliesTo == "RestDay"))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                id = x.EarningRuleID,
                name = x.Name,
                classification = MapClassification(x)
            })
            .ToListAsync(ct);

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
}
