using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.SuperAdmin.Plans
{
    public class IndexModel : PageModel
    {
      public List<PlanViewModel> Plans { get; set; } = new();
        private Dictionary<int, int> _subscriptionCounts = new();

      public void OnGet()
        {
  LoadPlans();
            LoadSubscriptionCounts();
        }

private void LoadPlans()
  {
      // Mock data - replace with actual repository calls
  Plans = new List<PlanViewModel>
      {
        new()
       {
    PlanID = 1,
      PlanName = "Basic",
    BasePrice = 1500,
  MaxEmployees = 25,
   RetentionMonths = 6,
     IsActive = true
     },
       new()
            {
     PlanID = 2,
    PlanName = "Standard",
     BasePrice = 2500,
        MaxEmployees = 100,
     RetentionMonths = 12,
       IsActive = true
     },
      new()
       {
   PlanID = 3,
      PlanName = "Enterprise",
     BasePrice = 5000,
     MaxEmployees = 0, // Unlimited
   RetentionMonths = 24,
     IsActive = true
     },
      new()
     {
     PlanID = 4,
    PlanName = "Legacy Basic",
       BasePrice = 1200,
    MaxEmployees = 20,
     RetentionMonths = 3,
    IsActive = false
      }
        };
   }

 private void LoadSubscriptionCounts()
   {
     // Mock data - replace with actual repository calls
   _subscriptionCounts = new Dictionary<int, int>
        {
  { 1, 8 },   // Basic: 8 subscriptions
   { 2, 15 },  // Standard: 15 subscriptions
    { 3, 5 },   // Enterprise: 5 subscriptions
      { 4, 2 }    // Legacy Basic: 2 subscriptions
        };
    }

   public int GetSubscriptionCount(int planId)
  {
    return _subscriptionCounts.TryGetValue(planId, out int count) ? count : 0;
        }
    }

  public class PlanViewModel
    {
        public int PlanID { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public int MaxEmployees { get; set; }
      public int RetentionMonths { get; set; }
     public bool IsActive { get; set; }
    }
}