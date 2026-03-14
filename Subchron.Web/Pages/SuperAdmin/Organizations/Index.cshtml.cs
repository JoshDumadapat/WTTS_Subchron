using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin.Organizations
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public IndexModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public List<OrganizationViewModel> Organizations { get; set; } = new();

        [TempData]
        public string? FlashMessage { get; set; }

        public async Task OnGetAsync()
        {
            Organizations = await LoadOrganizationsAsync();
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

        private async Task<List<OrganizationViewModel>> LoadOrganizationsAsync()
        {
            var client = CreateAuthorizedClient();
            if (client == null)
                return new List<OrganizationViewModel>();

            try
            {
                var rows = await client.GetFromJsonAsync<List<OrganizationViewModel>>("api/superadmin/organizations");
                return rows ?? new List<OrganizationViewModel>();
            }
            catch
            {
                return new List<OrganizationViewModel>();
            }
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
    }

    public class OrganizationViewModel
    {
        public int OrgID { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PlanName { get; set; }
        public string SubscriptionStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SuspendRequest
    {
        public int OrgId { get; set; }
    }
}
