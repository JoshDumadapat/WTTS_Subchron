using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.Employee
{
    public class MyQrModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        private readonly ILogger<MyQrModel> _logger;

        public MyQrModel(IHttpClientFactory http, IConfiguration config, ILogger<MyQrModel> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public string EmployeeNumber { get; set; } = "EMP-000";
        public string EmployeeName { get; set; } = "Employee";

        public async Task OnGetAsync()
        {
            EmployeeName = User.Identity?.Name ?? "Employee";

            var token = GetAccessToken();
            var baseUrl = GetApiBaseUrl();
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return;

            try
            {
                var client = CreateAuthorizedApiClient(token);
                var info = await client.GetFromJsonAsync<EmployeeInfoResponse>(baseUrl + "/api/auth/employee-info");
                if (info != null)
                {
                    if (!string.IsNullOrWhiteSpace(info.EmpNumber))
                        EmployeeNumber = info.EmpNumber.Trim();

                    var fullName = string.Join(" ", new[] { info.FirstName, info.MiddleName, info.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
                    if (!string.IsNullOrWhiteSpace(fullName))
                        EmployeeName = fullName;
                }
            }
            catch
            {
                // Keep defaults; QR image handler still attempts fetch.
            }
        }

        public async Task<IActionResult> OnGetQrAsync()
        {
            var token = GetAccessToken();
            var baseUrl = GetApiBaseUrl();
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return QrErrorSvg("Session unavailable", "Please sign in again.");

            try
            {
                var client = CreateAuthorizedApiClient(token);
                client.DefaultRequestHeaders.Remove("X-Web-Base");
                client.DefaultRequestHeaders.Add("X-Web-Base", Request.Scheme + "://" + Request.Host);

                var resp = await client.GetAsync(baseUrl + "/api/auth/employee-attendance-qr");
                if (!resp.IsSuccessStatusCode)
                {
                    if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized || resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        return QrErrorSvg("Session expired", "Please sign in again.");
                    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return QrErrorSvg("Employee QR unavailable", "No linked employee profile found.");

                    var body = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Employee QR request failed. Status: {Status} Body: {Body}", resp.StatusCode, body);
                    return QrErrorSvg("QR not available", "Please refresh or contact admin.");
                }

                var bytes = await resp.Content.ReadAsByteArrayAsync();
                if (bytes == null || bytes.Length == 0)
                    return QrErrorSvg("QR not available", "Empty QR response.");
                return File(bytes, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load employee QR.");
                return QrErrorSvg("QR loading failed", "Please refresh and try again.");
            }
        }

        private ContentResult QrErrorSvg(string title, string subtitle)
        {
            static string Esc(string value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
            var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='512' height='512' viewBox='0 0 512 512'>" +
                      "<rect width='512' height='512' fill='#f8fafc'/>" +
                      "<rect x='32' y='32' width='448' height='448' rx='24' ry='24' fill='#ffffff' stroke='#e2e8f0'/>" +
                      "<text x='256' y='220' text-anchor='middle' font-family='Segoe UI, Arial' font-size='28' fill='#0f172a' font-weight='700'>" + Esc(title) + "</text>" +
                      "<text x='256' y='260' text-anchor='middle' font-family='Segoe UI, Arial' font-size='18' fill='#475569'>" + Esc(subtitle) + "</text>" +
                      "</svg>";
            return Content(svg, "image/svg+xml", Encoding.UTF8);
        }

        private string? GetAccessToken() => User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;

        private string? GetApiBaseUrl()
        {
            var baseUrl = (_config["ApiBaseUrl"] ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.TrimEnd('/');
        }

        private HttpClient CreateAuthorizedApiClient(string token)
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private sealed class EmployeeInfoResponse
        {
            public string? FirstName { get; set; }
            public string? MiddleName { get; set; }
            public string? LastName { get; set; }
            public string? EmpNumber { get; set; }
        }
    }
}
