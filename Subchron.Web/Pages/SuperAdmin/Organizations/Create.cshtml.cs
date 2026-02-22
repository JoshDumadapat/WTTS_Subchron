using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Subchron.Web.Pages.SuperAdmin.Organizations
{
    public class CreateModel : PageModel
 {
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
    {
   [Required(ErrorMessage = "Organization name is required")]
            [StringLength(100, ErrorMessage = "Organization name cannot exceed 100 characters")]
            public string OrgName { get; set; } = string.Empty;

          [Required(ErrorMessage = "Organization code is required")]
      [StringLength(20, ErrorMessage = "Organization code cannot exceed 20 characters")]
     [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Organization code must contain only uppercase letters and numbers")]
          public string OrgCode { get; set; } = string.Empty;

            [Required]
     public string Status { get; set; } = "Trial";
        }

        public void OnGet()
        {
       // Initialize form
      }

        public async Task<IActionResult> OnPostAsync()
   {
     if (!ModelState.IsValid)
       {
             return Page();
   }

     try
 {
         // TODO: Replace with actual service calls
         
          // 1. Validate organization code is unique
if (await IsOrgCodeTaken(Input.OrgCode))
      {
         ErrorMessage = $"Organization code '{Input.OrgCode}' is already in use.";
          return Page();
     }

     // 2. Create organization
      var orgId = await CreateOrganization(Input.OrgName, Input.OrgCode, Input.Status);

 // 3. Create default organization settings
         await CreateOrganizationSettings(orgId);

        // 4. Create trial subscription
          await CreateTrialSubscription(orgId);

      // 5. Create audit log
       await CreateAuditLog("CREATE_ORG", "Organizations", orgId, Input.OrgName);

         return RedirectToPage("/SuperAdmin/Organizations/Details", new { id = orgId });
      }
      catch (Exception ex)
            {
       ErrorMessage = $"Failed to create organization: {ex.Message}";
       return Page();
   }
        }

     private async Task<bool> IsOrgCodeTaken(string orgCode)
     {
            // Mock implementation - replace with actual repository call
      await Task.Delay(10);
     return false; // Assume code is available for now
      }

   private async Task<int> CreateOrganization(string orgName, string orgCode, string status)
   {
        // Mock implementation - replace with actual repository call
       await Task.Delay(10);
       return new Random().Next(1000, 9999); // Mock org ID
 }

      private async Task CreateOrganizationSettings(int orgId)
 {
      // Mock implementation - replace with actual repository call
       // Default settings as specified in requirements:
       await Task.Delay(10);
/*
    var settings = new OrganizationSettings
         {
          OrgID = orgId,
                Timezone = "Asia/Manila",
      Currency = "PHP",
      AttendanceMode = "QR",
          AllowManualEntry = false,
        RequireGeo = false,
       EnforceGeofence = false,
  DefaultGraceMinutes = 10,
              RoundRule = "None",
OTEnabled = false,
        OTThresholdHours = 0,
               OTApprovalRequired = true,
          OTMaxHoursPerDay = 0,
       LeaveEnabled = true,
LeaveApprovalRequired = true,
      CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
     };
       */
        }

   private async Task CreateTrialSubscription(int orgId)
        {
      // Mock implementation - replace with actual repository call
   await Task.Delay(10);
            /*
        var subscription = new Subscription
   {
      OrgID = orgId,
PlanID = GetStandardPlanId(), // Default to Standard plan
                AttendanceMode = "QR",
         BasePrice = GetStandardPlanBasePrice(),
        ModePrice = 0,
        FinalPrice = GetStandardPlanBasePrice(),
       BillingCycle = "Monthly",
       StartDate = DateTime.Now,
          EndDate = DateTime.Now.AddDays(14), // 14-day trial
      Status = "Trial"
         };
          */
  }

    private async Task CreateAuditLog(string action, string entityName, int entityId, string details)
        {
       // Mock implementation - replace with actual repository call
   await Task.Delay(10);
      /*
        var auditLog = new AuditLog
     {
      Action = action,
        EntityName = entityName,
     EntityID = entityId,
      Details = details,
          CreatedAt = DateTime.Now,
  // UserID would come from current user context
      };
   */
   }
    }
}