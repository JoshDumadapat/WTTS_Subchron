namespace Subchron.API.Models.Entities;

/// <summary>Every PayMongo payment attempt; status reflects provider result. Updated by webhook and complete-signup.</summary>
public class PaymentTransaction
{
    public int Id { get; set; }
    public int? OrgID { get; set; }
    public Organization? Organization { get; set; }
    public int? UserID { get; set; }
    public User? User { get; set; }
    public int? SubscriptionID { get; set; }
    public Subscription? Subscription { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PHP";
    /// <summary>Normalized: paid, pending, failed, expired, refunded. Only "paid" grants subscription access.</summary>
    public string Status { get; set; } = null!;
    public string? PayMongoPaymentIntentId { get; set; }
    public string? PayMongoPaymentId { get; set; }
    public string? Description { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
}
