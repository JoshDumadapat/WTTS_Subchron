using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Employee;

public class EditModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public EditModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public int Id { get; set; }
    public EditEmployeeDto? Employee { get; set; }
    public List<DepartmentItem> Departments { get; set; } = new();
    public string? Error { get; set; }

    private string GetApiBaseUrl() => (_config["ApiBaseUrl"] ?? "").TrimEnd('/');

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (id <= 0)
            return RedirectToPage("/App/Employee/EmployeeManagement");
        Id = id;
        await LoadDepartmentsAsync();
        var emp = await LoadEmployeeAsync(id);
        if (emp == null)
        {
            TempData["ToastMessage"] = "Employee not found.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EmployeeManagement");
        }
        Employee = emp;
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        int id,
        string firstName, string lastName, string? middleName,
        int? age, string? gender,
        string? empNumber, int? departmentID, DateTime? dateHired,
        string? phone, string? addressLine1, string? addressLine2,
        string? city, string? stateProvince, string? postalCode, string? country,
        string role, string employmentType, string? workState,
        string? emergencyContactName, string? emergencyContactPhone, string? emergencyContactRelation)
    {
        if (id <= 0)
            return RedirectToPage("/App/Employee/EmployeeManagement");
        Id = id;
        await LoadDepartmentsAsync();
        Employee = await LoadEmployeeAsync(id);
        if (Employee == null)
        {
            TempData["ToastMessage"] = "Employee not found.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EmployeeManagement");
        }

        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
        {
            TempData["ToastMessage"] = "Not authenticated.";
            TempData["ToastSuccess"] = false;
            return Page();
        }
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            TempData["ToastMessage"] = "API URL is not configured.";
            TempData["ToastSuccess"] = false;
            return Page();
        }
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            TempData["ToastMessage"] = "First name and last name are required.";
            TempData["ToastSuccess"] = false;
            return Page();
        }

        var payload = new
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            EmpNumber = string.IsNullOrWhiteSpace(empNumber) ? null : empNumber.Trim(),
            DepartmentID = departmentID,
            Role = string.IsNullOrWhiteSpace(role) ? "Employee" : role.Trim(),
            WorkState = string.IsNullOrWhiteSpace(workState) ? "Active" : workState.Trim(),
            EmploymentType = string.IsNullOrWhiteSpace(employmentType) ? "Regular" : employmentType.Trim(),
            DateHired = dateHired,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            AddressLine1 = string.IsNullOrWhiteSpace(addressLine1) ? null : addressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            StateProvince = string.IsNullOrWhiteSpace(stateProvince) ? null : stateProvince.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(country) ? "Philippines" : country.Trim(),
            Age = age,
            Gender = string.IsNullOrWhiteSpace(gender) ? null : gender.Trim(),
            EmergencyContactName = string.IsNullOrWhiteSpace(emergencyContactName) ? null : emergencyContactName.Trim(),
            EmergencyContactPhone = string.IsNullOrWhiteSpace(emergencyContactPhone) ? null : emergencyContactPhone.Trim(),
            EmergencyContactRelation = string.IsNullOrWhiteSpace(emergencyContactRelation) ? null : emergencyContactRelation.Trim()
        };

        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
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
                return Page();
            }
            TempData["ToastMessage"] = "Employee updated successfully.";
            TempData["ToastSuccess"] = true;
            return RedirectToPage("/App/Employee/EmployeeManagement");
        }
        catch
        {
            TempData["ToastMessage"] = "Failed to update employee. Check that the API is running.";
            TempData["ToastSuccess"] = false;
            return Page();
        }
    }

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

    private async Task<EditEmployeeDto?> LoadEmployeeAsync(int id)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token)) return null;
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return null;
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var emp = await client.GetFromJsonAsync<EditEmployeeDto>(baseUrl + "/api/employees/" + id);
            return emp;
        }
        catch
        {
            return null;
        }
    }

    public class EditEmployeeDto
    {
        public int EmpID { get; set; }
        public int? UserID { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string? EmpNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? Role { get; set; }
        public string? WorkState { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime? DateHired { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? StateProvince { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public string? Email { get; set; }
    }

    public class DepartmentItem
    {
        public int DepID { get; set; }
        public string DepartmentName { get; set; } = "";
    }
}
