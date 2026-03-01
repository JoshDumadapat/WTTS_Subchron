using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Services;

public sealed class LayoutBrandingService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public LayoutBrandingService(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<LayoutBrandingResult> GetAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var result = new LayoutBrandingResult
        {
            OrgName = user.FindFirst(CompleteLoginModel.OrgNameClaimType)?.Value?.Trim(),
            ProfileName = user.FindFirst(ClaimTypes.Name)?.Value?.Trim()
        };

        var token = user.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        var baseUrl = (_config["ApiBaseUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
            return result;

        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var orgTask = TryGetAsync<OrgProfileDto>(client, baseUrl + "/api/org-profile/current", cancellationToken);
            var profileTask = TryGetAsync<UserProfileDto>(client, baseUrl + "/api/auth/profile", cancellationToken);

            await Task.WhenAll(orgTask, profileTask);

            var orgProfile = await orgTask;
            if (!string.IsNullOrWhiteSpace(orgProfile?.OrgName))
                result.OrgName = orgProfile!.OrgName!.Trim();
            if (!string.IsNullOrWhiteSpace(orgProfile?.LogoUrl))
                result.OrgLogoUrl = orgProfile!.LogoUrl!.Trim();

            var profile = await profileTask;
            if (!string.IsNullOrWhiteSpace(profile?.Name))
                result.ProfileName = profile!.Name!.Trim();
            if (!string.IsNullOrWhiteSpace(profile?.AvatarUrl))
                result.ProfileAvatarUrl = profile!.AvatarUrl!.Trim();
        }
        catch
        {
            // Swallow; layout will fall back to defaults/claims
        }

        return result;
    }

    private static async Task<T?> TryGetAsync<T>(HttpClient client, string url, CancellationToken ct)
    {
        try
        {
            var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                return default;
            return await resp.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
        catch
        {
            return default;
        }
    }

    private sealed class OrgProfileDto
    {
        public string? OrgName { get; set; }
        public string? LogoUrl { get; set; }
    }

    private sealed class UserProfileDto
    {
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
    }
}

public sealed class LayoutBrandingResult
{
    public string? OrgName { get; set; }
    public string? OrgLogoUrl { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfileAvatarUrl { get; set; }
}
