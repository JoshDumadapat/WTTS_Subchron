using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Auth;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[IgnoreAntiforgeryToken]
public class CompleteLoginModel : PageModel
{
    public record CompleteLoginRequest(int userId, int? orgId, string role, string? name, string? token, string? orgName);

    /// <summary>Claim name for the JWT access token (used to call API from server-side).</summary>
    public const string AccessTokenClaimType = "access_token";

    /// <summary>Claim name for organization display name (sidebar).</summary>
    public const string OrgNameClaimType = "orgName";

    // NEW: supports redirect from ExternalLoginCallback
    public async Task<IActionResult> OnGet(int userId, int? orgId, string role, string? name = null, string? token = null, string? orgName = null)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(role))
            return RedirectToPage("/Auth/Login");

        await IssueCookie(userId, orgId, role.Trim(), name?.Trim(), token?.Trim(), orgName?.Trim());
        return LocalRedirect(RoleToDest(role.Trim()));
    }

    // KEEP: supports your existing POST flows
    public async Task<IActionResult> OnPostAsync([FromBody] CompleteLoginRequest req)
    {
        if (req.userId <= 0 || string.IsNullOrWhiteSpace(req.role))
            return BadRequest(new { ok = false });

        await IssueCookie(req.userId, req.orgId, req.role.Trim(), req.name?.Trim(), req.token?.Trim(), req.orgName?.Trim());
        return new JsonResult(new { ok = true });
    }

    private async Task IssueCookie(int userId, int? orgId, string role, string? name = null, string? token = null, string? orgName = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role)
        };

        if (!string.IsNullOrWhiteSpace(name))
            claims.Add(new Claim(ClaimTypes.Name, name));

        if (orgId.HasValue)
            claims.Add(new Claim("orgId", orgId.Value.ToString()));

        if (!string.IsNullOrWhiteSpace(orgName))
            claims.Add(new Claim(OrgNameClaimType, orgName));

        if (!string.IsNullOrWhiteSpace(token))
            claims.Add(new Claim(AccessTokenClaimType, token));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true
            });
    }

    private static string RoleToDest(string role)
    {
        var r = (role ?? "").Trim();
        if (string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase)) return "/SuperAdmin/Dashboard";
        if (string.Equals(r, "Employee", StringComparison.OrdinalIgnoreCase)) return "/Employee/Dashboard";
        if (string.Equals(r, "OrgAdmin", StringComparison.OrdinalIgnoreCase)) return "/App/Dashboard";
        if (string.Equals(r, "HR", StringComparison.OrdinalIgnoreCase)) return "/App/Dashboard";
        if (string.Equals(r, "Manager", StringComparison.OrdinalIgnoreCase)) return "/App/Dashboard";
        if (string.Equals(r, "Supervisor", StringComparison.OrdinalIgnoreCase)) return "/App/Dashboard";
        if (string.Equals(r, "Payroll", StringComparison.OrdinalIgnoreCase)) return "/App/Dashboard";
        return "/App/Dashboard";
    }
}
