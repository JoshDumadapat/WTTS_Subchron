namespace Subchron.API.Models.Entities;

public class Plan
{
    public int PlanID { get; set; }
    public string PlanName { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public int MaxEmployees { get; set; }
    public int RetentionMonths { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
