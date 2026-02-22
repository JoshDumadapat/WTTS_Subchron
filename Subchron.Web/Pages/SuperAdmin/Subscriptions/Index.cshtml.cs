using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.SuperAdmin.Subscriptions
{
    public class IndexModel : PageModel
    {
  public List<SubscriptionViewModel> Subscriptions { get; set; } = new();

        public void OnGet()
{
     LoadSubscriptions();
 }

private void LoadSubscriptions()
        {
  // Mock data - replace with actual repository calls
            Subscriptions = new List<SubscriptionViewModel>
     {
       new()
  {
      SubscriptionID = 1,
        OrgID = 1,
     OrgName = "TechCorp Solutions",
     OrgCode = "TECH001",
      PlanName = "Standard",
   AttendanceMode = "Biometric",
     BasePrice = 2500,
     ModePrice = 500,
      FinalPrice = 3000,
     BillingCycle = "Monthly",
        StartDate = DateTime.Now.AddMonths(-3),
       EndDate = DateTime.Now.AddMonths(9),
    Status = "Active",
      DaysRemaining = 0
    },
 new()
      {
   SubscriptionID = 2,
       OrgID = 2,
       OrgName = "Digital Ventures",
     OrgCode = "DIGI002",
        PlanName = "Standard",
        AttendanceMode = "QR Code",
      BasePrice = 2500,
        ModePrice = 0,
           FinalPrice = 2500,
  BillingCycle = "Monthly",
       StartDate = DateTime.Now.AddDays(-10),
  EndDate = DateTime.Now.AddDays(4),
        Status = "Trial",
    DaysRemaining = 4
      },
       new()
        {
  SubscriptionID = 3,
     OrgID = 3,
         OrgName = "Innovation Labs",
        OrgCode = "INNO003",
         PlanName = "Enterprise",
     AttendanceMode = "Geo Location",
     BasePrice = 5000,
     ModePrice = 1000,
      FinalPrice = 6000,
   BillingCycle = "Annual",
      StartDate = DateTime.Now.AddMonths(-2),
        EndDate = DateTime.Now.AddMonths(10),
 Status = "Active",
    DaysRemaining = 0
   },
        new()
  {
    SubscriptionID = 4,
   OrgID = 4,
   OrgName = "StartupXYZ",
        OrgCode = "STAR004",
 PlanName = "Basic",
    AttendanceMode = "QR Code",
  BasePrice = 1500,
    ModePrice = 0,
     FinalPrice = 1500,
   BillingCycle = "Monthly",
      StartDate = DateTime.Now.AddMonths(-4),
  EndDate = DateTime.Now.AddDays(-30),
 Status = "Expired",
  DaysRemaining = 0
  },
     new()
       {
        SubscriptionID = 5,
    OrgID = 5,
         OrgName = "Global Services Inc",
        OrgCode = "GLOB005",
    PlanName = "Standard",
       AttendanceMode = "QR Code",
     BasePrice = 2500,
ModePrice = 0,
       FinalPrice = 2500,
      BillingCycle = "Monthly",
   StartDate = DateTime.Now.AddDays(-3),
     EndDate = DateTime.Now.AddDays(11),
  Status = "Trial",
      DaysRemaining = 11
  },
      new()
        {
        SubscriptionID = 6,
   OrgID = 6,
      OrgName = "Quick Solutions",
    OrgCode = "QUICK006",
   PlanName = "Standard",
    AttendanceMode = "QR Code",
     BasePrice = 2500,
   ModePrice = 0,
     FinalPrice = 2500,
      BillingCycle = "Monthly",
       StartDate = DateTime.Now.AddDays(-12),
     EndDate = DateTime.Now.AddDays(2),
         Status = "Trial",
 DaysRemaining = 2
 }
  };
  }
    }

    public class SubscriptionViewModel
    {
   public int SubscriptionID { get; set; }
        public int OrgID { get; set; }
   public string OrgName { get; set; } = string.Empty;
       public string OrgCode { get; set; } = string.Empty;
  public string PlanName { get; set; } = string.Empty;
  public string AttendanceMode { get; set; } = string.Empty;
   public decimal BasePrice { get; set; }
      public decimal ModePrice { get; set; }
        public decimal FinalPrice { get; set; }
   public string BillingCycle { get; set; } = string.Empty;
     public DateTime StartDate { get; set; }
      public DateTime EndDate { get; set; }
 public string Status { get; set; } = string.Empty;
  public int DaysRemaining { get; set; }
    }
}