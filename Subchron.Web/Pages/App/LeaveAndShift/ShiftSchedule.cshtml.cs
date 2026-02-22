using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;
using Subchron.Web.Rbac;

namespace Subchron.Web.Pages.App.LeaveAndShift;

public class ShiftScheduleModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public ShiftScheduleModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public IActionResult OnGet()
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.ShiftSchedule))
            return RedirectToPage("/App/Dashboard");
        return Page();
    }

    public string ApiBaseUrl => (_config["ApiBaseUrl"] ?? "").TrimEnd('/');

    private async Task<HttpClient> GetApiClientAsync()
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        var client = _http.CreateClient("Subchron.API");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<IActionResult> OnGetShiftsAsync(DateTime? from, DateTime? to, int? departmentId, int? empId)
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.ShiftSchedule))
            return new JsonResult(new List<ShiftItem>());
        var baseUrl = ApiBaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return new JsonResult(new List<ShiftItem>());
        var fromDate = from?.Date ?? DateTime.UtcNow.Date;
        var toDate = to?.Date ?? fromDate.AddMonths(1);
        try
        {
            var client = await GetApiClientAsync();
            var q = "from=" + fromDate.ToString("O") + "&to=" + toDate.ToString("O");
            if (departmentId.HasValue) q += "&departmentId=" + departmentId.Value;
            if (empId.HasValue) q += "&empId=" + empId.Value;
            var resp = await client.GetAsync(baseUrl + "/api/shiftassignments?" + q);
            if (!resp.IsSuccessStatusCode) return new JsonResult(new List<ShiftItem>());
            var list = await resp.Content.ReadFromJsonAsync<List<ShiftItem>>();
            return new JsonResult(list ?? new List<ShiftItem>());
        }
        catch { return new JsonResult(new List<ShiftItem>()); }
    }

    public async Task<IActionResult> OnGetShiftsByDateAsync(DateTime date, int? departmentId)
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.ShiftSchedule))
            return new JsonResult(new List<ShiftItem>());
        var baseUrl = ApiBaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return new JsonResult(new List<ShiftItem>());
        try
        {
            var client = await GetApiClientAsync();
            var q = "date=" + date.ToString("O") + (departmentId.HasValue ? "&departmentId=" + departmentId.Value : "");
            var resp = await client.GetAsync(baseUrl + "/api/shiftassignments/by-date?" + q);
            if (!resp.IsSuccessStatusCode) return new JsonResult(new List<ShiftItem>());
            var list = await resp.Content.ReadFromJsonAsync<List<ShiftItem>>();
            return new JsonResult(list ?? new List<ShiftItem>());
        }
        catch { return new JsonResult(new List<ShiftItem>()); }
    }

    public async Task<IActionResult> OnGetEmployeesAsync()
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.ShiftSchedule))
            return new JsonResult(new List<EmpOption>());
        var baseUrl = ApiBaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return new JsonResult(new List<EmpOption>());
        try
        {
            var client = await GetApiClientAsync();
            var resp = await client.GetAsync(baseUrl + "/api/employees?archivedOnly=false");
            if (!resp.IsSuccessStatusCode) return new JsonResult(new List<EmpOption>());
            var list = await resp.Content.ReadFromJsonAsync<List<EmpOption>>();
            return new JsonResult(list ?? new List<EmpOption>());
        }
        catch { return new JsonResult(new List<EmpOption>()); }
    }

    public async Task<IActionResult> OnGetDepartmentsAsync()
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.ShiftSchedule))
            return new JsonResult(new List<DepOption>());
        var baseUrl = ApiBaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return new JsonResult(new List<DepOption>());
        try
        {
            var client = await GetApiClientAsync();
            var resp = await client.GetAsync(baseUrl + "/api/departments");
            if (!resp.IsSuccessStatusCode) return new JsonResult(new List<DepOption>());
            var raw = await resp.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<DepOption>>(raw);
            return new JsonResult(list ?? new List<DepOption>());
        }
        catch { return new JsonResult(new List<DepOption>()); }
    }

    public async Task<IActionResult> OnPostCreateShiftAsync([FromForm] int empId, [FromForm] DateTime assignmentDate, [FromForm] string startTime, [FromForm] string endTime, [FromForm] string? notes)
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.ShiftSchedule))
            return new JsonResult(new { ok = false, message = "Forbidden" });
        var baseUrl = ApiBaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return new JsonResult(new { ok = false, message = "API not configured" });
        if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
            return new JsonResult(new { ok = false, message = "Invalid time format" });
        try
        {
            var client = await GetApiClientAsync();
            var body = new { empId, assignmentDate = assignmentDate.Date, startTime = start, endTime = end, notes };
            var resp = await client.PostAsJsonAsync(baseUrl + "/api/shiftassignments", body);
            var msg = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                return new JsonResult(new { ok = false, message = msg.Length > 200 ? "Request failed" : msg });
            return new JsonResult(new { ok = true });
        }
        catch (Exception ex) { return new JsonResult(new { ok = false, message = ex.Message }); }
    }

    public async Task<IActionResult> OnPostDeleteShiftAsync([FromForm] int id)
    {
        if (!RoleModuleAccess.CanAccessModule(User, AppModule.ShiftSchedule))
            return new JsonResult(new { ok = false });
        var baseUrl = ApiBaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return new JsonResult(new { ok = false });
        try
        {
            var client = await GetApiClientAsync();
            var resp = await client.DeleteAsync(baseUrl + "/api/shiftassignments/" + id);
            return new JsonResult(new { ok = resp.IsSuccessStatusCode });
        }
        catch { return new JsonResult(new { ok = false }); }
    }

    public class ShiftItem
    {
        public int ShiftAssignmentID { get; set; }
        public int EmpID { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmpNumber { get; set; }
        public int? DepartmentID { get; set; }
        public DateTime AssignmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EmpOption
    {
        public int EmpID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmpNumber { get; set; }
    }

    public class DepOption
    {
        public int DepID { get; set; }
        public string? DepartmentName { get; set; }
    }
}
