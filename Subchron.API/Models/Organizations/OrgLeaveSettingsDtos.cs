using Subchron.API.Models.LeaveSettings;

namespace Subchron.API.Models.Organizations;

public class OrgLeaveSettingsResponse
{
    public int OrgId { get; set; }
    public LeaveFiscalYearStart FiscalYearStart { get; set; }
    public LeaveBalanceResetRule BalanceResetRule { get; set; }
    public bool ProratedForNewHires { get; set; }
}

public class OrgLeaveSettingsUpdateRequest
{
    public LeaveFiscalYearStart FiscalYearStart { get; set; } = LeaveFiscalYearStart.January1;
    public LeaveBalanceResetRule BalanceResetRule { get; set; } = LeaveBalanceResetRule.FiscalYearStart;
    public bool ProratedForNewHires { get; set; } = true;
}
