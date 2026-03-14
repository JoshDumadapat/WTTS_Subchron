using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin.Organizations
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public DetailsModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

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
            var client = CreateAuthorizedClient();
            if (client == null)
                return RedirectToPage("/Auth/Login");

            try
            {
                var data = await client.GetFromJsonAsync<OrganizationDetailsResponse>("api/superadmin/organizations/" + id);
                if (data == null)
                    return NotFound();

                Organization = data.Organization;
                Settings = data.Settings;
                CurrentSubscription = data.CurrentSubscription;
                EmployeeCount = data.EmployeeCount;
                ActiveUsers = data.ActiveUsers;
                LastActivity = data.LastActivity;
                StorageUsed = data.StorageUsed;
                ApiCallsThisMonth = data.ApiCallsThisMonth;
                AttendanceRecords = data.AttendanceRecords;
                return Page();
            }
            catch
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostSuspendAsync([FromBody] SuspendRequest req)
        {
            if (req.OrgId <= 0)
                return new JsonResult(new { ok = false, message = "Invalid organization." }) { StatusCode = 400 };

            var client = CreateAuthorizedClient();
            if (client == null)
                return new JsonResult(new { ok = false, message = "Session expired." }) { StatusCode = 401 };

            var resp = await client.PostAsync("api/superadmin/organizations/" + req.OrgId + "/suspend", null);
            var body = await resp.Content.ReadAsStringAsync();
            return new ContentResult
            {
                StatusCode = (int)resp.StatusCode,
                ContentType = "application/json",
                Content = string.IsNullOrWhiteSpace(body) ? "{}" : body
            };
        }

        private HttpClient? CreateAuthorizedClient()
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var client = _http.CreateClient("Subchron.API");
            var baseUrl = (_config["ApiBaseUrl"] ?? string.Empty).TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public class OrganizationDetailsResponse
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
        public string? DefaultShiftTemplateCode { get; set; }
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
