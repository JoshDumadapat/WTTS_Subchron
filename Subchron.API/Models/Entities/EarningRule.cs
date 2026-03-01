namespace Subchron.API.Models.Entities;

/// <summary>
/// A configurable earning rule that applies a multiplier, percentage premium,
/// or fixed amount to employee pay for a given work category (OT, Holiday, etc.).
/// </summary>
public class EarningRule
{
    public int EarningRuleID { get; set; }
    public int OrgID { get; set; }
    public Organization Organization { get; set; } = null!;

    /// <summary>Display name, e.g. "Regular Overtime", "Night Differential"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category this rule applies to.
    /// Values: Regular | OT | Holiday | RestDay | NightDiff | Custom
    /// </summary>
    public string AppliesTo { get; set; } = "OT";

    /// <summary>
    /// How the rate is expressed.
    /// Values: Multiplier | Percentage | Fixed
    /// </summary>
    public string RateType { get; set; } = "Multiplier";

    /// <summary>
    /// Numeric value of the rate.
    /// For Multiplier: 1.25 means 1.25x base pay.
    /// For Percentage: 10 means +10% of base pay.
    /// For Fixed: flat amount added per applicable hour/day.
    /// </summary>
    public decimal RateValue { get; set; } = 1.00m;

    public bool IsTaxable { get; set; } = true;
    public bool IncludeInBenefitBase { get; set; } = false;
    public bool RequiresApproval { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
