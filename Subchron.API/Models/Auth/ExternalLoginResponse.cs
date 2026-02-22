namespace Subchron.API.Models.Auth;

public class ExternalLoginResponse
{
    public bool Ok { get; set; }
    public string? Message { get; set; }

    public bool RequiresSignup { get; set; }

    public int UserId { get; set; }
    public int? OrgId { get; set; }
    public string? OrgName { get; set; }
    public string? Role { get; set; }

    public string? Email { get; set; }
    public string? Name { get; set; }

    public string? Jwt { get; set; }
    public string? Token { get; set; }

    /// <summary>When true, do not complete login; redirect to Login with totpIntentToken and show TOTP step.</summary>
    public bool RequiresTotp { get; set; }
    /// <summary>Short-lived token to pass to verify-external-totp with the 6-digit code.</summary>
    public string? TotpIntentToken { get; set; }
}
