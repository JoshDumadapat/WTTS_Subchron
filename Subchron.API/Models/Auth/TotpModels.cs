namespace Subchron.API.Models.Auth;

public sealed class VerifyEnableTotpRequest
{
    public string? TotpCode { get; set; }
}

public sealed class VerifyTotpLoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? TotpCode { get; set; }
}

public sealed class VerifyRecoveryLoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? RecoveryCode { get; set; }
}

public sealed class VerifyExternalTotpRequest
{
    public string? TotpIntentToken { get; set; }
    public string? TotpCode { get; set; }
}

public sealed class VerifyExternalRecoveryRequest
{
    public string? TotpIntentToken { get; set; }
    public string? RecoveryCode { get; set; }
}