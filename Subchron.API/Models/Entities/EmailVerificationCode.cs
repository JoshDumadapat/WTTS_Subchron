namespace Subchron.API.Models.Entities;

// One-time codes sent to email during signup; used to verify the address.
public class EmailVerificationCode
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string CodeHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Used { get; set; }
}
