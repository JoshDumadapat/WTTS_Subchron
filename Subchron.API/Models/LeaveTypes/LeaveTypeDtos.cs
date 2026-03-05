using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.LeaveTypes;

public class LeaveTypeDto
{
    public int LeaveTypeID { get; set; }
    public int OrgID { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public decimal DefaultDaysPerYear { get; set; }
    public bool IsPaid { get; set; }
    public bool IsActive { get; set; }
    public LeaveAccrualType AccrualType { get; set; }
    public LeaveCarryOverType CarryOverType { get; set; }
    public int? CarryOverMaxDays { get; set; }
    public LeaveAppliesTo AppliesTo { get; set; }
    public bool RequireApproval { get; set; }
    public bool RequireDocument { get; set; }
    public bool AllowNegativeBalance { get; set; }
}

public class CreateLeaveTypeRequest
{
    [Required, MaxLength(50)]
    public string LeaveTypeName { get; set; } = string.Empty;

    [Range(0, 365)]
    public decimal DefaultDaysPerYear { get; set; }

    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public LeaveAccrualType AccrualType { get; set; } = LeaveAccrualType.LumpSum;
    public LeaveCarryOverType CarryOverType { get; set; } = LeaveCarryOverType.None;
    public int? CarryOverMaxDays { get; set; }
    public LeaveAppliesTo AppliesTo { get; set; } = LeaveAppliesTo.All;
    public bool RequireApproval { get; set; } = true;
    public bool RequireDocument { get; set; }
    public bool AllowNegativeBalance { get; set; }
}

public class UpdateLeaveTypeRequest
{
    [Required, MaxLength(50)]
    public string LeaveTypeName { get; set; } = string.Empty;

    [Range(0, 365)]
    public decimal DefaultDaysPerYear { get; set; }

    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public LeaveAccrualType AccrualType { get; set; } = LeaveAccrualType.LumpSum;
    public LeaveCarryOverType CarryOverType { get; set; } = LeaveCarryOverType.None;
    public int? CarryOverMaxDays { get; set; }
    public LeaveAppliesTo AppliesTo { get; set; } = LeaveAppliesTo.All;
    public bool RequireApproval { get; set; } = true;
    public bool RequireDocument { get; set; }
    public bool AllowNegativeBalance { get; set; }
}
