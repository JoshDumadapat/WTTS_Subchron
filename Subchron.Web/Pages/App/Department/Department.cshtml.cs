using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Department;

public class DepartmentModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public DepartmentModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public List<DepartmentItem> Departments { get; set; } = new();
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string ApiBaseUrl { get; set; } = "";

    public Task OnGetAsync()
    {
        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        Error = null;
        Departments = new List<DepartmentItem>();
        return Task.CompletedTask;
    }

    public async Task<IActionResult> OnGetDataAsync()
    {
        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        await LoadDepartmentsAsync();
        return new JsonResult(new { departments = Departments });
    }

    private string GetApiBaseUrl()
    {
        return (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
    }

    private HttpClient CreateAuthorizedApiClient(string token)
    {
        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task LoadDepartmentsAsync()
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token)) return;
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return;
        try
        {
            var client = CreateAuthorizedApiClient(token);
            var list = await client.GetFromJsonAsync<List<DepartmentItem>>(baseUrl + "/api/departments");
            Departments = list ?? new List<DepartmentItem>();
        }
        catch (Exception)
        {
            Departments = new List<DepartmentItem>();
            Error ??= "Unable to load departments right now.";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
            return JsonError("Department name is required.", 400);
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return JsonError("Not authenticated.", 401);
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return JsonError("API URL is not configured (ApiBaseUrl in appsettings).", 500);
        try
        {
            var client = CreateAuthorizedApiClient(token);
            client.Timeout = TimeSpan.FromSeconds(10);
            var resp = await client.PostAsJsonAsync(baseUrl + "/api/departments", new { DepartmentName = departmentName.Trim() });
            if (!resp.IsSuccessStatusCode)
            {
                var message = await TryGetMessageAsync(resp) ?? (resp.StatusCode == System.Net.HttpStatusCode.Conflict
                    ? "A department with this name already exists."
                    : resp.StatusCode == System.Net.HttpStatusCode.NotFound
                    ? "API not reachable. Ensure the API project is running."
                    : "Failed to create department.");
                return JsonError(message, (int)resp.StatusCode);
            }
            TempData["ToastMessage"] = "Department created successfully.";
            TempData["ToastSuccess"] = true;
            return new JsonResult(new { ok = true, message = "Department created successfully." });
        }
        catch (HttpRequestException)
        {
            return JsonError("Cannot reach the API. Is it running? " + (string.IsNullOrEmpty(baseUrl) ? "" : baseUrl), 502);
        }
        catch (TaskCanceledException)
        {
            return JsonError("Request timed out. Please try again.", 408);
        }
        catch
        {
            return JsonError("Failed to create department. Please try again.", 500);
        }
    }

    private IActionResult JsonError(string message, int statusCode = 400)
    {
        return new JsonResult(new { ok = false, message }) { StatusCode = statusCode };
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
            return JsonError("Department name is required.", 400);
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return JsonError("Not authenticated.", 401);
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return JsonError("API URL is not configured.", 500);
        try
        {
            var client = CreateAuthorizedApiClient(token);
            client.Timeout = TimeSpan.FromSeconds(10);
            var resp = await client.PutAsJsonAsync(baseUrl + $"/api/departments/{id}", new { DepartmentName = departmentName.Trim() });
            if (!resp.IsSuccessStatusCode)
            {
                var message = await TryGetMessageAsync(resp) ?? (resp.StatusCode == System.Net.HttpStatusCode.Conflict
                    ? "A department with this name already exists."
                    : "Failed to update department.");
                return JsonError(message, (int)resp.StatusCode);
            }
            TempData["ToastMessage"] = "Department updated successfully.";
            TempData["ToastSuccess"] = true;
            return new JsonResult(new { ok = true, message = "Department updated successfully." });
        }
        catch (TaskCanceledException)
        {
            return JsonError("Request timed out. Please try again.", 408);
        }
        catch
        {
            return JsonError("Failed to update department.", 500);
        }
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id, bool isActive, string? reason)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
        {
            TempData["ToastMessage"] = "Not authenticated.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            TempData["ToastMessage"] = "API URL is not configured (ApiBaseUrl in appsettings).";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
        try
        {
            var client = CreateAuthorizedApiClient(token);
            var payload = new { IsActive = isActive, Reason = reason?.Trim() ?? "" };
            var resp = await client.PatchAsJsonAsync(baseUrl + $"/api/departments/{id}/status", payload);
            if (!resp.IsSuccessStatusCode)
            {
                var errMsg = "Failed to update status.";
                try
                {
                    var errBody = await resp.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errBody) && errBody.Contains("message", StringComparison.OrdinalIgnoreCase))
                    {
                        var doc = System.Text.Json.JsonDocument.Parse(errBody);
                        if (doc.RootElement.TryGetProperty("message", out var msgEl))
                            errMsg = msgEl.GetString() ?? errMsg;
                    }
                }
                catch { /* use default errMsg */ }
                TempData["ToastMessage"] = errMsg;
                TempData["ToastSuccess"] = false;
                return RedirectToPage();
            }
            TempData["ToastMessage"] = isActive ? "Department activated successfully." : "Department deactivated successfully.";
            TempData["ToastSuccess"] = true;
            return RedirectToPage();
        }
        catch (Exception)
        {
            TempData["ToastMessage"] = "Failed to update department status. Check that the API is running.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
    }

    private static async Task<string?> TryGetMessageAsync(HttpResponseMessage resp)
    {
        try
        {
            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body) || !body.Contains("message", StringComparison.OrdinalIgnoreCase))
                return null;
            var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var m))
                return m.GetString();
        }
        catch { /* ignore */ }
        return null;
    }

    public class DepartmentItem
    {
        public int DepID { get; set; }
        public int OrgID { get; set; }
        public string DepartmentName { get; set; } = "";
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int EmployeeCount { get; set; }
    }
}
