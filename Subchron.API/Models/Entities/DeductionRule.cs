namespace Subchron.API.Models.Entities;

/// <summary>
/// A configurable deduction rule applied to employee payroll.
/// Supports fixed amounts, percentages, and formula-based deductions.
/// Global — not hardcoded to any country's contribution scheme.
/// </summary>
public class DeductionRule
{
    public int DeductionRuleID { get; set; }
    public int OrgID { get; set; }
    public Organization Organization { get; set; } = null!;

    /// <summary>Display name, e.g. "Income Tax", "SSS", "Health Insurance"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// How the deduction amount is computed.
    /// Values: Fixed | Percentage | Formula
    /// </summary>
    public string DeductionType { get; set; } = "Fixed";

    /// <summary>
    /// For Fixed: the flat amount per pay period.
    /// For Percentage: the rate (e.g. 4.5 = 4.5%).
    /// For Formula: ignored — formula expression used instead.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Formula expression string (for Formula type), e.g. "progressive_tax(gross)"
    /// Null for Fixed/Percentage types.
    /// </summary>
    public string? FormulaExpression { get; set; }

    public bool HasEmployerShare { get; set; } = false;
    public bool HasEmployeeShare { get; set; } = true;

    /// <summary>
    /// Employer share percentage (0–100). Only relevant when HasEmployerShare = true.
    /// </summary>
    public decimal? EmployerSharePercent { get; set; }

    /// <summary>
    /// Employee share percentage (0–100). Only relevant when HasEmployeeShare = true.
    /// For Fixed type this is ignored; Amount is used directly.
    /// </summary>
    public decimal? EmployeeSharePercent { get; set; }

    /// <summary>If true, system computes this automatically; otherwise admin inputs manually.</summary>
    public bool AutoCompute { get; set; } = true;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
