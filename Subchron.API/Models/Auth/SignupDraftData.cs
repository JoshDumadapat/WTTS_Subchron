namespace Subchron.API.Models.Auth;

// Signup data kept in the draft until billing is done; not saved to the database yet.
public class SignupDraftData
{
    public string OrgName { get; set; } = "";
    public string OrgCode { get; set; } = "";
    public int PlanId { get; set; }
    public string AttendanceMode { get; set; } = "QR";
    public string BillingCycle { get; set; } = "Monthly";
    public string AdminName { get; set; } = "";
    public string AdminEmail { get; set; } = "";
    public string Password { get; set; } = "";
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
}
