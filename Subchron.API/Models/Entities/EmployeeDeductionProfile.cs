namespace Subchron.API.Models.Entities;

public class EmployeeDeductionProfile
{
    public int EmployeeDeductionProfileID { get; set; }
    public int OrgID { get; set; }
    public int EmpID { get; set; }
    public int DeductionRuleID { get; set; }
    public string Mode { get; set; } = "UseRule";
    public decimal? Value { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
