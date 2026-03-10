using Subchron.API.Models.LeaveSettings;

namespace Subchron.API.Models.Entities;

public class OrgLeaveConfig
{
    public int OrgID { get; set; }
    public LeaveFiscalYearStart FiscalYearStart { get; set; } = LeaveFiscalYearStart.January1;
    public LeaveBalanceResetRule BalanceResetRule { get; set; } = LeaveBalanceResetRule.FiscalYearStart;
    public bool ProratedForNewHires { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
