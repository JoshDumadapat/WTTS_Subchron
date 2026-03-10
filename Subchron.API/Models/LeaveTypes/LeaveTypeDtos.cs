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
    public LeaveCategory LeaveCategory { get; set; }
    public LeaveCompensationSource CompensationSource { get; set; }
    public LeavePaidStatus PaidStatus { get; set; }
    public LeaveStatutoryCode StatutoryCode { get; set; }
    public int MinServiceMonths { get; set; }
    public int AdvanceFilingDays { get; set; }
    public bool AllowRetroactiveFiling { get; set; }
    public int MaxConsecutiveDays { get; set; }
    public LeaveFilingUnit FilingUnit { get; set; }
    public LeaveDeductionTiming DeductBalanceOn { get; set; }
    public LeaveApproverRole ApproverRole { get; set; }
    public LeaveExpiryRule LeaveExpiryRule { get; set; }
    public int? LeaveExpiryCustomMonths { get; set; }
    public bool AllowLeaveOnRestDay { get; set; }
    public bool AllowLeaveOnHoliday { get; set; }
    public bool RequiresLegalQualification { get; set; }
    public bool RequiresHrValidation { get; set; }
    public bool CanOrgOverride { get; set; }
    public bool IsSystemProtected { get; set; }
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

    public LeaveCategory LeaveCategory { get; set; } = LeaveCategory.CompanyPolicy;
    public LeaveCompensationSource CompensationSource { get; set; } = LeaveCompensationSource.CompanyPaid;
    public LeavePaidStatus PaidStatus { get; set; } = LeavePaidStatus.Paid;
    public LeaveStatutoryCode StatutoryCode { get; set; } = LeaveStatutoryCode.None;
    [Range(0, 480)]
    public int MinServiceMonths { get; set; }
    [Range(0, 365)]
    public int AdvanceFilingDays { get; set; }
    public bool AllowRetroactiveFiling { get; set; }
    [Range(0, 365)]
    public int MaxConsecutiveDays { get; set; }
    public LeaveFilingUnit FilingUnit { get; set; } = LeaveFilingUnit.FullDay;
    public LeaveDeductionTiming DeductBalanceOn { get; set; } = LeaveDeductionTiming.UponApproval;
    public LeaveApproverRole ApproverRole { get; set; } = LeaveApproverRole.Supervisor;
    public LeaveExpiryRule LeaveExpiryRule { get; set; } = LeaveExpiryRule.Never;
    [Range(1, 120)]
    public int? LeaveExpiryCustomMonths { get; set; }
    public bool AllowLeaveOnRestDay { get; set; }
    public bool AllowLeaveOnHoliday { get; set; }
    public bool RequiresLegalQualification { get; set; }
    public bool RequiresHrValidation { get; set; }
    public bool CanOrgOverride { get; set; } = true;
    public bool IsSystemProtected { get; set; }
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

    public LeaveCategory LeaveCategory { get; set; } = LeaveCategory.CompanyPolicy;
    public LeaveCompensationSource CompensationSource { get; set; } = LeaveCompensationSource.CompanyPaid;
    public LeavePaidStatus PaidStatus { get; set; } = LeavePaidStatus.Paid;
    public LeaveStatutoryCode StatutoryCode { get; set; } = LeaveStatutoryCode.None;
    [Range(0, 480)]
    public int MinServiceMonths { get; set; }
    [Range(0, 365)]
    public int AdvanceFilingDays { get; set; }
    public bool AllowRetroactiveFiling { get; set; }
    [Range(0, 365)]
    public int MaxConsecutiveDays { get; set; }
    public LeaveFilingUnit FilingUnit { get; set; } = LeaveFilingUnit.FullDay;
    public LeaveDeductionTiming DeductBalanceOn { get; set; } = LeaveDeductionTiming.UponApproval;
    public LeaveApproverRole ApproverRole { get; set; } = LeaveApproverRole.Supervisor;
    public LeaveExpiryRule LeaveExpiryRule { get; set; } = LeaveExpiryRule.Never;
    [Range(1, 120)]
    public int? LeaveExpiryCustomMonths { get; set; }
    public bool AllowLeaveOnRestDay { get; set; }
    public bool AllowLeaveOnHoliday { get; set; }
    public bool RequiresLegalQualification { get; set; }
    public bool RequiresHrValidation { get; set; }
    public bool CanOrgOverride { get; set; } = true;
    public bool IsSystemProtected { get; set; }
}
