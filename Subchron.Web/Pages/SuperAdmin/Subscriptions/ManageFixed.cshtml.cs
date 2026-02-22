using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Subchron.Web.Pages.SuperAdmin.Subscriptions
{
    public class ManageModel : PageModel
    {
      public OrganizationSummary Organization { get; set; } = new();
        public List<PlanOption> AvailablePlans { get; set; } = new();

      [BindProperty]
        public SubscriptionInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int orgId, bool activate = false)
        {
            var org = await LoadOrganization(orgId);
    if (org == null)
     {
          return NotFound();
  }

   Organization = org;
         await LoadAvailablePlans();
        await LoadCurrentSubscription(orgId);

    if (activate)
     {
           Input.Status = "Active";
      }

     return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            var org = await LoadOrganization(Input.OrgID);
    if (org == null)
 {
   return NotFound();
            }

      Organization = org;
          await LoadAvailablePlans();

     if (!ModelState.IsValid)
        {
           return Page();
    }

   try
      {
        if (action == "save")
       {
      await SaveSubscription();
    return RedirectToPage("/SuperAdmin/Subscriptions/Index");
                }
                else if (action == "activate")
    {
        await ActivateSubscription();
        return RedirectToPage("/SuperAdmin/Organizations/Details", new { id = Input.OrgID });
   }
         }
   catch (Exception ex)
     {
  ErrorMessage = $"Failed to update subscription: {ex.Message}";
  return Page();
            }

         return Page();
        }

        private async Task<OrganizationSummary?> LoadOrganization(int orgId)
        {
   // Mock data - replace with actual repository call
        await Task.Delay(10);

         var mockOrgs = new List<OrganizationSummary>
            {
     new() { OrgID = 1, OrgName = "TechCorp Solutions", OrgCode = "TECH001", Status = "Active" },
        new() { OrgID = 2, OrgName = "Digital Ventures", OrgCode = "DIGI002", Status = "Trial" },
     new() { OrgID = 3, OrgName = "Innovation Labs", OrgCode = "INNO003", Status = "Active" }
            };

  return mockOrgs.FirstOrDefault(o => o.OrgID == orgId);
        }

        private async Task LoadAvailablePlans()
        {
            // Mock data - replace with actual repository call
    await Task.Delay(10);

            AvailablePlans = new List<PlanOption>
    {
             new() { PlanID = 1, PlanName = "Basic", BasePrice = 1500 },
        new() { PlanID = 2, PlanName = "Standard", BasePrice = 2500 },
          new() { PlanID = 3, PlanName = "Enterprise", BasePrice = 5000 }
     };
   }

     private async Task LoadCurrentSubscription(int orgId)
        {
   // Mock data - replace with actual repository call
   await Task.Delay(10);

            // Pre-populate form with current subscription data
   if (orgId == 1)
       {
                Input.OrgID = 1;
  Input.PlanID = 2; // Standard
           Input.AttendanceMode = "Biometric";
                Input.BillingCycle = "Monthly";
              Input.StartDate = DateTime.Now.AddMonths(-3);
      Input.EndDate = DateTime.Now.AddMonths(9);
       Input.Status = "Active";
     Input.ModePrice = null; // Use default
            }
            else if (orgId == 2)
      {
      Input.OrgID = 2;
          Input.PlanID = 2; // Standard
              Input.AttendanceMode = "QR Code";
Input.BillingCycle = "Monthly";
       Input.StartDate = DateTime.Now.AddDays(-10);
        Input.EndDate = DateTime.Now.AddDays(4);
      Input.Status = "Trial";
      Input.ModePrice = null;
   }
      else
            {
         // New subscription
                Input.OrgID = orgId;
       Input.PlanID = 2; // Default to Standard
      Input.AttendanceMode = "QR Code"; // Default
                Input.BillingCycle = "Monthly";
     Input.StartDate = DateTime.Now;
      Input.EndDate = DateTime.Now.AddDays(14); // 14-day trial
        Input.Status = "Trial";
          Input.ModePrice = null;
    }
        }

        private async Task SaveSubscription()
 {
          // TODO: Replace with actual service calls
      await Task.Delay(10);

            // Calculate final price
      var selectedPlan = AvailablePlans.FirstOrDefault(p => p.PlanID == Input.PlanID);
    if (selectedPlan == null) throw new InvalidOperationException("Invalid plan selected");

     var basePrice = selectedPlan.BasePrice;
        var modePrice = Input.ModePrice ?? GetDefaultModePrice(Input.AttendanceMode);
var finalPrice = basePrice + modePrice;

        // Apply annual discount if applicable
  if (Input.BillingCycle == "Annual")
            {
      finalPrice = finalPrice * 0.9m; // 10% discount
}

            // Create audit log
          await CreateAuditLog("UPDATE_SUBSCRIPTION", "Subscriptions", Input.OrgID,
     $"Subscription updated: Plan={selectedPlan.PlanName}, Mode={Input.AttendanceMode}, Status={Input.Status}");
   }

        private async Task ActivateSubscription()
        {
            await SaveSubscription();

   // Create audit log for activation
  await CreateAuditLog("ACTIVATE_SUBSCRIPTION", "Subscriptions", Input.OrgID,
    "Trial subscription activated");
        }

        private decimal GetDefaultModePrice(string attendanceMode)
        {
return attendanceMode switch
            {
                "QR Code" => 0,
     "Biometric" => 500,
    "Geo Location" => 300,
          _ => 0
        };
        }

        private async Task CreateAuditLog(string action, string entityName, int entityId, string details)
        {
    // Mock implementation - replace with actual repository call
     await Task.Delay(10);
        }
  }

    public class OrganizationSummary
    {
        public int OrgID { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class PlanOption
    {
  public int PlanID { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
    }

    public class SubscriptionInput
    {
    [Required]
 public int OrgID { get; set; }

        [Required(ErrorMessage = "Please select a plan")]
     public int PlanID { get; set; }

        [Required(ErrorMessage = "Please select an attendance mode")]
        public string AttendanceMode { get; set; } = "QR Code";

        [Required(ErrorMessage = "Please select a billing cycle")]
        public string BillingCycle { get; set; } = "Monthly";

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        [Required]
    public string Status { get; set; } = "Trial";

      [Range(0, 10000, ErrorMessage = "Mode price must be between 0 and 10000")]
        public decimal? ModePrice { get; set; }
    }
}