namespace Subchron.API.Models.Entities;

// Org’s saved payment method; we only keep PayMongo ids and last4/brand for display, no full card.
public class OrganizationPaymentMethod
{
    public int Id { get; set; }
    public int OrgID { get; set; }
    public Organization Organization { get; set; } = null!;

    // PayMongo payment method id (e.g. pm_xxx).
    public string? PayMongoPaymentMethodId { get; set; }
    // PayMongo customer id when using the customer API.
    public string? PayMongoCustomerId { get; set; }

    public string Type { get; set; } = "card"; // card, gcash, paymaya
    public string? Last4 { get; set; }
    public string? Brand { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}
