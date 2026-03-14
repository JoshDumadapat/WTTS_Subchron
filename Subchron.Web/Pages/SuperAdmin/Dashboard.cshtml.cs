using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin;

public class DashboardModel : PageModel
{
    private readonly IHttpClientFactory _http;

    public DashboardModel(IHttpClientFactory http)
    {
        _http = http;
    }

    public int TotalOrganizations { get; set; }
    public int TrialOrganizations { get; set; }
    public int ActiveOrganizations { get; set; }
    public int SuspendedOrganizations { get; set; }
    public int PendingDemoRequests { get; set; }
    public int NewOrgsThisMonth { get; set; }
    public int NewActiveThisMonth { get; set; }
    public int TrialsExpiringSoon { get; set; }
    public int TotalUsers { get; set; }
    public decimal TotalRevenue { get; set; }
    public string Currency { get; set; } = "PHP";

    public List<TrialExpiring> TrialsExpiring { get; set; } = new();
    public List<AuditLogSummary> RecentAuditLogs { get; set; } = new();
    public List<GrowthPoint> OrganizationGrowth { get; set; } = new();

    public async Task OnGetAsync()
    {
        var client = CreateAuthorizedClient();
        if (client == null)
            return;

        var summary = await client.GetFromJsonAsync<DashboardSummaryResponse>("api/superadmin/dashboard/summary");
        if (summary == null)
            return;

        TotalOrganizations = summary.TotalOrganizations;
        TrialOrganizations = summary.TrialOrganizations;
        ActiveOrganizations = summary.ActiveOrganizations;
        SuspendedOrganizations = summary.SuspendedOrganizations;
        PendingDemoRequests = summary.PendingDemoRequests;
        NewOrgsThisMonth = summary.NewOrganizationsThisMonth;
        NewActiveThisMonth = summary.NewActiveOrganizationsThisMonth;
        TotalUsers = summary.TotalUsers;
        TotalRevenue = summary.TotalRevenue;
        Currency = string.IsNullOrWhiteSpace(summary.Currency) ? "PHP" : summary.Currency;

        TrialsExpiring = summary.TrialsExpiringSoon
            .Select(t => new TrialExpiring
            {
                OrgName = t.OrgName,
                OrgCode = t.OrgCode,
                EndDate = t.EndDate,
                DaysRemaining = t.DaysRemaining
            })
            .ToList();
        TrialsExpiringSoon = TrialsExpiring.Count;

        RecentAuditLogs = summary.RecentActivity
            .Select(a => new AuditLogSummary
            {
                Action = a.Action,
                EntityName = a.EntityName,
                CreatedAt = a.CreatedAt,
                OrgName = a.OrgName
            })
            .ToList();

        OrganizationGrowth = summary.OrganizationGrowth
            .Select(g => new GrowthPoint
            {
                Year = g.Year,
                Month = g.Month,
                NewOrganizations = g.NewOrganizations,
                TotalOrganizationsCumulative = g.TotalOrganizationsCumulative
            })
            .ToList();
    }

    private HttpClient? CreateAuthorizedClient()
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public class TrialExpiring
    {
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class AuditLogSummary
    {
        public string Action { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OrgName { get; set; }
    }

    public class GrowthPoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int NewOrganizations { get; set; }
        public int TotalOrganizationsCumulative { get; set; }
    }

    private sealed class DashboardSummaryResponse
    {
        public int TotalOrganizations { get; set; }
        public int TrialOrganizations { get; set; }
        public int ActiveOrganizations { get; set; }
        public int SuspendedOrganizations { get; set; }
        public int NewOrganizationsThisMonth { get; set; }
        public int NewActiveOrganizationsThisMonth { get; set; }
        public int TotalUsers { get; set; }
        public int PendingDemoRequests { get; set; }
        public decimal TotalRevenue { get; set; }
        public string Currency { get; set; } = "PHP";
        public List<TrialExpiringResponse> TrialsExpiringSoon { get; set; } = new();
        public List<ActivityResponse> RecentActivity { get; set; } = new();
        public List<GrowthPointResponse> OrganizationGrowth { get; set; } = new();
    }

    private sealed class TrialExpiringResponse
    {
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    private sealed class ActivityResponse
    {
        public string Action { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OrgName { get; set; }
    }

    private sealed class GrowthPointResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int NewOrganizations { get; set; }
        public int TotalOrganizationsCumulative { get; set; }
    }
}
