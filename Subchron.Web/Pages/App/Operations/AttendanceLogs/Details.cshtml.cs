using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Operations.AttendanceLogs
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

        [FromQuery(Name = "id")]
        public int LogId { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnGetItemAsync()
        {
            if (LogId <= 0)
                return new JsonResult(new { ok = false, message = "Invalid attendance log id." }) { StatusCode = 400 };

            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return new JsonResult(new { ok = false, message = "Not authenticated." }) { StatusCode = 401 };

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var resp = await client.GetAsync(baseUrl + $"/api/attendance-logs/current/{LogId}");
                var body = await resp.Content.ReadAsStringAsync();
                return new ContentResult
                {
                    StatusCode = (int)resp.StatusCode,
                    ContentType = "application/json",
                    Content = string.IsNullOrWhiteSpace(body) ? "{}" : body
                };
            }
            catch
            {
                return new JsonResult(new { ok = false, message = "Failed to load attendance details." }) { StatusCode = 500 };
            }
        }
    }
}
