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
    Regular = 5
}
