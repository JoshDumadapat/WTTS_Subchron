using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly IHttpClientFactory _http;

    public LogoutModel(IHttpClientFactory http)
    {
        _http = http;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Record logout in API audit log before signing out (token still in claims)
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                _ = await client.PostAsync("api/auth/logout", null);
            }
            catch
            {
                // Best effort; don't block logout
            }
        }

        // Clear the APP cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        try { await HttpContext.SignOutAsync("Google"); } catch { /* ignore */ }

        var cookieName = CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme;
        Response.Cookies.Delete(cookieName);

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        return RedirectToPage("/Auth/Login");
    }

    public async Task<IActionResult> OnPostAsync() => await OnGetAsync();
}
