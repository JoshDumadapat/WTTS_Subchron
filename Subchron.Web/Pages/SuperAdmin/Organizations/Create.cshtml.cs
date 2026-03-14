using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin.Organizations
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public CreateModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

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

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            Input.OrgCode = (Input.OrgCode ?? string.Empty).Trim().ToUpperInvariant();

            if (!ModelState.IsValid)
                return Page();

            var client = CreateAuthorizedClient();
            if (client == null)
                return RedirectToPage("/Auth/Login");

            try
            {
                var resp = await client.PostAsJsonAsync("api/superadmin/organizations", new
                {
                    orgName = Input.OrgName,
                    orgCode = Input.OrgCode,
                    status = Input.Status
                });

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    ErrorMessage = string.IsNullOrWhiteSpace(body)
                        ? "Failed to create organization."
                        : body;
                    return Page();
                }

                var result = await resp.Content.ReadFromJsonAsync<CreateOrgResponse>();
                if (result == null || !result.Ok || result.OrgId <= 0)
                {
                    ErrorMessage = "Failed to create organization.";
                    return Page();
                }

                return RedirectToPage("/SuperAdmin/Organizations/Details", new { id = result.OrgId });
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to create organization: " + ex.Message;
                return Page();
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

        private class CreateOrgResponse
        {
            public bool Ok { get; set; }
            public int OrgId { get; set; }
        }
    }
}
