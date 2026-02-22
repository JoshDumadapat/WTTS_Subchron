using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin.Settings;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public IndexModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public const string TabProfile = "Profile";
    public const string TabAccount = "Account";
    public const string TabPreferences = "Preferences";

    public string CurrentTab { get; set; } = TabProfile;

    public void OnGet(string? tab = null)
    {
        if (!string.IsNullOrWhiteSpace(tab))
        {
            var t = tab.Trim();
            if (t == TabAccount || t == TabPreferences || t == TabProfile)
                CurrentTab = t;
        }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Session expired. Please sign in again." }) { StatusCode = 401 };
        if (string.IsNullOrWhiteSpace(currentPassword))
            return new JsonResult(new { ok = false, message = "Current password is required." }) { StatusCode = 400 };
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            return new JsonResult(new { ok = false, message = "New password must be at least 8 characters." }) { StatusCode = 400 };

        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var resp = await client.PostAsJsonAsync("api/auth/change-password", new { currentPassword, newPassword });
        var body = await resp.Content.ReadAsStringAsync();
        var statusCode = (int)resp.StatusCode;
        object? json = null;
        if (!string.IsNullOrEmpty(body))
        {
            try { json = JsonSerializer.Deserialize<JsonElement>(body); }
            catch { json = new { ok = resp.IsSuccessStatusCode }; }
        }
        return new JsonResult(json ?? new { ok = resp.IsSuccessStatusCode }) { StatusCode = statusCode };
    }
}
