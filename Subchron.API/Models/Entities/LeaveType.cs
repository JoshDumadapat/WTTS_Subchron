using Subchron.API.Models.LeaveTypes;

namespace Subchron.API.Models.Entities;

public class LeaveType
{
    public int LeaveTypeID { get; set; }
    public int OrgID { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public decimal DefaultDaysPerYear { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public LeaveAccrualType AccrualType { get; set; } = LeaveAccrualType.LumpSum;
    public LeaveCarryOverType CarryOverType { get; set; } = LeaveCarryOverType.None;
    public int? CarryOverMaxDays { get; set; }
    public LeaveAppliesTo AppliesTo { get; set; } = LeaveAppliesTo.All;
    public bool RequireApproval { get; set; } = true;
    public bool RequireDocument { get; set; } = false;
    public bool AllowNegativeBalance { get; set; } = false;
}
