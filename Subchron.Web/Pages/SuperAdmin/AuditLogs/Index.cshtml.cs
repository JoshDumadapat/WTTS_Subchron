using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.SuperAdmin.AuditLogs
{
    public class IndexModel : PageModel
 {
        public List<AuditLogViewModel> AuditLogs { get; set; } = new();
  public Dictionary<string, string> Organizations { get; set; } = new();

   public void OnGet(string? orgId = null, string? action = null, DateTime? startDate = null, DateTime? endDate = null)
   {
       LoadOrganizations();
   LoadAuditLogs(orgId, action, startDate, endDate);
   }

        private void LoadOrganizations()
   {
      // Mock data - replace with actual repository calls
            Organizations = new Dictionary<string, string>
     {
    { "1", "TechCorp Solutions" },
    { "2", "Digital Ventures" },
      { "3", "Innovation Labs" },
   { "4", "StartupXYZ" },
     { "5", "Global Services Inc" }
  };
      }

   private void LoadAuditLogs(string? orgId = null, string? action = null, DateTime? startDate = null, DateTime? endDate = null)
 {
    // Mock data - replace with actual repository calls
AuditLogs = new List<AuditLogViewModel>
     {
         new()
   {
  AuditID = 1,
       OrgID = 1,
     OrgName = "TechCorp Solutions",
     Action = "CREATE_ORG",
       EntityName = "Organizations",
      EntityID = 1,
       Details = "Organization 'TechCorp Solutions' created with code TECH001",
        CreatedAt = DateTime.Now.AddHours(-2)
      },
        new()
     {
 AuditID = 2,
   OrgID = 2,
      OrgName = "Digital Ventures",
      Action = "APPROVE_DEMO_REQUEST",
        EntityName = "DemoRequests",
      EntityID = 2,
    Details = "Demo request #2 approved and organization created",
    CreatedAt = DateTime.Now.AddHours(-4)
   },
new()
     {
 AuditID = 3,
         OrgID = 1,
      OrgName = "TechCorp Solutions",
      Action = "UPDATE_SUBSCRIPTION",
      EntityName = "Subscriptions",
        EntityID = 1,
      Details = "Subscription updated: Plan changed to Enterprise, Mode changed to Biometric",
    CreatedAt = DateTime.Now.AddHours(-6)
      },
    new()
    {
   AuditID = 4,
OrgID = null,
      OrgName = null,
         Action = "CREATE_PLAN",
       EntityName = "Plans",
  EntityID = 4,
         Details = "New plan 'Premium' created with base price ?3500",
      CreatedAt = DateTime.Now.AddHours(-8)
   },
    new()
    {
        AuditID = 5,
      OrgID = 4,
      OrgName = "StartupXYZ",
     Action = "SUSPEND_ORG",
 EntityName = "Organizations",
   EntityID = 4,
 Details = "Organization suspended due to payment failure",
   CreatedAt = DateTime.Now.AddHours(-10)
      },
        new()
 {
       AuditID = 6,
       OrgID = 3,
  OrgName = "Innovation Labs",
   Action = "ACTIVATE_SUBSCRIPTION",
   EntityName = "Subscriptions",
     EntityID = 3,
   Details = "Trial subscription activated to Enterprise plan",
     CreatedAt = DateTime.Now.AddHours(-12)
   },
     new()
   {
       AuditID = 7,
 OrgID = null,
     OrgName = null,
      Action = "REJECT_DEMO_REQUEST",
     EntityName = "DemoRequests",
        EntityID = 4,
Details = "Demo request #4 rejected - insufficient requirements",
    CreatedAt = DateTime.Now.AddDays(-1)
     },
     new()
    {
    AuditID = 8,
 OrgID = 5,
    OrgName = "Global Services Inc",
  Action = "CREATE_ORG",
     EntityName = "Organizations",
      EntityID = 5,
     Details = "Organization 'Global Services Inc' created with code GLOB005",
  CreatedAt = DateTime.Now.AddDays(-1).AddHours(-2)
      },
       new()
     {
      AuditID = 9,
        OrgID = 2,
   OrgName = "Digital Ventures",
        Action = "UPDATE_ORG",
         EntityName = "Organizations",
     EntityID = 2,
     Details = "Organization status updated from Trial to Active",
     CreatedAt = DateTime.Now.AddDays(-1).AddHours(-4)
    },
      new()
      {
     AuditID = 10,
  OrgID = null,
       OrgName = null,
       Action = "UPDATE_PLAN",
        EntityName = "Plans",
     EntityID = 1,
    Details = "Basic plan price updated from ?1200 to ?1500",
   CreatedAt = DateTime.Now.AddDays(-1).AddHours(-6)
    },
       new()
     {
      AuditID = 11,
    OrgID = 1,
   OrgName = "TechCorp Solutions",
   Action = "CREATE_SUBSCRIPTION",
   EntityName = "Subscriptions",
       EntityID = 1,
         Details = "Trial subscription created with 14-day period",
      CreatedAt = DateTime.Now.AddDays(-2)
        },
     new()
   {
   AuditID = 12,
     OrgID = 3,
     OrgName = "Innovation Labs",
     Action = "UPDATE_SUBSCRIPTION",
       EntityName = "Subscriptions",
     EntityID = 3,
          Details = "Subscription billing cycle changed to Annual",
    CreatedAt = DateTime.Now.AddDays(-2).AddHours(-3)
    }
 };

      // Apply filters if provided
     if (!string.IsNullOrEmpty(orgId))
  {
       AuditLogs = AuditLogs.Where(log => log.OrgID?.ToString() == orgId).ToList();
    }

      if (!string.IsNullOrEmpty(action))
      {
 AuditLogs = AuditLogs.Where(log => log.Action == action).ToList();
       }

      if (startDate.HasValue)
     {
       AuditLogs = AuditLogs.Where(log => log.CreatedAt.Date >= startDate.Value.Date).ToList();
  }

    if (endDate.HasValue)
    {
       AuditLogs = AuditLogs.Where(log => log.CreatedAt.Date <= endDate.Value.Date).ToList();
   }

        // Sort by most recent first
AuditLogs = AuditLogs.OrderByDescending(log => log.CreatedAt).ToList();
     }
    }

    public class AuditLogViewModel
    {
      public int AuditID { get; set; }
        public int? OrgID { get; set; }
        public string? OrgName { get; set; }
    public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
 public int? EntityID { get; set; }
      public string Details { get; set; } = string.Empty;
      public DateTime CreatedAt { get; set; }
    }
}