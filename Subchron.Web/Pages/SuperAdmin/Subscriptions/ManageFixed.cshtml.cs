using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin.Subscriptions
{
    public class ManageModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public ManageModel(IHttpClientFactory http)
        {
            _http = http;
        }

        public OrganizationSummary Organization { get; set; } = new();
        public List<PlanOption> AvailablePlans { get; set; } = new();

        [BindProperty]
        public SubscriptionInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int orgId, bool activate = false)
        {
            if (orgId <= 0)
                return NotFound();

            var loaded = await LoadManageDataAsync(orgId);
            if (!loaded)
                return NotFound();

            if (activate)
                Input.Status = "Active";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            if (!ModelState.IsValid)
            {
                await LoadManageDataAsync(Input.OrgID);
                return Page();
            }

            var client = CreateAuthorizedClient();
            if (client == null)
                return RedirectToPage("/Auth/Login");

            if (action == "activate")
                Input.Status = "Active";

            var payload = new SuperAdminManagePayload
            {
                OrgID = Input.OrgID,
                PlanID = Input.PlanID,
                AttendanceMode = Input.AttendanceMode,
                BillingCycle = Input.BillingCycle,
                StartDate = Input.StartDate,
                EndDate = Input.EndDate,
                Status = Input.Status,
                ModePrice = Input.ModePrice
            };

            var resp = await client.PostAsJsonAsync($"api/superadmin/subscriptions/{Input.OrgID}/manage", payload);
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = "Failed to update subscription.";
                await LoadManageDataAsync(Input.OrgID);
                return Page();
            }

            return RedirectToPage("/SuperAdmin/Subscriptions/Index");
        }

        private async Task<bool> LoadManageDataAsync(int orgId)
        {
            var client = CreateAuthorizedClient();
            if (client == null)
                return false;

            var resp = await client.GetAsync($"api/superadmin/subscriptions/{orgId}/manage");
            if (!resp.IsSuccessStatusCode)
                return false;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = await resp.Content.ReadFromJsonAsync<SuperAdminManageResponse>(options);
            if (data == null)
                return false;

            Organization = data.Organization;
            AvailablePlans = data.Plans;
            Input = data.Input;
            return true;
        }

        private HttpClient? CreateAuthorizedClient()
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
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
        public int? SubscriptionID { get; set; }

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

    public sealed class SuperAdminManageResponse
    {
        public OrganizationSummary Organization { get; set; } = new();
        public List<PlanOption> Plans { get; set; } = new();
        public SubscriptionInput Input { get; set; } = new();
    }

    public sealed class SuperAdminManagePayload
    {
        public int OrgID { get; set; }
        public int PlanID { get; set; }
        public string AttendanceMode { get; set; } = "QR Code";
        public string BillingCycle { get; set; } = "Monthly";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Trial";
        public decimal? ModePrice { get; set; }
    }
}
