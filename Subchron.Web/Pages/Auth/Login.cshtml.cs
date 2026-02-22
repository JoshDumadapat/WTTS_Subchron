using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IConfiguration _config;

    public LoginModel(IConfiguration config)
    {
        _config = config;
    }

    public string ApiBaseUrl { get; private set; } = "";
    public string RecaptchaSiteKey { get; private set; } = "";

    /// <summary>When set, show TOTP step on load (e.g. after external login with 2FA).</summary>
    public string? TotpIntentToken { get; set; }
    public string? TotpIntentEmail { get; set; }

    public IActionResult OnGet(string? step = null)
    {
        // If already authenticated, redirect to appropriate dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToRoleDashboard();
        }

        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        RecaptchaSiteKey = _config["Recaptcha:SiteKey"] ?? "";

        if (step == "totp")
        {
            TotpIntentToken = TempData["TotpIntentToken"]?.ToString();
            TotpIntentEmail = TempData["TotpIntentEmail"]?.ToString();
        }

        // Set no-cache headers for login page
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        return Page();
    }

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