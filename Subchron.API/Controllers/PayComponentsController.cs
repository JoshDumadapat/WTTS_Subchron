using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/organizations/{orgId:int}/pay-components")]
[Authorize]
public class PayComponentsController : ControllerBase
{
    private readonly SubchronDbContext _db;

    public PayComponentsController(SubchronDbContext db)
    {
        _db = db;
    }

    // ─── Guard helper ─────────────────────────────────────────────────────────
    private bool CanAccessOrg(int orgId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (role == "SuperAdmin") return true;
        var claim = User.FindFirstValue("orgId");
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id) && id == orgId;
    }

    // =========================================================================
    // EARNING RULES
    // =========================================================================

    // GET api/organizations/{orgId}/pay-components/earnings
    [HttpGet("earnings")]
    public async Task<IActionResult> GetEarnings(int orgId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var rules = await _db.EarningRules
            .AsNoTracking()
            .Where(r => r.OrgID == orgId && r.IsActive)
            .OrderBy(r => r.EarningRuleID)
            .Select(r => new
            {
                r.EarningRuleID,
                r.Name,
                r.AppliesTo,
                r.RateType,
                r.RateValue,
                r.IsTaxable,
                r.IncludeInBenefitBase,
                r.RequiresApproval,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(rules);
    }

    // POST api/organizations/{orgId}/pay-components/earnings
    [HttpPost("earnings")]
    public async Task<IActionResult> CreateEarning(int orgId, [FromBody] EarningRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var rule = new EarningRule
        {
            OrgID = orgId,
            Name = req.Name.Trim(),
            AppliesTo = req.AppliesTo,
            RateType = req.RateType,
            RateValue = req.RateValue,
            IsTaxable = req.IsTaxable,
            IncludeInBenefitBase = req.IncludeInBenefitBase,
            RequiresApproval = req.RequiresApproval,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.EarningRules.Add(rule);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, id = rule.EarningRuleID });
    }

    // PUT api/organizations/{orgId}/pay-components/earnings/{ruleId}
    [HttpPut("earnings/{ruleId:int}")]
    public async Task<IActionResult> UpdateEarning(int orgId, int ruleId, [FromBody] EarningRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var rule = await _db.EarningRules.FirstOrDefaultAsync(r => r.EarningRuleID == ruleId && r.OrgID == orgId);
        if (rule is null) return NotFound(new { ok = false, message = "Earning rule not found." });

        rule.Name = req.Name.Trim();
        rule.AppliesTo = req.AppliesTo;
        rule.RateType = req.RateType;
        rule.RateValue = req.RateValue;
        rule.IsTaxable = req.IsTaxable;
        rule.IncludeInBenefitBase = req.IncludeInBenefitBase;
        rule.RequiresApproval = req.RequiresApproval;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // DELETE api/organizations/{orgId}/pay-components/earnings/{ruleId}
    [HttpDelete("earnings/{ruleId:int}")]
    public async Task<IActionResult> DeleteEarning(int orgId, int ruleId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var rule = await _db.EarningRules.FirstOrDefaultAsync(r => r.EarningRuleID == ruleId && r.OrgID == orgId);
        if (rule is null) return NotFound(new { ok = false, message = "Earning rule not found." });

        // Soft delete
        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }

    // =========================================================================
    // DEDUCTION RULES
    // =========================================================================

    // GET api/organizations/{orgId}/pay-components/deductions
    [HttpGet("deductions")]
    public async Task<IActionResult> GetDeductions(int orgId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var rules = await _db.DeductionRules
            .AsNoTracking()
            .Where(r => r.OrgID == orgId && r.IsActive)
            .OrderBy(r => r.DeductionRuleID)
            .Select(r => new
            {
                r.DeductionRuleID,
                r.Name,
                r.DeductionType,
                r.Amount,
                r.FormulaExpression,
                r.HasEmployerShare,
                r.HasEmployeeShare,
                r.EmployerSharePercent,
                r.EmployeeSharePercent,
                r.AutoCompute,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(rules);
    }

    // POST api/organizations/{orgId}/pay-components/deductions
    [HttpPost("deductions")]
    public async Task<IActionResult> CreateDeduction(int orgId, [FromBody] DeductionRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var rule = new DeductionRule
        {
            OrgID = orgId,
            Name = req.Name.Trim(),
            DeductionType = req.DeductionType,
            Amount = req.Amount,
            FormulaExpression = req.FormulaExpression?.Trim(),
            HasEmployerShare = req.HasEmployerShare,
            HasEmployeeShare = req.HasEmployeeShare,
            EmployerSharePercent = req.EmployerSharePercent,
            EmployeeSharePercent = req.EmployeeSharePercent,
            AutoCompute = req.AutoCompute,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.DeductionRules.Add(rule);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, id = rule.DeductionRuleID });
    }

    // PUT api/organizations/{orgId}/pay-components/deductions/{ruleId}
    [HttpPut("deductions/{ruleId:int}")]
    public async Task<IActionResult> UpdateDeduction(int orgId, int ruleId, [FromBody] DeductionRuleRequest req)
    {
        if (!CanAccessOrg(orgId)) return Forbid();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var rule = await _db.DeductionRules.FirstOrDefaultAsync(r => r.DeductionRuleID == ruleId && r.OrgID == orgId);
        if (rule is null) return NotFound(new { ok = false, message = "Deduction rule not found." });

        rule.Name = req.Name.Trim();
        rule.DeductionType = req.DeductionType;
        rule.Amount = req.Amount;
        rule.FormulaExpression = req.FormulaExpression?.Trim();
        rule.HasEmployerShare = req.HasEmployerShare;
        rule.HasEmployeeShare = req.HasEmployeeShare;
        rule.EmployerSharePercent = req.EmployerSharePercent;
        rule.EmployeeSharePercent = req.EmployeeSharePercent;
        rule.AutoCompute = req.AutoCompute;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // DELETE api/organizations/{orgId}/pay-components/deductions/{ruleId}
    [HttpDelete("deductions/{ruleId:int}")]
    public async Task<IActionResult> DeleteDeduction(int orgId, int ruleId)
    {
        if (!CanAccessOrg(orgId)) return Forbid();

        var rule = await _db.DeductionRules.FirstOrDefaultAsync(r => r.DeductionRuleID == ruleId && r.OrgID == orgId);
        if (rule is null) return NotFound(new { ok = false, message = "Deduction rule not found." });

        // Soft delete
        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public class EarningRuleRequest
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Regular | OT | Holiday | RestDay | NightDiff | Custom</summary>
    public string AppliesTo { get; set; } = "OT";
    /// <summary>Multiplier | Percentage | Fixed</summary>
    public string RateType { get; set; } = "Multiplier";
    public decimal RateValue { get; set; } = 1.00m;
    public bool IsTaxable { get; set; } = true;
    public bool IncludeInBenefitBase { get; set; } = false;
    public bool RequiresApproval { get; set; } = false;
}

public class DeductionRuleRequest
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Fixed | Percentage | Formula</summary>
    public string DeductionType { get; set; } = "Fixed";
    public decimal? Amount { get; set; }
    public string? FormulaExpression { get; set; }
    public bool HasEmployerShare { get; set; } = false;
    public bool HasEmployeeShare { get; set; } = true;
    public decimal? EmployerSharePercent { get; set; }
    public decimal? EmployeeSharePercent { get; set; }
    public bool AutoCompute { get; set; } = true;
}
