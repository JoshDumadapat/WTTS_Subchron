using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.AuditLog;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public IndexModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public List<AuditLogItem> Logs { get; set; } = new();
    public string ApiBaseUrl { get; set; } = "";
    public int TotalCount { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
    public string? ActionFilter { get; set; }
    public string? EntityFilter { get; set; }
    public string? SearchFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public async Task OnGetAsync(DateTime? from, DateTime? to, string? action, string? entityName, string? search, int page = 1, int pageSize = 50)
    {
        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        CurrentPage = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 200);
        FromDate = from?.ToString("yyyy-MM-dd");
        ToDate = to?.ToString("yyyy-MM-dd");
        ActionFilter = action;
        EntityFilter = entityName;
        SearchFilter = search;
        Logs = new List<AuditLogItem>();
    }

    public async Task<IActionResult> OnGetDataAsync(DateTime? from, DateTime? to, string? action, string? entityName, string? search, int page = 1, int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { logs = new List<AuditLogItem>(), totalCount = 0 });

        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return new JsonResult(new { logs = new List<AuditLogItem>(), totalCount = 0 });

        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var query = new List<string> { "page=" + page, "pageSize=" + pageSize };
            if (from.HasValue) query.Add("from=" + from.Value.ToString("O"));
            if (to.HasValue) query.Add("to=" + to.Value.ToString("O"));
            if (!string.IsNullOrWhiteSpace(action)) query.Add("action=" + Uri.EscapeDataString(action));
            if (!string.IsNullOrWhiteSpace(entityName)) query.Add("entityName=" + Uri.EscapeDataString(entityName));
            if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search));
            var resp = await client.GetAsync(baseUrl + "/api/auditlogs?" + string.Join("&", query));
            if (!resp.IsSuccessStatusCode)
                return new JsonResult(new { logs = new List<AuditLogItem>(), totalCount = 0 });
            var list = await resp.Content.ReadFromJsonAsync<List<AuditLogItem>>();
            var total = 0;
            if (resp.Headers.TryGetValues("X-Total-Count", out var values) && values.Any())
                int.TryParse(values.First(), out total);
            if (total == 0 && list != null) total = list.Count;
            return new JsonResult(new { logs = list ?? new List<AuditLogItem>(), totalCount = total });
        }
        catch
        {
            return new JsonResult(new { logs = new List<AuditLogItem>(), totalCount = 0 });
        }
    }

    public class AuditLogItem
    {
        public int AuditID { get; set; }
        public int? OrgID { get; set; }
        public int? UserID { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = "";
        public string? EntityName { get; set; }
        public int? EntityID { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
