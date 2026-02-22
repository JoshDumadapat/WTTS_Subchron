namespace Subchron.API.Models.Auth;

public class SignupResponse
{
    public bool Ok { get; set; }
    public string? Message { get; set; }

    public int OrgId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "OrgAdmin";
    public string? Token { get; set; }

    // When true, the client should send the user to the billing page to pay before the account is created.
    public bool RequiresBilling { get; set; }
    public string? SignupToken { get; set; }
    public decimal? Amount { get; set; }
    public string? PlanName { get; set; }
}
