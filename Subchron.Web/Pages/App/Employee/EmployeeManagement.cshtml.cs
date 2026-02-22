using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Employee;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class EmployeeManagementModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public EmployeeManagementModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public List<EmployeeItem> Employees { get; set; } = new();
    public List<DepartmentItem> Departments { get; set; } = new();
    public string? Error { get; set; }
    public string ApiBaseUrl { get; set; } = "";

    public async Task OnGetAsync()
    {
        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        await LoadDepartmentsAsync();
        await LoadEmployeesAsync(archivedOnly: false);
    }

    private string GetApiBaseUrl() => (_config["ApiBaseUrl"] ?? "").TrimEnd('/');

    private async Task LoadDepartmentsAsync()
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token)) return;
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return;
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var list = await client.GetFromJsonAsync<List<DepartmentItem>>(baseUrl + "/api/departments");
            Departments = list ?? new List<DepartmentItem>();
        }
        catch
        {
            Departments = new List<DepartmentItem>();
        }
    }

    private async Task LoadEmployeesAsync(bool archivedOnly)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token)) return;
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return;
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var url = baseUrl + "/api/employees" + (archivedOnly ? "?archivedOnly=true" : "");
            var list = await client.GetFromJsonAsync<List<EmployeeItem>>(url);
            Employees = list ?? new List<EmployeeItem>();
        }
        catch
        {
            Employees = new List<EmployeeItem>();
        }
    }

    /// <summary>Proxies API GET /api/employees/{id}/attendance-qr and returns PNG for the Generate QR modal.</summary>
    public async Task<IActionResult> OnGetAttendanceQrAsync(int id)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return NotFound();
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return NotFound();
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.GetAsync(baseUrl + $"/api/employees/{id}/attendance-qr");
            if (!resp.IsSuccessStatusCode)
                return NotFound();
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            return File(bytes, "image/png");
        }
        catch
        {
            return NotFound();
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

    public async Task<IActionResult> OnPostUpdateEmployeeAsync(
        int id,
        string? firstName, string? lastName, string? middleName,
        string? empNumber, int? departmentID, string? role, string? workState, string? employmentType,
        DateTime? dateHired, string? phone,
        string? addressLine1, string? addressLine2, string? city, string? stateProvince, string? postalCode, string? country,
        int? age, string? gender,
        string? emergencyContactName, string? emergencyContactPhone, string? emergencyContactRelation)
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
            TempData["ToastMessage"] = "API URL is not configured.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var payload = new
            {
                FirstName = firstName,
                LastName = lastName,
                MiddleName = middleName,
                EmpNumber = empNumber,
                DepartmentID = departmentID,
                Role = role,
                WorkState = workState,
                EmploymentType = employmentType,
                DateHired = dateHired,
                Phone = phone,
                AddressLine1 = addressLine1,
                AddressLine2 = addressLine2,
                City = city,
                StateProvince = stateProvince,
                PostalCode = postalCode,
                Country = country,
                Age = age,
                Gender = gender,
                EmergencyContactName = emergencyContactName,
                EmergencyContactPhone = emergencyContactPhone,
                EmergencyContactRelation = emergencyContactRelation
            };
            var resp = await client.PutAsJsonAsync(baseUrl + "/api/employees/" + id, payload);
            if (!resp.IsSuccessStatusCode)
            {
                var msg = "Failed to update employee.";
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
            TempData["ToastMessage"] = "Employee updated successfully.";
            TempData["ToastSuccess"] = true;
            return RedirectToPage();
        }
        catch
        {
            TempData["ToastMessage"] = "Failed to update employee.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostUpdateWorkStateAsync(int id, string workState)
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
            TempData["ToastMessage"] = "API URL is not configured.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
        var ws = (workState ?? "").Trim();
        if (string.IsNullOrEmpty(ws))
        {
            TempData["ToastMessage"] = "Work state is required.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.PutAsJsonAsync(baseUrl + "/api/employees/" + id, new { WorkState = ws });
            if (!resp.IsSuccessStatusCode)
            {
                var msg = "Failed to update work state.";
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
            TempData["ToastMessage"] = "Work state updated.";
            TempData["ToastSuccess"] = true;
            return RedirectToPage();
        }
        catch
        {
            TempData["ToastMessage"] = "Failed to update work state.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id, string? reason)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
        {
            TempData["ToastMessage"] = "Not authenticated.";
            TempData["ToastSuccess"] = false;
            await LoadDepartmentsAsync();
            await LoadEmployeesAsync(false);
            return RedirectToPage();
        }
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            TempData["ToastMessage"] = "API URL is not configured.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
        var r = (reason ?? "").Trim();
        if (string.IsNullOrEmpty(r))
        {
            TempData["ToastMessage"] = "Archived reason is required (e.g. Resigned, Terminated).";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.PatchAsJsonAsync(baseUrl + $"/api/employees/{id}/archive", new { Reason = r });
            if (!resp.IsSuccessStatusCode)
            {
                var msg = "Failed to archive employee.";
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
            TempData["ToastMessage"] = "Employee transferred to archive.";
            TempData["ToastSuccess"] = true;
            return RedirectToPage();
        }
        catch
        {
            TempData["ToastMessage"] = "Failed to archive employee. Check that the API is running.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage();
        }
    }

    public class EmployeeItem
    {
        public int EmpID { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string EmpNumber { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? MiddleName { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string Role { get; set; } = "";
        public string WorkState { get; set; } = "";
        public string EmploymentType { get; set; } = "";
        public DateTime? DateHired { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public bool IsArchived { get; set; }
    }

    public class DepartmentItem
    {
        public int DepID { get; set; }
        public string DepartmentName { get; set; } = "";
    }
}
