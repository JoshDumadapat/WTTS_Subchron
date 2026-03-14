using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin.Subscriptions
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public IndexModel(IHttpClientFactory http)
        {
            _http = http;
        }

        public List<SubscriptionViewModel> Subscriptions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PlanFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ExpiringOnly { get; set; }

        [TempData]
        public string? FlashMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadSubscriptionsAsync();
        }

        public async Task<IActionResult> OnPostExtendTrialAsync(int subscriptionId)
        {
            var client = CreateAuthorizedClient();
            if (client == null)
                return RedirectToPage("/Auth/Login");

            var resp = await client.PostAsJsonAsync($"api/superadmin/subscriptions/{subscriptionId}/extend-trial", new { days = 7 });
            if (!resp.IsSuccessStatusCode)
                FlashMessage = "Could not extend trial.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateAsync(int subscriptionId)
        {
            var client = CreateAuthorizedClient();
            if (client == null)
                return RedirectToPage("/Auth/Login");

            var resp = await client.PostAsync($"api/superadmin/subscriptions/{subscriptionId}/activate", null);
            if (!resp.IsSuccessStatusCode)
                FlashMessage = "Could not activate subscription.";
            return RedirectToPage();
        }

        private async Task LoadSubscriptionsAsync()
        {
            Subscriptions = new List<SubscriptionViewModel>();
            var client = CreateAuthorizedClient();
            if (client == null)
                return;

            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(StatusFilter))
                query.Add("status=" + Uri.EscapeDataString(StatusFilter.Trim()));
            if (!string.IsNullOrWhiteSpace(PlanFilter))
                query.Add("plan=" + Uri.EscapeDataString(PlanFilter.Trim()));
            if (ExpiringOnly)
                query.Add("expiringOnly=true");

            var url = "api/superadmin/subscriptions" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = await resp.Content.ReadFromJsonAsync<List<SubscriptionViewModel>>(options);
            if (data != null)
                Subscriptions = data;
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

    public class SubscriptionViewModel
    {
   public int SubscriptionID { get; set; }
        public int OrgID { get; set; }
   public string OrgName { get; set; } = string.Empty;
       public string OrgCode { get; set; } = string.Empty;
  public string PlanName { get; set; } = string.Empty;
  public string AttendanceMode { get; set; } = string.Empty;
   public decimal BasePrice { get; set; }
      public decimal ModePrice { get; set; }
        public decimal FinalPrice { get; set; }
   public string BillingCycle { get; set; } = string.Empty;
     public DateTime StartDate { get; set; }
       public DateTime? EndDate { get; set; }
  public string Status { get; set; } = string.Empty;
  public int DaysRemaining { get; set; }
    }
}
