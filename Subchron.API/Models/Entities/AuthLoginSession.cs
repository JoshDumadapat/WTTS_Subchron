namespace Subchron.API.Models.Entities;

public class AuthLoginSession
{
    public Guid SessionID { get; set; }

    public int UserID { get; set; }
    public User User { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
