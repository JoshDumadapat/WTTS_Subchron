using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;
using Subchron.Web.Rbac;

namespace Subchron.Web.Pages.App.LeaveAndShift;

public class LeaveManagementModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public LeaveManagementModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public string? StatusFilter { get; set; }
    public string? SearchFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public IActionResult OnGet(string? status, string? search, int page = 1, int pageSize = 25)
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.LeaveManagement))
            return RedirectToPage("/App/Dashboard");

        StatusFilter = status;
        SearchFilter = search;
        CurrentPage = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 10, 100);
        return Page();
    }

    public async Task<IActionResult> OnGetDataAsync(string? status, string? search, int page = 1, int pageSize = 25)
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.LeaveManagement))
            return new JsonResult(new { items = new List<LeaveRequestItem>(), totalCount = 0, page = 1, pageSize = 25 });

        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { items = new List<LeaveRequestItem>(), totalCount = 0, page = 1, pageSize = 25 });

        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return new JsonResult(new { items = new List<LeaveRequestItem>(), totalCount = 0, page = 1, pageSize = 25 });

        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var query = new List<string> { "page=" + page, "pageSize=" + pageSize };
            if (!string.IsNullOrWhiteSpace(status)) query.Add("status=" + Uri.EscapeDataString(status.Trim()));
            if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search.Trim()));
            var resp = await client.GetAsync(baseUrl + "/api/leaverequests?" + string.Join("&", query));
            if (!resp.IsSuccessStatusCode)
                return new JsonResult(new { items = new List<LeaveRequestItem>(), totalCount = 0, page = 1, pageSize = 25 });
            var data = await resp.Content.ReadFromJsonAsync<PagedLeaveResult>();
            if (data == null)
                return new JsonResult(new { items = new List<LeaveRequestItem>(), totalCount = 0, page = 1, pageSize = 25 });
            var items = data.Items.Select(x => new LeaveRequestItem
            {
                LeaveRequestID = x.LeaveRequestID,
                EmpID = x.EmpID,
                EmployeeName = x.EmployeeName,
                EmpNumber = x.EmpNumber,
                LeaveType = x.LeaveType,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                Reason = x.Reason,
                ReviewedByUserName = x.ReviewedByUserName,
                ReviewedAt = x.ReviewedAt,
                ReviewNotes = x.ReviewNotes,
                CreatedAt = x.CreatedAt
            }).ToList();
            return new JsonResult(new { items, totalCount = data.TotalCount, page = data.Page, pageSize = data.PageSize });
        }
        catch
        {
            return new JsonResult(new { items = new List<LeaveRequestItem>(), totalCount = 0, page = 1, pageSize = 25 });
        }
    }

    public async Task<IActionResult> OnPostApproveAsync([FromForm] int id, [FromForm] string? notes)
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.LeaveManagement))
            return Forbid();
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token)) { TempData["ToastMessage"] = "Not authenticated."; TempData["ToastSuccess"] = false; return RedirectToPage(); }
        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl)) { TempData["ToastMessage"] = "API not configured."; TempData["ToastSuccess"] = false; return RedirectToPage(); }
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.PostAsJsonAsync(baseUrl + "/api/leaverequests/" + id + "/approve", new { Notes = notes });
            if (!resp.IsSuccessStatusCode)
            {
                var msg = "Failed to approve.";
                try { var body = await resp.Content.ReadAsStringAsync(); if (body.Contains("message")) { var d = System.Text.Json.JsonDocument.Parse(body); if (d.RootElement.TryGetProperty("message", out var m)) msg = m.GetString() ?? msg; } } catch { }
                TempData["ToastMessage"] = msg; TempData["ToastSuccess"] = false; return RedirectToPage();
            }
            TempData["ToastMessage"] = "Leave request approved."; TempData["ToastSuccess"] = true; return RedirectToPage();
        }
        catch { TempData["ToastMessage"] = "Request failed."; TempData["ToastSuccess"] = false; return RedirectToPage(); }
    }

    public async Task<IActionResult> OnPostDeclineAsync([FromForm] int id, [FromForm] string? notes)
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.LeaveManagement))
            return Forbid();
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token)) { TempData["ToastMessage"] = "Not authenticated."; TempData["ToastSuccess"] = false; return RedirectToPage(); }
        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl)) { TempData["ToastMessage"] = "API not configured."; TempData["ToastSuccess"] = false; return RedirectToPage(); }
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.PostAsJsonAsync(baseUrl + "/api/leaverequests/" + id + "/decline", new { Notes = notes });
            if (!resp.IsSuccessStatusCode)
            {
                var msg = "Failed to decline.";
                try { var body = await resp.Content.ReadAsStringAsync(); if (body.Contains("message")) { var d = System.Text.Json.JsonDocument.Parse(body); if (d.RootElement.TryGetProperty("message", out var m)) msg = m.GetString() ?? msg; } } catch { }
                TempData["ToastMessage"] = msg; TempData["ToastSuccess"] = false; return RedirectToPage();
            }
            TempData["ToastMessage"] = "Leave request declined."; TempData["ToastSuccess"] = true; return RedirectToPage();
        }
        catch { TempData["ToastMessage"] = "Request failed."; TempData["ToastSuccess"] = false; return RedirectToPage(); }
    }

    public class LeaveRequestItem
    {
        public int LeaveRequestID { get; set; }
        public int EmpID { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmpNumber { get; set; }
        public string? LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? ReviewedByUserName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class PagedLeaveResult
    {
        public List<LeaveRequestDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private class LeaveRequestDto
    {
        public int LeaveRequestID { get; set; }
        public int EmpID { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmpNumber { get; set; }
        public string? LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? ReviewedByUserName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
