namespace Subchron.API.Models.Auth;

public class VerifyEmailCodeRequest
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
}
