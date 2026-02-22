using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.SuperAdmin.Organizations
{
    public class IndexModel : PageModel
 {
        public List<OrganizationViewModel> Organizations { get; set; } = new();

        public void OnGet()
 {
      LoadOrganizations();
   }

        private void LoadOrganizations()
   {
// Mock data - replace with actual repository calls
      Organizations = new List<OrganizationViewModel>
  {
  new() 
  { 
      OrgID = 1, 
          OrgName = "TechCorp Solutions", 
             OrgCode = "TECH001", 
   Status = "Active", 
       SubscriptionStatus = "Standard Plan", 
 CreatedAt = DateTime.Now.AddMonths(-6) 
    },
        new() 
     { 
       OrgID = 2, 
    OrgName = "Digital Ventures", 
           OrgCode = "DIGI002", 
          Status = "Trial", 
        SubscriptionStatus = "14-day Trial", 
             CreatedAt = DateTime.Now.AddDays(-10) 
    },
     new() 
    { 
         OrgID = 3, 
          OrgName = "Innovation Labs", 
          OrgCode = "INNO003", 
       Status = "Active", 
            SubscriptionStatus = "Enterprise Plan", 
           CreatedAt = DateTime.Now.AddMonths(-3) 
                },
           new() 
      { 
 OrgID = 4, 
           OrgName = "StartupXYZ", 
                OrgCode = "STAR004", 
     Status = "Suspended", 
         SubscriptionStatus = "Basic Plan (Suspended)", 
         CreatedAt = DateTime.Now.AddMonths(-2) 
          },
          new() 
       { 
OrgID = 5, 
     OrgName = "Global Services Inc", 
          OrgCode = "GLOB005", 
    Status = "Trial", 
    SubscriptionStatus = "14-day Trial", 
    CreatedAt = DateTime.Now.AddDays(-3) 
       }
            };
    }
}

    public class OrganizationViewModel
    {
        public int OrgID { get; set; }
        public string OrgName { get; set; } = string.Empty;
public string OrgCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SubscriptionStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}