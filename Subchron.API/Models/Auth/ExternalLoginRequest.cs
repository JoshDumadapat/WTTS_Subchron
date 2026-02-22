namespace Subchron.API.Models.Auth;

public class ExternalLoginRequest
{
    public string Provider { get; set; } = null!;
    public string ExternalId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
}
