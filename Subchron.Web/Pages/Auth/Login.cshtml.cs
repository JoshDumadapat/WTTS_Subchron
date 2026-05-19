using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Infrastructure;

namespace Subchron.Web.Pages.Auth;

[IgnoreAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class LoginModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly AuthApiForwarder _api;

    public LoginModel(IConfiguration config, AuthApiForwarder api)
    {
        _config = config;
        _api = api;
    }

    /// <summary>Base URL for login JSON handlers on this page (same origin).</summary>
    public string LoginHandlerUrl { get; private set; } = "/Auth/Login";

    public string RecaptchaSiteKey { get; private set; } = "";

    public string? LoginErrorMessage { get; private set; }

    /// <summary>When set, show TOTP step on load (e.g. after external login with 2FA).</summary>
    public string? TotpIntentToken { get; set; }
    public string? TotpIntentEmail { get; set; }

    public IActionResult OnGet(string? step = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToRoleDashboard();

        LoginHandlerUrl = Url.Page("/Auth/Login") ?? "/Auth/Login";
        RecaptchaSiteKey = _config["Recaptcha:SiteKey"] ?? "";
        LoginErrorMessage = TempData["LoginError"]?.ToString();

        if (step == "totp")
        {
            TotpIntentToken = TempData["TotpIntentToken"]?.ToString();
            TotpIntentEmail = TempData["TotpIntentEmail"]?.ToString();
        }

        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        return Page();
    }

    public Task<IActionResult> OnGetCaptchaRequiredAsync() =>
        _api.ForwardGetAsync(HttpContext, "api/auth/captcha-required");

    public Task<IActionResult> OnPostLoginAsync() =>
        _api.ForwardPostAsync(HttpContext, "api/auth/login");

    public Task<IActionResult> OnPostVerifyTotpAsync() =>
        _api.ForwardPostAsync(HttpContext, "api/auth/verify-totp");

    public Task<IActionResult> OnPostVerifyExternalTotpAsync() =>
        _api.ForwardPostAsync(HttpContext, "api/auth/verify-external-totp");

    public Task<IActionResult> OnPostVerifyRecoveryAsync() =>
        _api.ForwardPostAsync(HttpContext, "api/auth/verify-recovery");

    public Task<IActionResult> OnPostVerifyExternalRecoveryAsync() =>
        _api.ForwardPostAsync(HttpContext, "api/auth/verify-external-recovery");

    private IActionResult RedirectToRoleDashboard()
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

        return RedirectToPage(dest);
    }
}
