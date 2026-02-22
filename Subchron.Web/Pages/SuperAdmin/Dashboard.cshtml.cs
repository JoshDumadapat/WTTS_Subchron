using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.SuperAdmin
{
    public class DashboardModel : PageModel
    {
        // Stats properties
        public int TotalOrganizations { get; set; }
        public int TrialOrganizations { get; set; }
        public int ActiveOrganizations { get; set; }
        public int SuspendedOrganizations { get; set; }
        public int PendingDemoRequests { get; set; }
        public int NewOrgsThisMonth { get; set; }
        public int NewActiveThisMonth { get; set; }
        public int TrialsExpiringSoon { get; set; }

        // Data lists
        public List<TrialExpiring> TrialsExpiring { get; set; } = new();
        public List<AuditLogSummary> RecentAuditLogs { get; set; } = new();

    public void OnGet()
        {
            // TODO: Replace with actual data service calls
LoadDashboardStats();
     LoadTrialsExpiring();
            LoadRecentActivity();
        }

    private void LoadDashboardStats()
        {
            // Mock data - replace with actual repository calls
          TotalOrganizations = 42;
      TrialOrganizations = 8;
            ActiveOrganizations = 32;
            SuspendedOrganizations = 2;
PendingDemoRequests = 5;
  NewOrgsThisMonth = 6;
            NewActiveThisMonth = 4;
            TrialsExpiringSoon = 3;
        }

        private void LoadTrialsExpiring()
     {
      // Mock data - replace with actual repository calls
            TrialsExpiring = new List<TrialExpiring>
  {
         new() { OrgName = "TechCorp Solutions", OrgCode = "TECH001", EndDate = DateTime.Now.AddDays(2), DaysRemaining = 2 },
  new() { OrgName = "Digital Ventures", OrgCode = "DIGI002", EndDate = DateTime.Now.AddDays(5), DaysRemaining = 5 },
    new() { OrgName = "Innovation Labs", OrgCode = "INNO003", EndDate = DateTime.Now.AddDays(6), DaysRemaining = 6 }
        };
        }

        private void LoadRecentActivity()
        {
        // Mock data - replace with actual repository calls
       RecentAuditLogs = new List<AuditLogSummary>
         {
new() { Action = "CREATE_ORG", EntityName = "Organizations", CreatedAt = DateTime.Now.AddHours(-2), OrgName = "NewCorp" },
      new() { Action = "APPROVE_DEMO_REQUEST", EntityName = "DemoRequests", CreatedAt = DateTime.Now.AddHours(-4), OrgName = null },
            new() { Action = "UPDATE_SUBSCRIPTION", EntityName = "Subscriptions", CreatedAt = DateTime.Now.AddHours(-6), OrgName = "TechCorp" },
         new() { Action = "CREATE_PLAN", EntityName = "Plans", CreatedAt = DateTime.Now.AddDays(-1), OrgName = null },
           new() { Action = "SUSPEND_ORG", EntityName = "Organizations", CreatedAt = DateTime.Now.AddDays(-1), OrgName = "OldCorp" }
   };
        }
    }

    public class TrialExpiring
    {
   public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class AuditLogSummary
    {
        public string Action { get; set; } = string.Empty;
  public string? EntityName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OrgName { get; set; }
    }
}