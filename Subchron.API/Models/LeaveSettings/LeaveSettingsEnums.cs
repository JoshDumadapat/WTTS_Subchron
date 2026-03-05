namespace Subchron.API.Models.LeaveSettings;

public enum LeaveFiscalYearStart
{
    January1 = 1,
    April1 = 2,
    July1 = 3,
    EmployeeHireDate = 4
}

public enum LeaveBalanceResetRule
{
    FiscalYearStart = 1,
    EmployeeAnniversary = 2,
    None = 3
}
