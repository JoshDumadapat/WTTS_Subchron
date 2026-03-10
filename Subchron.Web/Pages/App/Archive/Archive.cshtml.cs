using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Archive;

public class ArchiveModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public ArchiveModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public List<ArchivedEmployeeItem> ArchivedEmployees { get; set; } = new();
    public string? Error { get; set; }

    private string GetApiBaseUrl() => (_config["ApiBaseUrl"] ?? "").TrimEnd('/');

    public async Task OnGetAsync()
    {
        await LoadArchivedAsync();
    }

    private async Task LoadArchivedAsync()
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token)) return;
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return;
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var list = await client.GetFromJsonAsync<List<ArchivedEmployeeItem>>(baseUrl + "/api/employees?archivedOnly=true");
            ArchivedEmployees = list ?? new List<ArchivedEmployeeItem>();
        }
        catch
        {
            ArchivedEmployees = new List<ArchivedEmployeeItem>();
        }
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id, string? reason)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
        {
            TempData["ToastMessage"] = "Not authenticated.";
            TempData["ToastSuccess"] = false;
            await LoadArchivedAsync();
            return RedirectToPage();
        }
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            TempData["ToastMessage"] = "API URL is not configured.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ToastMessage"] = "Reason for restore is required.";
            TempData["ToastSuccess"] = false;
            await LoadArchivedAsync();
            return RedirectToPage();
        }
        if (reason.Trim().Length > 60)
        {
            TempData["ToastMessage"] = "Reason for restore must be 60 characters or fewer.";
            TempData["ToastSuccess"] = false;
            await LoadArchivedAsync();
            return RedirectToPage();
        }
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.PatchAsJsonAsync(baseUrl + $"/api/employees/{id}/restore", new { Reason = reason?.Trim() });
            if (!resp.IsSuccessStatusCode)
            {
                var msg = "Failed to restore employee.";
                try
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    if (body.Contains("message", StringComparison.OrdinalIgnoreCase))
                    {
                        var doc = System.Text.Json.JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("message", out var m))
                            msg = m.GetString() ?? msg;
                    }
                }
                catch { /* ignore */ }
                TempData["ToastMessage"] = msg;
                TempData["ToastSuccess"] = false;
                return RedirectToPage();
            }
            TempData["ToastMessage"] = "Employee restored to active directory.";
            TempData["ToastSuccess"] = true;
            return RedirectToPage();
        }
        catch
        {
            TempData["ToastMessage"] = "Failed to restore employee. Check that the API is running.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetEmployeeAsync(int id)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { });

        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return new JsonResult(new { });

        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.GetAsync(baseUrl + "/api/employees/" + id);
            if (!resp.IsSuccessStatusCode)
                return new JsonResult(new { });
            var json = await resp.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
        catch
        {
            return new JsonResult(new { });
        }
    }

    public class ArchivedEmployeeItem
    {
        public int EmpID { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string EmpNumber { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string? Email { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateHired { get; set; }
        public string Role { get; set; } = "";
        public string WorkState { get; set; } = "";
        public string? ArchivedReason { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public string? AddressLine2 { get; set; }
        public string? StateProvince { get; set; }
        public string? PostalCode { get; set; }
        public string? EmploymentType { get; set; }
        public decimal? BaseSalary { get; set; }
        public string? AvatarUrl { get; set; }
        public string? IdPictureUrl { get; set; }
        public string? SignatureUrl { get; set; }
    }
}
