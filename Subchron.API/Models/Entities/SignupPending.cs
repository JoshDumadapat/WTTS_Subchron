namespace Subchron.API.Models.Entities;

// Holds signup data until payment is done; token goes to billing page, then we create org/user and remove this.
public class SignupPending
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
