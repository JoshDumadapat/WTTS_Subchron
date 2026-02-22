// ===============================
// Pages/Auth/ExternalLogin.cshtml.cs
// ===============================
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Auth;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ExternalLoginModel : PageModel
{
    public IActionResult OnGet(string provider, string? flow = "login")
    {
        if (string.IsNullOrWhiteSpace(provider))
            return RedirectToPage("/Auth/Login");

        // If already authenticated, go dashboard
        if (User?.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value
                       ?? User.FindFirst("role")?.Value
                       ?? "";

            var dest = role switch
            {
                "SuperAdmin" => "/SuperAdmin/Dashboard",
                "Employee" => "/Employee/Dashboard",
                "OrgAdmin" => "/App/Dashboard",
                "HR" => "/App/Dashboard",
                "Manager" => "/App/Dashboard",
                "Supervisor" => "/App/Dashboard",
                "Payroll" => "/App/Dashboard",
                _ => "/App/Dashboard"
            };

            return LocalRedirect(dest);
        }

        // Always return to callback (don't rely on query string surviving)
        var redirectUrl = Url.Page("/Auth/ExternalLoginCallback");
        var props = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        var flowValue = (flow ?? "login").Trim().ToLowerInvariant();
        props.Items["flow"] = flowValue;
        props.Items["provider"] = provider;

        // Fallback: cookie in case OAuth state doesn't preserve Items (e.g. callback loses flow)
        if (flowValue == "signup")
        {
            Response.Cookies.Append("Subchron.ExternalFlow", "signup", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = true,
                MaxAge = TimeSpan.FromMinutes(5)
            });
        }

        return Challenge(props, provider);
    }
}