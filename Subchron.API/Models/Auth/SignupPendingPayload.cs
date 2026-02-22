namespace Subchron.API.Models.Auth;

// Data we store in SignupPending while the user completes payment before we create the account.
public class SignupPendingPayload
{
    public string OrgName { get; set; } = null!;
    public string OrgCode { get; set; } = null!;
    public int PlanId { get; set; }
    public string AttendanceMode { get; set; } = "QR";
    public string BillingCycle { get; set; } = "Monthly";
    public string AdminName { get; set; } = null!;
    public string AdminEmail { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    public bool IsExternal { get; set; }
}
