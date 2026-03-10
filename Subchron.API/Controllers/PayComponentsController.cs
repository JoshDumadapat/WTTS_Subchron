using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;

namespace Subchron.API.Controllers;

[ApiController]
[Authorize]
[Route("api/organizations/{orgId:int}/pay-components")]
public class PayComponentsController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TenantDbContext _db;

    public PayComponentsController(TenantDbContext db)
    {
        _db = db;
    }

    private bool CanAccessOrg(int orgId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return true;

        var claim = User.FindFirstValue("orgId");
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var id) && id == orgId;
    }

    #region Earnings

    [HttpGet("earnings")]
    public async Task<IActionResult> GetEarnings(int orgId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var rules = await _db.EarningRules.AsNoTracking()
            .Where(r => r.OrgID == orgId && r.IsActive)
            .OrderBy(r => r.EarningRuleID)
            .Select(r => new
            {
                r.EarningRuleID,
                r.Name,
                r.AppliesTo,
                r.DayType,
                r.HolidayCombo,
                r.RestDayHandling,
                r.Scope,
                ScopeTags = DeserializeTags(r.ScopeTagsJson),
                r.RateType,
                r.RateValue,
                r.IsTaxable,
                r.IncludeInBenefitBase,
                r.RequiresApproval,
                r.EffectiveFrom,
                r.EffectiveTo,
                r.Notes,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost("earnings")]
    public async Task<IActionResult> CreateEarning(int orgId, [FromBody] EarningRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var entity = new EarningRule
        {
            OrgID = orgId,
            Name = req.Name.Trim(),
            AppliesTo = req.AppliesTo,
            DayType = req.DayType,
            HolidayCombo = req.HolidayCombo,
            RestDayHandling = req.RestDayHandling,
            Scope = req.Scope,
            ScopeTagsJson = SerializeTags(req.ScopeTags),
            RateType = req.RateType,
            RateValue = req.RateValue,
            IsTaxable = req.IsTaxable,
            IncludeInBenefitBase = req.IncludeInBenefitBase,
            RequiresApproval = req.RequiresApproval,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = req.EffectiveTo,
            Notes = req.Notes ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.EarningRules.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true, id = entity.EarningRuleID });
    }

    [HttpPut("earnings/{ruleId:int}")]
    public async Task<IActionResult> UpdateEarning(int orgId, int ruleId, [FromBody] EarningRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var entity = await _db.EarningRules.FirstOrDefaultAsync(r => r.EarningRuleID == ruleId && r.OrgID == orgId);
        if (entity == null) return NotFound(new { ok = false, message = "Earning rule not found." });

        entity.Name = req.Name.Trim();
        entity.AppliesTo = req.AppliesTo;
        entity.DayType = req.DayType;
        entity.HolidayCombo = req.HolidayCombo;
        entity.RestDayHandling = req.RestDayHandling;
        entity.Scope = req.Scope;
        entity.ScopeTagsJson = SerializeTags(req.ScopeTags);
        entity.RateType = req.RateType;
        entity.RateValue = req.RateValue;
        entity.IsTaxable = req.IsTaxable;
        entity.IncludeInBenefitBase = req.IncludeInBenefitBase;
        entity.RequiresApproval = req.RequiresApproval;
        entity.EffectiveFrom = req.EffectiveFrom;
        entity.EffectiveTo = req.EffectiveTo;
        entity.Notes = req.Notes ?? string.Empty;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpDelete("earnings/{ruleId:int}")]
    public async Task<IActionResult> DeleteEarning(int orgId, int ruleId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var entity = await _db.EarningRules.FirstOrDefaultAsync(r => r.EarningRuleID == ruleId && r.OrgID == orgId);
        if (entity == null) return NotFound(new { ok = false, message = "Earning rule not found." });

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    #endregion

    #region Allowances

    [HttpGet("allowances")]
    public async Task<IActionResult> GetAllowances(int orgId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var rules = await _db.OrgAllowanceRules.AsNoTracking()
            .Where(r => r.OrgID == orgId)
            .OrderBy(r => r.OrgAllowanceRuleID)
            .Select(r => new
            {
                allowanceRuleID = r.OrgAllowanceRuleID,
                r.Name,
                r.AllowanceType,
                r.Category,
                r.Amount,
                r.IsTaxable,
                r.AttendanceDependent,
                r.ProrateIfPartialPeriod,
                r.EffectiveFrom,
                r.EffectiveTo,
                r.IsActive,
                ScopeTags = DeserializeTags(r.ScopeTagsJson),
                r.ComplianceNotes,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost("allowances")]
    public async Task<IActionResult> CreateAllowance(int orgId, [FromBody] AllowanceRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var rule = new OrgAllowanceRule
        {
            OrgID = orgId,
            Name = req.Name.Trim(),
            AllowanceType = req.AllowanceType,
            Category = req.Category,
            Amount = req.Amount,
            IsTaxable = req.IsTaxable,
            AttendanceDependent = req.AttendanceDependent,
            ProrateIfPartialPeriod = req.ProrateIfPartialPeriod,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = req.EffectiveTo,
            IsActive = req.IsActive,
            ScopeTagsJson = SerializeTags(req.ScopeTags),
            ComplianceNotes = req.ComplianceNotes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.OrgAllowanceRules.Add(rule);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true, id = rule.OrgAllowanceRuleID });
    }

    [HttpPut("allowances/{ruleId:int}")]
    public async Task<IActionResult> UpdateAllowance(int orgId, int ruleId, [FromBody] AllowanceRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var rule = await _db.OrgAllowanceRules.FirstOrDefaultAsync(r => r.OrgAllowanceRuleID == ruleId && r.OrgID == orgId);
        if (rule == null) return NotFound(new { ok = false, message = "Allowance not found." });

        rule.Name = req.Name.Trim();
        rule.AllowanceType = req.AllowanceType;
        rule.Category = req.Category;
        rule.Amount = req.Amount;
        rule.IsTaxable = req.IsTaxable;
        rule.AttendanceDependent = req.AttendanceDependent;
        rule.ProrateIfPartialPeriod = req.ProrateIfPartialPeriod;
        rule.EffectiveFrom = req.EffectiveFrom;
        rule.EffectiveTo = req.EffectiveTo;
        rule.IsActive = req.IsActive;
        rule.ScopeTagsJson = SerializeTags(req.ScopeTags);
        rule.ComplianceNotes = req.ComplianceNotes ?? string.Empty;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpDelete("allowances/{ruleId:int}")]
    public async Task<IActionResult> DeleteAllowance(int orgId, int ruleId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var rule = await _db.OrgAllowanceRules.FirstOrDefaultAsync(r => r.OrgAllowanceRuleID == ruleId && r.OrgID == orgId);
        if (rule == null) return NotFound(new { ok = false, message = "Allowance not found." });

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    #endregion

    #region Deductions

    [HttpGet("deductions")]
    public async Task<IActionResult> GetDeductions(int orgId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var rules = await _db.DeductionRules.AsNoTracking()
            .Where(r => r.OrgID == orgId && r.IsActive)
            .OrderBy(r => r.DeductionRuleID)
            .Select(r => new
            {
                r.DeductionRuleID,
                r.Name,
                r.Category,
                r.DeductionType,
                r.ComputeBasedOn,
                r.Amount,
                r.FormulaExpression,
                r.HasEmployerShare,
                r.HasEmployeeShare,
                r.EmployerSharePercent,
                r.EmployeeSharePercent,
                r.AutoCompute,
                r.EffectiveFrom,
                r.MaxDeductionAmount,
                ScopeTags = DeserializeTags(r.ScopeTagsJson),
                r.Notes,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost("deductions")]
    public async Task<IActionResult> CreateDeduction(int orgId, [FromBody] DeductionRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var rule = new DeductionRule
        {
            OrgID = orgId,
            Name = req.Name.Trim(),
            Category = req.Category,
            DeductionType = req.DeductionType,
            Amount = req.Amount,
            FormulaExpression = req.FormulaExpression?.Trim(),
            HasEmployerShare = req.HasEmployerShare,
            HasEmployeeShare = req.HasEmployeeShare,
            EmployerSharePercent = req.EmployerSharePercent,
            EmployeeSharePercent = req.EmployeeSharePercent,
            AutoCompute = req.AutoCompute,
            ComputeBasedOn = req.ComputeBasedOn,
            EffectiveFrom = req.EffectiveFrom,
            MaxDeductionAmount = req.MaxDeductionAmount,
            ScopeTagsJson = SerializeTags(req.ScopeTags),
            Notes = req.Notes ?? string.Empty,
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.DeductionRules.Add(rule);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true, id = rule.DeductionRuleID });
    }

    [HttpPut("deductions/{ruleId:int}")]
    public async Task<IActionResult> UpdateDeduction(int orgId, int ruleId, [FromBody] DeductionRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var rule = await _db.DeductionRules.FirstOrDefaultAsync(r => r.DeductionRuleID == ruleId && r.OrgID == orgId);
        if (rule == null) return NotFound(new { ok = false, message = "Deduction rule not found." });

        rule.Name = req.Name.Trim();
        rule.Category = req.Category;
        rule.DeductionType = req.DeductionType;
        rule.Amount = req.Amount;
        rule.FormulaExpression = req.FormulaExpression?.Trim();
        rule.HasEmployerShare = req.HasEmployerShare;
        rule.HasEmployeeShare = req.HasEmployeeShare;
        rule.EmployerSharePercent = req.EmployerSharePercent;
        rule.EmployeeSharePercent = req.EmployeeSharePercent;
        rule.AutoCompute = req.AutoCompute;
        rule.ComputeBasedOn = req.ComputeBasedOn;
        rule.EffectiveFrom = req.EffectiveFrom;
        rule.MaxDeductionAmount = req.MaxDeductionAmount;
        rule.ScopeTagsJson = SerializeTags(req.ScopeTags);
        rule.Notes = req.Notes ?? string.Empty;
        rule.IsActive = req.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpDelete("deductions/{ruleId:int}")]
    public async Task<IActionResult> DeleteDeduction(int orgId, int ruleId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var rule = await _db.DeductionRules.FirstOrDefaultAsync(r => r.DeductionRuleID == ruleId && r.OrgID == orgId);
        if (rule == null) return NotFound(new { ok = false, message = "Deduction rule not found." });

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    #endregion

    private static string SerializeTags(IEnumerable<string>? tags)
    {
        var list = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        return JsonSerializer.Serialize(list, JsonOptions);
    }

    private static List<string> DeserializeTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

public class EarningRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string AppliesTo { get; set; } = "OT";
    public string DayType { get; set; } = "Any";
    public string HolidayCombo { get; set; } = "Standard";
    public string RestDayHandling { get; set; } = "FollowAttendance";
    public string Scope { get; set; } = "AllEmployees";
    public IEnumerable<string>? ScopeTags { get; set; }
        = Array.Empty<string>();
    public string RateType { get; set; } = "Multiplier";
    public decimal RateValue { get; set; } = 1.00m;
    public bool IsTaxable { get; set; } = true;
    public bool IncludeInBenefitBase { get; set; } = false;
    public bool RequiresApproval { get; set; } = false;
    public DateTime? EffectiveFrom { get; set; }
        = null;
    public DateTime? EffectiveTo { get; set; }
        = null;
    public string? Notes { get; set; }
        = string.Empty;
}

public class AllowanceRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string AllowanceType { get; set; } = "FixedPerPayroll";
    public string Category { get; set; } = "DeMinimis";
    public decimal Amount { get; set; }
        = 0m;
    public bool IsTaxable { get; set; }
        = false;
    public bool AttendanceDependent { get; set; }
        = false;
    public bool ProrateIfPartialPeriod { get; set; }
        = false;
    public DateTime? EffectiveFrom { get; set; }
        = null;
    public DateTime? EffectiveTo { get; set; }
        = null;
    public bool IsActive { get; set; }
        = true;
    public IEnumerable<string>? ScopeTags { get; set; }
        = Array.Empty<string>();
    public string? ComplianceNotes { get; set; }
        = string.Empty;
}

public class DeductionRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Statutory";
    public string DeductionType { get; set; } = "Fixed";
    public string ComputeBasedOn { get; set; } = "BasicPay";
    public decimal? Amount { get; set; }
        = null;
    public string? FormulaExpression { get; set; }
        = string.Empty;
    public bool HasEmployerShare { get; set; }
        = false;
    public bool HasEmployeeShare { get; set; }
        = true;
    public decimal? EmployerSharePercent { get; set; }
        = null;
    public decimal? EmployeeSharePercent { get; set; }
        = null;
    public bool AutoCompute { get; set; }
        = true;
    public bool IsActive { get; set; }
        = true;
    public DateTime? EffectiveFrom { get; set; }
        = null;
    public decimal? MaxDeductionAmount { get; set; }
        = null;
    public IEnumerable<string>? ScopeTags { get; set; }
        = Array.Empty<string>();
    public string? Notes { get; set; }
        = string.Empty;
}
