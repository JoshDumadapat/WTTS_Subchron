using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin.DemoRequests
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

        public new DemoRequestDetailViewModel Request { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var request = await LoadDemoRequestAsync(id);
            if (request == null)
                return NotFound();

            Request = request;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, string action)
        {
            var request = await LoadDemoRequestAsync(id);
            if (request == null)
                return NotFound();

            Request = request;
            if (!string.Equals(Request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "This request has already been reviewed.";
                return Page();
            }

            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
            {
                ErrorMessage = "Session expired. Please sign in again.";
                return Page();
            }

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await client.PostAsJsonAsync(baseUrl + $"/api/demo-requests/{id}/review", new { action });
                if (!resp.IsSuccessStatusCode)
                {
                    ErrorMessage = "Unable to update request status.";
                    return Page();
                }

                TempData["FlashMessage"] = action == "approve"
                    ? "Demo request approved successfully."
                    : "Demo request rejected successfully.";
                return RedirectToPage("/SuperAdmin/DemoRequests/Index");
            }
            catch
            {
                ErrorMessage = "Unable to update request status.";
                return Page();
            }
        }

        private async Task<DemoRequestDetailViewModel?> LoadDemoRequestAsync(int id)
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return null;

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return await client.GetFromJsonAsync<DemoRequestDetailViewModel>(baseUrl + $"/api/demo-requests/{id}");
            }
            catch
            {
                return null;
            }
        }
    }

    public class DemoRequestDetailViewModel
    {
        public int DemoRequestID { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? OrgSize { get; set; }
        public string? DesiredMode { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserID { get; set; }
        public int? OrgID { get; set; }
    }
}
