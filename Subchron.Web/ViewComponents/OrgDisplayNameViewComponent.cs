using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.ViewComponents;

public class OrgDisplayNameViewComponent : ViewComponent
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public OrgDisplayNameViewComponent(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var displayName = await GetOrgDisplayNameAsync();
        return Content(displayName);
    }

    private async Task<string> GetOrgDisplayNameAsync()
    {
        var user = HttpContext.User;
        var orgName = user.FindFirst(CompleteLoginModel.OrgNameClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(orgName))
            return orgName.Trim();

        var orgId = user.FindFirstValue("orgId");
        var token = user.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(orgId) || string.IsNullOrWhiteSpace(token))
            return "Subchron";

        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return "Subchron";

        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("api/organizations/current/name");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<OrgNameResponse>();
                if (!string.IsNullOrWhiteSpace(json?.OrgName))
                    return json.OrgName.Trim();
            }
        }
        catch
        {
            // Ignore; fall back to default
        }

        return "Subchron";
    }

    private sealed class OrgNameResponse
    {
        public string? OrgName { get; set; }
    }
}
