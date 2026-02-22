using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Subchron.API.Models.Entities;
using Subchron.API.Models.Settings;

namespace Subchron.API.Services;

public class OnboardingTokenPayload
{
    public int UserId { get; set; }
    public int OrgId { get; set; }
    public string Role { get; set; } = "";
    public int PlanId { get; set; }
    public string PlanName { get; set; } = "";
    public bool IsFreeTrial { get; set; }
}

public class JwtTokenService
{
    private const int OnboardingTokenExpiryMinutes = 15;
    private readonly JwtSettings _jwt;

    public JwtTokenService(IOptions<JwtSettings> jwt)
    {
        _jwt = jwt.Value;
    }

    // Short-lived token used only for the billing step, not for normal sessions.
    public string CreateOnboardingToken(int userId, int orgId, string role, int planId, string planName, bool isFreeTrial)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("orgId", orgId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role),
            new Claim("planId", planId.ToString()),
            new Claim("planName", planName ?? ""),
            new Claim("onboarding", "1"),
            new Claim("isFreeTrial", isFreeTrial ? "1" : "0")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(OnboardingTokenExpiryMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Validates the onboarding token and returns the payload, or null if invalid or expired.
    public OnboardingTokenPayload? ValidateOnboardingToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            if (principal.FindFirstValue("onboarding") != "1") return null;
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var orgId = principal.FindFirstValue("orgId");
            var role = principal.FindFirstValue(ClaimTypes.Role) ?? principal.FindFirstValue("role");
            var planId = principal.FindFirstValue("planId");
            var planName = principal.FindFirstValue("planName") ?? "";
            var isFreeTrial = principal.FindFirstValue("isFreeTrial") == "1";
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(orgId) || string.IsNullOrEmpty(role)) return null;
            return new OnboardingTokenPayload
            {
                UserId = int.Parse(userId),
                OrgId = int.Parse(orgId),
                Role = role,
                PlanId = int.TryParse(planId, out var pid) ? pid : 0,
                PlanName = planName,
                IsFreeTrial = isFreeTrial
            };
        }
        catch
        {
            return null;
        }
    }

    private const int TotpIntentExpiryMinutes = 5;

    /// <summary>Creates a short-lived token for external-login TOTP step. Only contains userId; validated by VerifyExternalTotp.</summary>
    public string CreateTotpIntentToken(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("purpose", "external_totp")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(TotpIntentExpiryMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Validates a TOTP intent token; returns userId or null.</summary>
    public int? ValidateTotpIntentToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            if (principal.FindFirstValue("purpose") != "external_totp") return null;
            var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out var uid) ? uid : null;
        }
        catch { return null; }
    }

    /// <param name="effectiveRole">If set, used for role claims instead of user.Role (e.g. "Employee" when user is linked to an Employee so they are redirected to employee portal).</param>
    public string CreateToken(User user, string? effectiveRole = null)
    {
        var role = !string.IsNullOrEmpty(effectiveRole) ? effectiveRole : user.Role.ToString();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),

            // keep your custom claim
            new Claim("role", role),

            // optional but recommended for ASP.NET role auth
            new Claim(ClaimTypes.Role, role)
        };

        if (user.OrgID.HasValue)
            claims.Add(new Claim("orgId", user.OrgID.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var issuer = _jwt?.Issuer?.Trim() ?? "Subchron.API";
        var audience = _jwt?.Audience?.Trim() ?? "Subchron.Web";
        var expiryMinutes = _jwt != null && _jwt.ExpiryMinutes > 0 ? _jwt.ExpiryMinutes : 120;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
