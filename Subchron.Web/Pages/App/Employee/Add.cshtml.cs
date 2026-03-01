using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Employee;

public class AddModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public AddModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public List<DepartmentItem> Departments { get; set; } = new();
    public string? Error { get; set; }
    public string? ToastMessage { get; set; }
    public bool? ToastSuccess { get; set; }
    private string GetApiBaseUrl() => (_config["ApiBaseUrl"] ?? "").TrimEnd('/');

    public async Task OnGetAsync()
    {
        await LoadDepartmentsAsync();
    }

    public async Task<IActionResult> OnGetNextEmpNumberAsync(string? role)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { empNumber = "" });
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return new JsonResult(new { empNumber = "" });
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var url = baseUrl + "/api/employees/next-number" + (string.IsNullOrEmpty(role) ? "" : "?role=" + Uri.EscapeDataString(role));
            var resp = await client.GetFromJsonAsync<JsonElement>(url);
            var empNumber = resp.TryGetProperty("empNumber", out var p) ? p.GetString() ?? "" : "";
            return new JsonResult(new { empNumber });
        }
        catch
        {
            return new JsonResult(new { empNumber = "" });
        }
    }

    public async Task<IActionResult> OnGetCheckUniqueAsync(string type, string value)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, exists = false });
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return new JsonResult(new { ok = false, exists = false });
        var kind = (type ?? string.Empty).Trim();
        var val = value ?? string.Empty;
        if (string.IsNullOrEmpty(kind))
            return new JsonResult(new { ok = false, exists = false });
        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var url = baseUrl + "/api/employees/check-unique?type=" + Uri.EscapeDataString(kind) + "&value=" + Uri.EscapeDataString(val);
            var resp = await client.GetFromJsonAsync<UniqueCheckResponse>(url);
            return new JsonResult(new { ok = resp?.Ok == true, exists = resp?.Exists == true });
        }
        catch
        {
            return new JsonResult(new { ok = false, exists = false });
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

    public async Task<IActionResult> OnPostCreateAsync(
        string firstName, string lastName, string? middleName,
        int? age, string? gender,
        string? empNumber, int? departmentID, DateTime? dateHired,
        string? phone, string addressLine1, string? addressLine2,
        string city, string? stateProvince, string? postalCode, string country,
        string role, string employmentType,
        string? emergencyContactName, string? emergencyContactPhone, string? emergencyContactRelation,
        bool createAccount, string? ContactEmail, string? Email, string? DefaultPassword,
        string? idPictureUrl, string? signatureUrl,
        IFormFile? profilePicture, IFormFile? signature)
    {
        await LoadDepartmentsAsync();

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
        if (age.HasValue && (age.Value <= 18 || age.Value > 70))
        {
            TempData["ToastMessage"] = "Age must be between 19 and 70.";
            TempData["ToastSuccess"] = false;
            await LoadDepartmentsAsync();
            return Page();
        }
        if (string.IsNullOrWhiteSpace(role)) role = "Employee";
        if (string.IsNullOrWhiteSpace(employmentType)) employmentType = "Regular";
        var dateHiredVal = dateHired ?? DateTime.Today;

        if (createAccount)
        {
            var loginEmail = string.IsNullOrWhiteSpace(Email) ? ContactEmail?.Trim() : Email.Trim();
            var password = DefaultPassword?.Trim();
            if (string.IsNullOrEmpty(loginEmail) || string.IsNullOrEmpty(password))
            {
                TempData["ToastMessage"] = "Email and default password are required when creating a login account.";
                TempData["ToastSuccess"] = false;
                return Page();
            }
        }

        var resolvedIdPictureUrl = idPictureUrl?.Trim();
        var resolvedSignatureUrl = signatureUrl?.Trim();
        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (profilePicture != null && profilePicture.Length > 0)
        {
            var uploadUrl = await UploadEmployeeImageAsync(client, baseUrl, profilePicture, "photo");
            if (uploadUrl != null) resolvedIdPictureUrl = uploadUrl;
        }
        if (signature != null && signature.Length > 0)
        {
            var uploadUrl = await UploadEmployeeImageAsync(client, baseUrl, signature, "signature");
            if (uploadUrl != null) resolvedSignatureUrl = uploadUrl;
        }

        // 1) Create employee first (with or without UserID). This ensures the employee row is always saved.
        var payload = new
        {
            UserID = (int?)null,
            DepartmentID = departmentID,
            EmpNumber = string.IsNullOrWhiteSpace(empNumber) ? null : empNumber.Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            Age = age,
            Gender = string.IsNullOrWhiteSpace(gender) ? null : gender.Trim(),
            Role = role.Trim(),
            WorkState = "Active",
            EmploymentType = employmentType.Trim(),
            DateHired = dateHiredVal,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            AddressLine1 = string.IsNullOrWhiteSpace(addressLine1) ? null : addressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            StateProvince = string.IsNullOrWhiteSpace(stateProvince) ? null : stateProvince.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(country) ? "Philippines" : country.Trim(),
            EmergencyContactName = string.IsNullOrWhiteSpace(emergencyContactName) ? null : emergencyContactName.Trim(),
            EmergencyContactPhone = string.IsNullOrWhiteSpace(emergencyContactPhone) ? null : emergencyContactPhone.Trim(),
            EmergencyContactRelation = string.IsNullOrWhiteSpace(emergencyContactRelation) ? null : emergencyContactRelation.Trim(),
            IdPictureUrl = string.IsNullOrWhiteSpace(resolvedIdPictureUrl) ? null : resolvedIdPictureUrl,
            SignatureUrl = string.IsNullOrWhiteSpace(resolvedSignatureUrl) ? null : resolvedSignatureUrl
        };

        HttpResponseMessage resp;
        try
        {
            resp = await client.PostAsJsonAsync(baseUrl + "/api/employees", payload);
        }
        catch (Exception)
        {
            TempData["ToastMessage"] = "Failed to create employee. Check that the API is running.";
            TempData["ToastSuccess"] = false;
            return Page();
        }

        if (!resp.IsSuccessStatusCode)
        {
            var msg = "Failed to create employee.";
            try
            {
                var body = await resp.Content.ReadAsStringAsync();
                if (body.Contains("message", StringComparison.OrdinalIgnoreCase))
                {
                    var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var m))
                        msg = m.GetString() ?? msg;
                }
            }
            catch { /* ignore */ }
            TempData["ToastMessage"] = msg;
            TempData["ToastSuccess"] = false;
            return Page();
        }

        int empId;
        try
        {
            var created = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (!created.TryGetProperty("empId", out var idProp) && !created.TryGetProperty("empID", out idProp))
            {
                TempData["ToastMessage"] = "Employee was created but could not read response. Check Employee Management.";
                TempData["ToastSuccess"] = true;
                return RedirectToPage("/App/Employee/EmployeeManagement");
            }
            empId = idProp.GetInt32();
        }
        catch
        {
            TempData["ToastMessage"] = "Employee was created but could not read response. Check Employee Management.";
            TempData["ToastSuccess"] = true;
            return RedirectToPage("/App/Employee/EmployeeManagement");
        }

        // 2) If "Create account" was checked, create user and link to the new employee.
        if (createAccount)
        {
            var loginEmail = string.IsNullOrWhiteSpace(Email) ? ContactEmail?.Trim() : Email.Trim();
            var password = DefaultPassword?.Trim();
            HttpResponseMessage userResp;
            try
            {
                var userPayload = new
                {
                    Email = loginEmail,
                    Password = password,
                    Name = $"{firstName.Trim()} {lastName.Trim()}".Trim(),
                    AvatarUrl = string.IsNullOrWhiteSpace(resolvedIdPictureUrl) ? null : resolvedIdPictureUrl
                };
                userResp = await client.PostAsJsonAsync(baseUrl + "/api/Auth/create-employee-user", userPayload);
            }
            catch (Exception)
            {
                TempData["ToastMessage"] = "Employee saved. Failed to create login account; you can link an account later from Edit Employee.";
                TempData["ToastSuccess"] = true;
                return RedirectToPage("/App/Employee/EmployeeManagement");
            }

            if (!userResp.IsSuccessStatusCode)
            {
                var msg = "Employee saved. Failed to create login account.";
                try
                {
                    var body = await userResp.Content.ReadAsStringAsync();
                    if (body.Contains("message", StringComparison.OrdinalIgnoreCase))
                    {
                        var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("message", out var m))
                            msg = "Employee saved. " + (m.GetString() ?? "Failed to create login account.");
                    }
                }
                catch { /* ignore */ }
                TempData["ToastMessage"] = msg;
                TempData["ToastSuccess"] = false;
                return RedirectToPage("/App/Employee/EmployeeManagement");
            }

            int createdUserId;
            try
            {
                var userJson = await userResp.Content.ReadFromJsonAsync<JsonElement>();
                if (!userJson.TryGetProperty("userId", out var uid))
                {
                    TempData["ToastMessage"] = "Employee saved. Login account was created but could not be linked; link from Edit Employee.";
                    TempData["ToastSuccess"] = true;
                    return RedirectToPage("/App/Employee/EmployeeManagement");
                }
                createdUserId = uid.GetInt32();
            }
            catch
            {
                TempData["ToastMessage"] = "Employee saved. Login account was created but could not be linked; link from Edit Employee.";
                TempData["ToastSuccess"] = true;
                return RedirectToPage("/App/Employee/EmployeeManagement");
            }

            var updateResp = await client.PutAsJsonAsync(baseUrl + "/api/employees/" + empId.ToString(System.Globalization.CultureInfo.InvariantCulture), new { UserID = createdUserId });
            if (!updateResp.IsSuccessStatusCode)
            {
                TempData["ToastMessage"] = "Employee and login account were created but could not be linked. Link from Edit Employee.";
                TempData["ToastSuccess"] = true;
                return RedirectToPage("/App/Employee/EmployeeManagement");
            }
        }

        TempData["ToastMessage"] = "Employee created successfully.";
        TempData["ToastSuccess"] = true;
        return RedirectToPage("/App/Employee/EmployeeManagement", new { showId = empId });
    }

    private static async Task<string?> UploadEmployeeImageAsync(HttpClient client, string baseUrl, IFormFile file, string type)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "image/jpeg");
            content.Add(fileContent, "file", file.FileName ?? "image");
            var resp = await client.PostAsync(baseUrl + "/api/employees/upload-image?type=" + Uri.EscapeDataString(type), content);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.TryGetProperty("url", out var u) ? u.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    public class DepartmentItem
    {
        public int DepID { get; set; }
        public string DepartmentName { get; set; } = "";
    }

    private class UniqueCheckResponse
    {
        public bool Ok { get; set; }
        public bool Exists { get; set; }
    }
}
