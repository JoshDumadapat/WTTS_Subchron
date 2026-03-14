using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace Subchron.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory http, IConfiguration config)
        {
            _logger = logger;
            _http = http;
            _config = config;
        }

        [BindProperty]
        public DemoRequestInput Input { get; set; } = new();

        [TempData]
        public string? DemoSuccessMessage { get; set; }

        [TempData]
        public string? DemoErrorMessage { get; set; }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostRequestDemoAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var baseUrl = (_config["ApiBaseUrl"] ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                DemoErrorMessage = "Demo request service is currently unavailable.";
                return Page();
            }

            try
            {
                var client = _http.CreateClient("Subchron.API");
                var resp = await client.PostAsJsonAsync(baseUrl + "/api/demo-requests", new
                {
                    orgName = Input.OrgName,
                    contactName = Input.ContactName,
                    email = Input.Email,
                    phone = Input.Phone,
                    orgSize = Input.OrgSize,
                    desiredMode = Input.DesiredMode,
                    message = Input.Message
                });

                if (resp.IsSuccessStatusCode)
                {
                    DemoSuccessMessage = "Demo request submitted. We will contact you soon.";
                    return RedirectToPage();
                }

                DemoErrorMessage = "Unable to submit your request right now. Please try again.";
                return Page();
            }
            catch
            {
                DemoErrorMessage = "Unable to submit your request right now. Please try again later.";
                return Page();
            }
        }

        public class DemoRequestInput
        {
            [Required]
            [MaxLength(100)]
            public string OrgName { get; set; } = string.Empty;

            [Required]
            [MaxLength(100)]
            public string ContactName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [MaxLength(100)]
            public string Email { get; set; } = string.Empty;

            [MaxLength(30)]
            public string? Phone { get; set; }

            [MaxLength(20)]
            public string? OrgSize { get; set; }

            [Required]
            [MaxLength(20)]
            public string DesiredMode { get; set; } = string.Empty;

            [MaxLength(255)]
            public string? Message { get; set; }
        }
    }
}
