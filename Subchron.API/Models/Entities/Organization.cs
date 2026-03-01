namespace Subchron.API.Models.Entities;

public class Organization
{
    public int OrgID { get; set; }
    public string OrgName { get; set; } = null!;
    public string OrgCode { get; set; } = null!;
    public string Status { get; set; } = "Trial";
    public DateTime CreatedAt { get; set; }

    public OrganizationSettings? Settings { get; set; }
    public OrganizationProfile? Profile { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
}
