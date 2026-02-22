using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.SuperAdmin.DemoRequests
{
 public class DetailsModel : PageModel
    {
     public new DemoRequestDetailViewModel Request { get; set; } = new();
        public string? ErrorMessage { get; set; }
 public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
      var request = await LoadDemoRequest(id);
     if (request == null)
      {
       return NotFound();
  }

        Request = request;
  return Page();
 }

    public async Task<IActionResult> OnPostAsync(int id, string action)
  {
var request = await LoadDemoRequest(id);
  if (request == null)
       {
    return NotFound();
   }

    Request = request;

   if (Request.Status != "Pending")
    {
ErrorMessage = "This request has already been reviewed.";
   return Page();
        }

   try
  {
   if (action == "approve")
       {
    await ApproveRequest(id);
        SuccessMessage = "Demo request approved successfully. Organization created.";
         return RedirectToPage("/SuperAdmin/Organizations/Details", new { id = Request.OrgID });
      }
      else if (action == "reject")
        {
         await RejectRequest(id);
  SuccessMessage = "Demo request rejected.";
   return RedirectToPage("/SuperAdmin/DemoRequests/Index");
       }
     }
     catch (Exception ex)
  {
     ErrorMessage = $"Failed to process request: {ex.Message}";
  return Page();
     }

     return Page();
     }

   private async Task<DemoRequestDetailViewModel?> LoadDemoRequest(int id)
   {
      // Mock data - replace with actual repository call
     await Task.Delay(10);

 var mockRequests = new List<DemoRequestDetailViewModel>
   {
       new()
       {
    DemoRequestID = 1,
   OrgName = "TechFlow Solutions",
ContactName = "John Smith",
       Email = "john.smith@techflow.com",
      Phone = "+1-555-0101",
      OrgSize = "50-100",
            DesiredMode = "Biometric",
  Message = "We need a comprehensive time tracking system for our growing team. Our current solution is outdated and we're looking for something modern with biometric authentication to ensure accuracy.",
      Status = "Pending",
     CreatedAt = DateTime.Now.AddDays(-2)
  },
  new()
      {
    DemoRequestID = 2,
     OrgName = "Digital Marketing Pro",
    ContactName = "Sarah Johnson",
     Email = "sarah@digitalmp.com",
   Phone = "+1-555-0102",
     OrgSize = "10-50",
 DesiredMode = "QR Code",
            Message = "Looking for a simple solution for remote team tracking.",
    Status = "Approved",
     CreatedAt = DateTime.Now.AddDays(-5),
   ReviewedAt = DateTime.Now.AddDays(-1),
      OrgID = 123
      }
    };

      return mockRequests.FirstOrDefault(r => r.DemoRequestID == id);
        }

      private async Task ApproveRequest(int demoRequestId)
      {
       // TODO: Replace with actual service calls
       await Task.Delay(10);

// 1. Create organization
    var orgId = await CreateOrganizationFromRequest(demoRequestId);

     // 2. Create default organization settings
       await CreateOrganizationSettings(orgId);

   // 3. Create trial subscription
     await CreateTrialSubscription(orgId);

       // 4. Update demo request
  await UpdateDemoRequestStatus(demoRequestId, "Approved", orgId);

     // 5. Create audit logs
    await CreateAuditLog("APPROVE_DEMO_REQUEST", "DemoRequests", demoRequestId, $"Approved demo request #{demoRequestId}");
   await CreateAuditLog("CREATE_ORG", "Organizations", orgId, $"Organization created from demo request #{demoRequestId}");

      // Update the current request object
  Request.Status = "Approved";
  Request.ReviewedAt = DateTime.Now;
  Request.OrgID = orgId;
  }

   private async Task RejectRequest(int demoRequestId)
        {
       // TODO: Replace with actual service calls
     await Task.Delay(10);

        // 1. Update demo request
       await UpdateDemoRequestStatus(demoRequestId, "Rejected", null);

        // 2. Create audit log
   await CreateAuditLog("REJECT_DEMO_REQUEST", "DemoRequests", demoRequestId, $"Rejected demo request #{demoRequestId}");

       // Update the current request object
   Request.Status = "Rejected";
 Request.ReviewedAt = DateTime.Now;
   }

      private async Task<int> CreateOrganizationFromRequest(int demoRequestId)
      {
     // Mock implementation - replace with actual repository call
       await Task.Delay(10);
        var orgId = new Random().Next(1000, 9999);
         
          // Organization creation logic would go here
    // Return the created organization ID
     return orgId;
    }

       private async Task CreateOrganizationSettings(int orgId)
        {
     // Mock implementation - replace with actual repository call
    await Task.Delay(10);
      // Default settings creation logic
   }

    private async Task CreateTrialSubscription(int orgId)
       {
      // Mock implementation - replace with actual repository call
        await Task.Delay(10);
    // Trial subscription creation logic
   }

   private async Task UpdateDemoRequestStatus(int demoRequestId, string status, int? orgId)
    {
    // Mock implementation - replace with actual repository call
     await Task.Delay(10);
        // Update status, reviewed date, and org ID if provided
   }

   private async Task CreateAuditLog(string action, string entityName, int entityId, string details)
        {
   // Mock implementation - replace with actual repository call
     await Task.Delay(10);
  }
    }

 public class DemoRequestDetailViewModel
    {
      public int DemoRequestID { get; set; }
     public string OrgName { get; set; } = string.Empty;
      public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
  public string Phone { get; set; } = string.Empty;
      public string OrgSize { get; set; } = string.Empty;
       public string DesiredMode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
     public string Status { get; set; } = "Pending";
   public DateTime CreatedAt { get; set; }
     public DateTime? ReviewedAt { get; set; }
   public int? ReviewedByUserID { get; set; }
       public int? OrgID { get; set; }
   }
}