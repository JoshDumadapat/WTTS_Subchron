using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.SuperAdmin.Organizations
{
    public class DetailsModel : PageModel
    {
        public OrganizationDetailViewModel Organization { get; set; } = new();
  public OrganizationSettingsViewModel Settings { get; set; } = new();
  public SubscriptionDetailViewModel? CurrentSubscription { get; set; }
 public int EmployeeCount { get; set; }
  public int ActiveUsers { get; set; }
     public DateTime? LastActivity { get; set; }
        public decimal StorageUsed { get; set; }
   public int ApiCallsThisMonth { get; set; }
 public int AttendanceRecords { get; set; }

 public async Task<IActionResult> OnGetAsync(int id)
        {
          var organization = await LoadOrganization(id);
  if (organization == null)
    {
     return NotFound();
    }

      Organization = organization;
  Settings = await LoadOrganizationSettings(id);
          CurrentSubscription = await LoadCurrentSubscription(id);
      await LoadStatistics(id);
       
            return Page();
      }

 private async Task<OrganizationDetailViewModel?> LoadOrganization(int id)
   {
            // Mock data - replace with actual repository call
await Task.Delay(10);

       var mockOrganizations = new List<OrganizationDetailViewModel>
    {
     new()
      {
        OrgID = 1,
     OrgName = "TechCorp Solutions",
      OrgCode = "TECH001",
 Status = "Active",
      CreatedAt = DateTime.Now.AddMonths(-6)
       },
      new()
       {
      OrgID = 2,
  OrgName = "Digital Ventures",
     OrgCode = "DIGI002",
     Status = "Trial",
     CreatedAt = DateTime.Now.AddDays(-10)
          },
      new()
       {
     OrgID = 3,
  OrgName = "Innovation Labs",
      OrgCode = "INNO003",
      Status = "Active",
      CreatedAt = DateTime.Now.AddMonths(-3)
    }
       };

         return mockOrganizations.FirstOrDefault(o => o.OrgID == id);
}

 private async Task<OrganizationSettingsViewModel> LoadOrganizationSettings(int orgId)
        {
    // Mock data - replace with actual repository call
      await Task.Delay(10);

       return new OrganizationSettingsViewModel
  {
        OrgID = orgId,
     Timezone = "Asia/Manila",
   Currency = "PHP",
        AttendanceMode = "Biometric",
       AllowManualEntry = false,
RequireGeo = true,
        EnforceGeofence = false,
    DefaultGraceMinutes = 15,
  RoundRule = "15 minutes",
 OTEnabled = true,
      OTThresholdHours = 8,
        OTApprovalRequired = true,
         OTMaxHoursPerDay = 12,
         LeaveEnabled = true,
    LeaveApprovalRequired = true
    };
  }

 private async Task<SubscriptionDetailViewModel?> LoadCurrentSubscription(int orgId)
 {
   // Mock data - replace with actual repository call
      await Task.Delay(10);

      if (orgId == 1)
        {
         return new SubscriptionDetailViewModel
    {
         SubscriptionID = 1,
  PlanName = "Standard",
  AttendanceMode = "Biometric",
       FinalPrice = 3000,
            BillingCycle = "Monthly",
    StartDate = DateTime.Now.AddMonths(-3),
    EndDate = DateTime.Now.AddMonths(9),
        Status = "Active",
    DaysRemaining = 0
  };
  }
      
        if (orgId == 2)
      {
       return new SubscriptionDetailViewModel
     {
   SubscriptionID = 2,
        PlanName = "Standard",
     AttendanceMode = "QR Code",
      FinalPrice = 2500,
      BillingCycle = "Monthly",
     StartDate = DateTime.Now.AddDays(-10),
    EndDate = DateTime.Now.AddDays(4),
        Status = "Trial",
         DaysRemaining = 4
        };
        }

    return null;
 }

        private async Task LoadStatistics(int orgId)
        {
       // Mock data - replace with actual repository calls
     await Task.Delay(10);
     
        EmployeeCount = 45;
       ActiveUsers = 42;
  LastActivity = DateTime.Now.AddHours(-2);
  StorageUsed = 2.3m;
     ApiCallsThisMonth = 15420;
       AttendanceRecords = 8750;
  }
    }

    public class OrganizationDetailViewModel
    {
       public int OrgID { get; set; }
      public string OrgName { get; set; } = string.Empty;
      public string OrgCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

 public class OrganizationSettingsViewModel
    {
     public int OrgID { get; set; }
   public string Timezone { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
 public string AttendanceMode { get; set; } = string.Empty;
    public bool AllowManualEntry { get; set; }
  public bool RequireGeo { get; set; }
        public bool EnforceGeofence { get; set; }
   public int DefaultGraceMinutes { get; set; }
        public string RoundRule { get; set; } = string.Empty;
  public bool OTEnabled { get; set; }
     public int OTThresholdHours { get; set; }
     public bool OTApprovalRequired { get; set; }
       public int OTMaxHoursPerDay { get; set; }
    public bool LeaveEnabled { get; set; }
      public bool LeaveApprovalRequired { get; set; }
    }

    public class SubscriptionDetailViewModel
    {
     public int SubscriptionID { get; set; }
    public string PlanName { get; set; } = string.Empty;
     public string AttendanceMode { get; set; } = string.Empty;
    public decimal FinalPrice { get; set; }
 public string BillingCycle { get; set; } = string.Empty;
     public DateTime StartDate { get; set; }
   public DateTime EndDate { get; set; }
  public string Status { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
    }
}