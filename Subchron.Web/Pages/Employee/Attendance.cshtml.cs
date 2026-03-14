using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.Employee
{
    public class AttendanceModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public AttendanceModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnGetTodayAsync()
            => await ProxyGetAsync("/api/shift-schedule/my/attendance?from=" + DateTime.UtcNow.Date.AddDays(-1).ToString("O") + "&to=" + DateTime.UtcNow.Date.AddDays(1).ToString("O"));

        public async Task<IActionResult> OnPostPunchAsync([FromForm] string action, [FromForm] bool testMode, [FromForm] decimal? latitude, [FromForm] decimal? longitude, [FromForm] string? deviceTimestamp)
            => await ProxyPostAsync("/api/shift-schedule/my/punch", new
            {
                action,
                testMode,
                latitude,
                longitude,
                deviceTimestamp,
                deviceInfo = Request.Headers["User-Agent"].ToString()
            });

        private async Task<IActionResult> ProxyGetAsync(string path)
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return new JsonResult(new List<object>());

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var resp = await client.GetAsync(baseUrl + path);
                var body = await resp.Content.ReadAsStringAsync();
                return new ContentResult { StatusCode = (int)resp.StatusCode, ContentType = "application/json", Content = string.IsNullOrWhiteSpace(body) ? "[]" : body };
            }
            catch
            {
                return new JsonResult(new List<object>());
            }
        }

        private async Task<IActionResult> ProxyPostAsync(string path, object payload)
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return new JsonResult(new { ok = false, message = "Not authenticated." });

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var resp = await client.PostAsJsonAsync(baseUrl + path, payload);
                var body = await resp.Content.ReadAsStringAsync();
                return new ContentResult { StatusCode = (int)resp.StatusCode, ContentType = "application/json", Content = string.IsNullOrWhiteSpace(body) ? "{}" : body };
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, message = ex.Message });
            }
        }
    }
}
