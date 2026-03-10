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

    public LeaveCategory LeaveCategory { get; set; } = LeaveCategory.CompanyPolicy;
    public LeaveCompensationSource CompensationSource { get; set; } = LeaveCompensationSource.CompanyPaid;
    public LeavePaidStatus PaidStatus { get; set; } = LeavePaidStatus.Paid;
    public LeaveStatutoryCode StatutoryCode { get; set; } = LeaveStatutoryCode.None;
    public int MinServiceMonths { get; set; }
    public int AdvanceFilingDays { get; set; }
    public bool AllowRetroactiveFiling { get; set; }
    public int MaxConsecutiveDays { get; set; }
    public LeaveFilingUnit FilingUnit { get; set; } = LeaveFilingUnit.FullDay;
    public LeaveDeductionTiming DeductBalanceOn { get; set; } = LeaveDeductionTiming.UponApproval;
    public LeaveApproverRole ApproverRole { get; set; } = LeaveApproverRole.Supervisor;
    public LeaveExpiryRule LeaveExpiryRule { get; set; } = LeaveExpiryRule.Never;
    public int? LeaveExpiryCustomMonths { get; set; }
    public bool AllowLeaveOnRestDay { get; set; }
    public bool AllowLeaveOnHoliday { get; set; }
    public bool RequiresLegalQualification { get; set; }
    public bool RequiresHrValidation { get; set; }
    public bool CanOrgOverride { get; set; } = true;
    public bool IsSystemProtected { get; set; }
    public string? TemplateKey { get; set; }
    public string? ColorHex { get; set; }
}
