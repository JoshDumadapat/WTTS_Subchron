namespace Subchron.API.Models.Entities;

public class BillingRecord
{
    public int Id { get; set; }
    public int OrgID { get; set; }
    public Organization Organization { get; set; } = null!;
    public int UserID { get; set; }
    public User User { get; set; } = null!;

    public string? Last4 { get; set; }    
    public string? Expiry { get; set; }     
    public string? Brand { get; set; }        
    public string? BillingEmail { get; set; }
    public string? BillingPhone { get; set; }   
    public string? PayMongoPaymentMethodId { get; set; }
    public string? PayMongoCustomerId { get; set; }
    public string? NameOnCard { get; set; }
    public int? PaymentTransactionId { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }

    public DateTime CreatedAt { get; set; }
}
