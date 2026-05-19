using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App;

public class DashboardModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public DashboardModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? DepartmentId { get; set; }

    // KPI Stats
    public int TotalEmployees { get; set; }
    public int PresentToday { get; set; }
    public int LateArrivals { get; set; }
    public int OnLeave { get; set; }
    public int PendingOTRequests { get; set; }
    public int MissingTimeOut { get; set; }

    public List<string> TrendLabels { get; set; } = new();
    public List<int> OnTimeSeries { get; set; } = new();
    public List<int> LateSeries { get; set; } = new();
    public List<DepartmentBreakdown> DepartmentBreakdownItems { get; set; } = new();
    public List<ActivityItem> RecentActivity { get; set; } = new();
    public List<DepartmentOption> Departments { get; set; } = new();

    /// <summary>Set when the dashboard cannot load KPI data from the API (e.g. API not running).</summary>
    public string? DashboardLoadError { get; set; }

    public async Task OnGetAsync()
    {
        var client = CreateAuthorizedClient();
        if (client == null)
            return;

        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return;

        Departments = await FetchDepartmentsAsync(client, baseUrl);

        var query = new List<string>();
        if (From.HasValue) query.Add("from=" + Uri.EscapeDataString(From.Value.ToString("O")));
        if (To.HasValue) query.Add("to=" + Uri.EscapeDataString(To.Value.ToString("O")));
        if (DepartmentId.HasValue) query.Add("departmentId=" + DepartmentId.Value);

        var url = baseUrl + "/api/admin-dashboard/summary" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        try
        {
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    DashboardLoadError =
                        "The API rejected the request (401). Sign out and sign in again. If you just changed ApiBaseUrl, complete a fresh login so your session token matches this API.";
                }
                else if (resp.StatusCode == HttpStatusCode.Forbidden)
                {
                    DashboardLoadError =
                        "The API returned 403 (forbidden) for this dashboard. Your account may be missing an organization scope, or this module is not allowed for your role.";
                }
                else
                {
                    DashboardLoadError =
                        $"The API returned {(int)resp.StatusCode} for the dashboard. Ensure Subchron.API is running and ApiBaseUrl ({baseUrl}) points at this API instance.";
                }
                return;
            }

            var summary = await resp.Content.ReadFromJsonAsync<DashboardSummaryResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (summary == null)
                return;

            TotalEmployees = summary.TotalEmployees;
            PresentToday = summary.PresentToday;
            LateArrivals = summary.LateArrivals;
            OnLeave = summary.OnLeave;
            PendingOTRequests = summary.PendingOvertime;
            MissingTimeOut = summary.MissingTimeOut;

            TrendLabels = summary.AttendanceTrends
                .Select(x => x.Date.ToString("MMM dd"))
                .ToList();
            OnTimeSeries = summary.AttendanceTrends.Select(x => x.OnTimeCount).ToList();
            LateSeries = summary.AttendanceTrends.Select(x => x.LateCount).ToList();

            DepartmentBreakdownItems = summary.DepartmentBreakdown
                .Select(x => new DepartmentBreakdown
                {
                    DepartmentId = x.DepartmentId,
                    DepartmentName = x.DepartmentName,
                    PresentCount = x.PresentCount
                })
                .ToList();

            RecentActivity = summary.RecentActivity
                .Select(x => BuildActivityItem(x))
                .ToList();
        }
        catch (HttpRequestException)
        {
            DashboardLoadError =
                $"Could not connect to the API at {baseUrl}. Start Subchron.API (HTTP port 5058, or HTTPS profile https://localhost:7077), then refresh.";
        }
        catch (TaskCanceledException)
        {
            DashboardLoadError = "The dashboard request timed out. Check that Subchron.API is running and reachable.";
        }
        catch (JsonException)
        {
            DashboardLoadError = "The API returned data the dashboard could not read. Check API logs for errors.";
        }
    }

    private async Task<List<DepartmentOption>> FetchDepartmentsAsync(HttpClient client, string baseUrl)
    {
        try
        {
            var resp = await client.GetAsync(baseUrl + "/api/departments?activeOnly=true");
            if (!resp.IsSuccessStatusCode)
                return new List<DepartmentOption>();

            var list = await resp.Content.ReadFromJsonAsync<List<DepartmentOptionResponse>>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return list?.Select(d => new DepartmentOption
            {
                DepartmentId = d.DepID,
                DepartmentName = d.DepartmentName
            }).ToList() ?? new List<DepartmentOption>();
        }
        catch
        {
            return new List<DepartmentOption>();
        }
    }

    private ActivityItem BuildActivityItem(ActivityResponse response)
    {
        var initials = string.Join("", (response.EmployeeName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p[..1].ToUpperInvariant()))
            .Trim();
        if (string.IsNullOrWhiteSpace(initials))
            initials = "?";

        var status = (response.Status ?? "Present").Trim();
        var badge = status switch
        {
            "Late" => "bg-amber-100 text-amber-700",
            "Pending" => "bg-purple-100 text-purple-700",
            "Leave" => "bg-gray-100 text-gray-600",
            "Out" => "bg-blue-100 text-blue-700",
            _ => "bg-emerald-100 text-emerald-700"
        };

        return new ActivityItem
        {
            Name = response.EmployeeName ?? "",
            Department = response.DepartmentName ?? "",
            Action = response.Action ?? "",
            Status = status,
            Time = response.Time,
            Initials = initials,
            BadgeClass = badge
        };
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

    public class DepartmentOption
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class DepartmentBreakdown
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int PresentCount { get; set; }
    }

    public class ActivityItem
    {
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Initials { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = string.Empty;
    }

    private sealed class DepartmentOptionResponse
    {
        public int DepID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    private sealed class DashboardSummaryResponse
    {
        public int TotalEmployees { get; set; }
        public int PresentToday { get; set; }
        public int LateArrivals { get; set; }
        public int OnLeave { get; set; }
        public int PendingOvertime { get; set; }
        public int MissingTimeOut { get; set; }
        public List<TrendPointResponse> AttendanceTrends { get; set; } = new();
        public List<DepartmentBreakdownResponse> DepartmentBreakdown { get; set; } = new();
        public List<ActivityResponse> RecentActivity { get; set; } = new();
    }

    private sealed class TrendPointResponse
    {
        public DateTime Date { get; set; }
        public int OnTimeCount { get; set; }
        public int LateCount { get; set; }
    }

    private sealed class DepartmentBreakdownResponse
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int PresentCount { get; set; }
    }

    private sealed class ActivityResponse
    {
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? Action { get; set; }
        public string? Status { get; set; }
        public DateTime Time { get; set; }
    }
}
