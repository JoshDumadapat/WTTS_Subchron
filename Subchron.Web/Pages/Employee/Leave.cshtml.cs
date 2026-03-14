using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.Employee;

public class LeaveModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public LeaveModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnGetLeaveTypesAsync()
        => await ProxyGetAsync("/api/leave-types/mine", "[]");

    public async Task<IActionResult> OnGetMyRequestsAsync()
        => await ProxyGetAsync("/api/leaverequests/mine", "[]");

    public async Task<IActionResult> OnPostCreateRequestAsync([FromForm] string leaveType, [FromForm] string leaveStart, [FromForm] string leaveEnd, [FromForm] string? leaveReason)
    {
        if (!DateTime.TryParse(leaveStart, out var startDate) || !DateTime.TryParse(leaveEnd, out var endDate))
            return new JsonResult(new { ok = false, message = "Invalid leave date range." });

        return await ProxyPostAsync("/api/leaverequests/mine", new
        {
            leaveType,
            startDate = startDate.Date,
            endDate = endDate.Date,
            reason = leaveReason
        });
    }

    private async Task<IActionResult> ProxyGetAsync(string path, string defaultJson)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
            return new ContentResult { Content = defaultJson, ContentType = "application/json", StatusCode = 200 };

        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.GetAsync(baseUrl + path);
            var body = await resp.Content.ReadAsStringAsync();
            return new ContentResult
            {
                StatusCode = (int)resp.StatusCode,
                ContentType = "application/json",
                Content = string.IsNullOrWhiteSpace(body) ? defaultJson : body
            };
        }
        catch
        {
            return new ContentResult { Content = defaultJson, ContentType = "application/json", StatusCode = 200 };
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
