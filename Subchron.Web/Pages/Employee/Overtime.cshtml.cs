using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.Employee
{
    public class OvertimeModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public OvertimeModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnGetMyRequestsAsync()
        {
            return await ProxyGetAsync("/api/overtime-requests/mine");
        }

        public async Task<IActionResult> OnPostCreateRequestAsync([FromForm] string otDate, [FromForm] string otStart, [FromForm] string otEnd, [FromForm] string otReason)
        {
            if (!DateTime.TryParse(otDate, out var d) || !TimeSpan.TryParse(otStart, out var st) || !TimeSpan.TryParse(otEnd, out var et))
                return new JsonResult(new { ok = false, message = "Invalid overtime date/time." });

            var start = d.Date.Add(st);
            var end = d.Date.Add(et);
            return await ProxyPostAsync("/api/overtime-requests", new { otDate = d.Date, startTime = start, endTime = end, reason = otReason });
        }

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
                if (string.IsNullOrWhiteSpace(body))
                    body = resp.IsSuccessStatusCode ? "{\"ok\":true}" : "{\"ok\":false}";
                return new ContentResult { StatusCode = (int)resp.StatusCode, ContentType = "application/json", Content = body };
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, message = ex.Message });
            }
        }
    }
}
