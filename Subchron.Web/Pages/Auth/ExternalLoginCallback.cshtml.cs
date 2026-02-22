using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Auth;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ExternalLoginCallbackModel : PageModel
{
    private readonly IHttpClientFactory _http;

    public ExternalLoginCallbackModel(IHttpClientFactory http)
    {
        _http = http;
    }

    public record ExternalLoginResponse(
        bool Ok,
        bool RequiresSignup,
        int UserId,
        int? OrgId,
        string? OrgName,
        string? Role,
        string? Email,
        string? Name,
        string? Message,
        string? Token,
        bool RequiresTotp = false,
        string? TotpIntentToken = null
    );

    public async Task<IActionResult> OnGet(string? flow = "login")
    {
        var ext = await HttpContext.AuthenticateAsync("External");
        if (!ext.Succeeded || ext.Principal == null)
            return RedirectToPage("/Auth/AccessDenied");

        // Flow from URL (we put it in RedirectUri in ExternalLogin); default "login" so Login page gets AccessDenied when user missing
        var flowValue = (Request.Query["flow"].FirstOrDefault() ?? flow ?? "login").Trim().ToLowerInvariant();

        var email = ext.Principal.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var name = ext.Principal.FindFirst(ClaimTypes.Name)?.Value ?? email;
        var externalId = ext.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        await HttpContext.SignOutAsync("External");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(externalId))
            return RedirectToPage("/Auth/AccessDenied");

        var client = _http.CreateClient("Subchron.API");
        var resp = await client.PostAsJsonAsync("/api/auth/external-login", new
        {
            provider = "Google",
            externalId,
            email,
            name
        });

        if (!resp.IsSuccessStatusCode)
        {
            // API error (e.g. 500): if signup flow, still send to SignupDetails so user can continue
            if (flowValue != "login" || Request.Query["flow"].Count == 0)
            {
                var fallbackUrl = "/Landing/SignupDetails?provider=google&email=" + Uri.EscapeDataString(email) + "&externalId=" + Uri.EscapeDataString(externalId) + "&name=" + Uri.EscapeDataString(name);
                return LocalRedirect(fallbackUrl);
            }
            return RedirectToPage("/Auth/AccessDenied");
        }

        var data = await resp.Content.ReadFromJsonAsync<ExternalLoginResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (data is null)
        {
            if (flowValue != "login" || Request.Query["flow"].Count == 0)
            {
                var fallbackUrl = "/Landing/SignupDetails?provider=google&email=" + Uri.EscapeDataString(email) + "&externalId=" + Uri.EscapeDataString(externalId) + "&name=" + Uri.EscapeDataString(name);
                return LocalRedirect(fallbackUrl);
            }
            return RedirectToPage("/Auth/AccessDenied");
        }

        if (data.Ok)
        {
            if (data.RequiresTotp && !string.IsNullOrEmpty(data.TotpIntentToken))
            {
                TempData["TotpIntentToken"] = data.TotpIntentToken;
                TempData["TotpIntentEmail"] = data.Email ?? email;
                return RedirectToPage("/Auth/Login", new { step = "totp" });
            }
            return RedirectToPage("/Auth/CompleteLogin", new
            {
                userId = data.UserId,
                orgId = data.OrgId,
                role = data.Role,
                name = data.Name ?? name,
                token = data.Token,
                orgName = data.OrgName
            });
        }

        // API returned a message (e.g. account deactivated)
        if (!string.IsNullOrWhiteSpace(data.Message))
        {
            TempData["LoginError"] = data.Message;
            return RedirectToPage("/Auth/Login");
        }

        // User does not exist: send to SignupDetails unless they explicitly came from Login (flow=login in URL)
        if (data.RequiresSignup)
        {
            bool fromLoginPage = flowValue == "login" && Request.Query["flow"].Count > 0;
            if (!fromLoginPage)
            {
                // Signup flow or flow missing (e.g. query lost) → always SignupDetails when user doesn't exist
                var signupUrl = Url.Page("/Landing/SignupDetails", values: new
                {
                    provider = "google",
                    email = data.Email ?? email,
                    externalId,
                    name = data.Name ?? name
                });
                if (string.IsNullOrEmpty(signupUrl))
                {
                    var q = new List<string>
                    {
                        "provider=google",
                        "email=" + Uri.EscapeDataString(data.Email ?? email),
                        "externalId=" + Uri.EscapeDataString(externalId),
                        "name=" + Uri.EscapeDataString(data.Name ?? name)
                    };
                    signupUrl = "/Landing/SignupDetails?" + string.Join("&", q);
                }
                return LocalRedirect(signupUrl);
            }
            // Came from Login but no account with this Google email — show helpful message
            TempData["LoginError"] = "No account found with this Google email. Your admin account must use the same email as your Google sign-in (e.g. " + (data.Email ?? email) + "). Sign in with email/password if your admin uses a different address, or contact your administrator to add this email.";
            return RedirectToPage("/Auth/Login");
        }

        return RedirectToPage("/Auth/AccessDenied");
    }
}