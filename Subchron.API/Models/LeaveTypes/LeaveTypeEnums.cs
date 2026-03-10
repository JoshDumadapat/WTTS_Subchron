namespace Subchron.API.Models.LeaveTypes;

public enum LeaveAccrualType
{
    LumpSum = 1,
    Monthly = 2,
    PerPayPeriod = 3
}

public enum LeaveCarryOverType
{
    None = 1,
    MaxDays = 2
}

public enum LeaveAppliesTo
{
    All = 1,
    FullTime = 2,
    PartTime = 3,
    Probationary = 4,
    Regular = 5,
    FemaleOnly = 6,
    MaleOnly = 7
}

public enum LeaveCategory
{
    Statutory = 1,
    CompanyPolicy = 2,
    UnpaidAdministrative = 3
}

public enum LeaveCompensationSource
{
    CompanyPaid = 1,
    GovernmentBenefit = 2,
    Unpaid = 3,
    Mixed = 4
}

public enum LeavePaidStatus
{
    Paid = 1,
    Unpaid = 2,
    Conditional = 3
}

public enum LeaveStatutoryCode
{
    None = 0,
    ServiceIncentiveLeave = 1,
    Maternity = 2,
    Paternity = 3,
    SoloParent = 4,
    ViolenceAgainstWomenChildren = 5,
    SpecialLeaveForWomen = 6
}

public enum LeaveFilingUnit
{
    FullDay = 1,
    HalfDay = 2,
    Hourly = 3
}

public enum LeaveDeductionTiming
{
    UponApproval = 1,
    OnLeaveDate = 2
}

public enum LeaveApproverRole
{
    Supervisor = 1,
    Manager = 2,
    Hr = 3,
    AutoApprove = 4
}

public enum LeaveExpiryRule
{
    Never = 1,
    FiscalYearEnd = 2,
    CustomMonths = 3
}
