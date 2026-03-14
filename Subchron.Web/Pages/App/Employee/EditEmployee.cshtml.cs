using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Employee;

public class EditEmployeeModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public EditEmployeeModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public int Id { get; set; }
    public EditEmployeeDto? Employee { get; set; }
    public List<DepartmentItem> Departments { get; set; } = new();
    public List<ShiftTemplateItem> ShiftTemplates { get; set; } = new();
    public List<LocationItem> Locations { get; set; } = new();
    public List<DeductionRuleItem> DeductionRules { get; set; } = new();
    public HashSet<int> SelectedDeductionRuleIds { get; set; } = new();

    private string GetApiBaseUrl() => (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
    private string? GetAccessToken() => User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;

    private HttpClient CreateAuthorizedApiClient(string token)
    {
        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (id <= 0) return RedirectToPage("/App/Employee/EmployeeManagement");
        Id = id;
        await LoadFormOptionsAsync();
        Employee = await LoadEmployeeAsync(id);
        if (Employee == null)
        {
            TempData["ToastMessage"] = "Employee not found.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EmployeeManagement");
        }

        await LoadEmployeeDeductionsAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        int id,
        string? submitAction,
        bool hasAccount,
        string? loginEmail,
        string? defaultPassword,
        string firstName, string lastName, string? middleName,
        DateTime? birthDate, string? gender,
        string? empNumber, int? departmentID, DateTime? dateHired,
        string? assignedShiftTemplateCode, int? assignedLocationId,
        string? phone, string? contactEmail,
        string? addressLine1, string? addressLine2,
        string? city, string? stateProvince, string? postalCode, string? country,
        string role, string employmentType, string? workState,
        string? emergencyContactName, string? emergencyContactPhone, string? emergencyContactRelation,
        string? compensationBasisOverride, decimal? basePayAmount, string? customUnitLabel, decimal? customWorkHours,
        int[]? selectedDeductionRuleIds,
        string? idPictureUrl, string? signatureUrl,
        IFormFile? profilePicture, IFormFile? signature)
    {
        if (id <= 0) return RedirectToPage("/App/Employee/EmployeeManagement");

        if (string.Equals(submitAction, "reissue-access", StringComparison.OrdinalIgnoreCase))
            return await ReissueTemporaryAccessAsync(id);

        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            TempData["ToastMessage"] = "Not authenticated.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/Auth/Login");
        }

        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            TempData["ToastMessage"] = "API URL is not configured.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EmployeeManagement");
        }

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            TempData["ToastMessage"] = "First name and last name are required.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EditEmployee", new { id });
        }

        var client = CreateAuthorizedApiClient(token);

        var resolvedIdPictureUrl = string.IsNullOrWhiteSpace(idPictureUrl) ? null : idPictureUrl.Trim();
        var resolvedSignatureUrl = string.IsNullOrWhiteSpace(signatureUrl) ? null : signatureUrl.Trim();

        var uploaded = await UploadEmployeeImagesAsync(client, baseUrl, profilePicture, signature);
        if (!string.IsNullOrWhiteSpace(uploaded.idPictureUrl)) resolvedIdPictureUrl = uploaded.idPictureUrl;
        if (!string.IsNullOrWhiteSpace(uploaded.signatureUrl)) resolvedSignatureUrl = uploaded.signatureUrl;

        var payload = new
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            EmpNumber = string.IsNullOrWhiteSpace(empNumber) ? null : empNumber.Trim(),
            DepartmentID = departmentID,
            AssignedShiftTemplateCode = string.IsNullOrWhiteSpace(assignedShiftTemplateCode) ? null : assignedShiftTemplateCode.Trim(),
            AssignedLocationId = assignedLocationId,
            Role = string.IsNullOrWhiteSpace(role) ? "Employee" : role.Trim(),
            WorkState = string.IsNullOrWhiteSpace(workState) ? "Active" : workState.Trim(),
            EmploymentType = string.IsNullOrWhiteSpace(employmentType) ? "Regular" : employmentType.Trim(),
            CompensationBasisOverride = string.IsNullOrWhiteSpace(compensationBasisOverride) ? "UseOrgDefault" : compensationBasisOverride.Trim(),
            BasePayAmount = basePayAmount,
            CustomUnitLabel = string.IsNullOrWhiteSpace(customUnitLabel) ? null : customUnitLabel.Trim(),
            CustomWorkHours = customWorkHours,
            DateHired = dateHired,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Email = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim(),
            AddressLine1 = string.IsNullOrWhiteSpace(addressLine1) ? null : addressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            StateProvince = string.IsNullOrWhiteSpace(stateProvince) ? null : stateProvince.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(country) ? "Philippines" : country.Trim(),
            BirthDate = birthDate?.Date,
            Gender = string.IsNullOrWhiteSpace(gender) ? null : gender.Trim(),
            EmergencyContactName = string.IsNullOrWhiteSpace(emergencyContactName) ? null : emergencyContactName.Trim(),
            EmergencyContactPhone = string.IsNullOrWhiteSpace(emergencyContactPhone) ? null : emergencyContactPhone.Trim(),
            EmergencyContactRelation = string.IsNullOrWhiteSpace(emergencyContactRelation) ? null : emergencyContactRelation.Trim(),
            IdPictureUrl = resolvedIdPictureUrl,
            SignatureUrl = resolvedSignatureUrl
        };

        try
        {
            var resp = await client.PutAsJsonAsync(baseUrl + "/api/employees/" + id, payload);
            if (!resp.IsSuccessStatusCode)
            {
                var msg = "Failed to update employee.";
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
                catch { }

                TempData["ToastMessage"] = msg;
                TempData["ToastSuccess"] = false;
                return RedirectToPage("/App/Employee/EditEmployee", new { id });
            }

            var deductionIds = (selectedDeductionRuleIds ?? Array.Empty<int>()).Distinct().ToArray();
            var deductionResp = await client.PutAsJsonAsync(baseUrl + "/api/employees/" + id + "/deductions", new
            {
                Items = deductionIds.Select(x => new { DeductionRuleID = x, Mode = "UseRule" }).ToArray()
            });
            if (!deductionResp.IsSuccessStatusCode)
            {
                TempData["ToastMessage"] = "Employee updated, but deduction assignments failed to save.";
                TempData["ToastSuccess"] = false;
                return RedirectToPage("/App/Employee/EditEmployee", new { id });
            }

            // If employee has no account and admin opted to create one from edit page, create and link now.
            var latest = await LoadEmployeeAsync(id);
            if (latest != null && !latest.UserID.HasValue && hasAccount)
            {
                var emailToUse = string.IsNullOrWhiteSpace(loginEmail) ? (string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim()) : loginEmail.Trim();
                var tempPassword = string.IsNullOrWhiteSpace(defaultPassword) ? null : defaultPassword.Trim();

                if (string.IsNullOrWhiteSpace(emailToUse) || string.IsNullOrWhiteSpace(tempPassword))
                {
                    TempData["ToastMessage"] = "Employee updated. Login email and temporary password are required to create account.";
                    TempData["ToastSuccess"] = false;
                    return RedirectToPage("/App/Employee/EditEmployee", new { id });
                }

                var userResp = await client.PostAsJsonAsync(baseUrl + "/api/Auth/create-employee-user", new
                {
                    Email = emailToUse,
                    Password = tempPassword,
                    Name = (firstName.Trim() + " " + lastName.Trim()).Trim(),
                    AvatarUrl = resolvedIdPictureUrl
                });

                if (!userResp.IsSuccessStatusCode)
                {
                    var msg = "Employee updated but failed to create login account.";
                    try
                    {
                        var body = await userResp.Content.ReadAsStringAsync();
                        if (body.Contains("message", StringComparison.OrdinalIgnoreCase))
                        {
                            var doc = JsonDocument.Parse(body);
                            if (doc.RootElement.TryGetProperty("message", out var m))
                                msg = "Employee updated. " + (m.GetString() ?? "Failed to create login account.");
                        }
                    }
                    catch { }

                    TempData["ToastMessage"] = msg;
                    TempData["ToastSuccess"] = false;
                    return RedirectToPage("/App/Employee/EditEmployee", new { id });
                }

                try
                {
                    var createdUser = await userResp.Content.ReadFromJsonAsync<JsonElement>();
                    if (createdUser.TryGetProperty("userId", out var userIdEl) && userIdEl.TryGetInt32(out var createdUserId))
                    {
                        var linkResp = await client.PutAsJsonAsync(baseUrl + "/api/employees/" + id, new { UserID = createdUserId });
                        if (!linkResp.IsSuccessStatusCode)
                        {
                            TempData["ToastMessage"] = "Employee and account created, but linking failed. Please retry reissue access.";
                            TempData["ToastSuccess"] = false;
                            return RedirectToPage("/App/Employee/EditEmployee", new { id });
                        }
                    }
                    else
                    {
                        TempData["ToastMessage"] = "Employee updated, but account response was invalid.";
                        TempData["ToastSuccess"] = false;
                        return RedirectToPage("/App/Employee/EditEmployee", new { id });
                    }
                }
                catch
                {
                    TempData["ToastMessage"] = "Employee updated, but failed to parse account creation response.";
                    TempData["ToastSuccess"] = false;
                    return RedirectToPage("/App/Employee/EditEmployee", new { id });
                }
            }

            TempData["ToastMessage"] = "Employee updated successfully.";
            TempData["ToastSuccess"] = true;
            return RedirectToPage("/App/Employee/EmployeeManagement");
        }
        catch
        {
            TempData["ToastMessage"] = "Failed to update employee. Check that the API is running.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EditEmployee", new { id });
        }
    }

    private async Task<IActionResult> ReissueTemporaryAccessAsync(int id)
    {
        if (id <= 0)
            return RedirectToPage("/App/Employee/EmployeeManagement");

        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            TempData["ToastMessage"] = "Not authenticated.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EditEmployee", new { id });
        }

        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            TempData["ToastMessage"] = "API URL is not configured.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EditEmployee", new { id });
        }

        var employee = await LoadEmployeeAsync(id);
        if (employee == null)
        {
            TempData["ToastMessage"] = "Employee not found.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EmployeeManagement");
        }

        if (!employee.UserID.HasValue)
        {
            TempData["ToastMessage"] = "This employee has no linked login account yet.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EditEmployee", new { id });
        }

        try
        {
            var client = CreateAuthorizedApiClient(token);
            var resp = await client.PostAsJsonAsync(baseUrl + "/api/auth/reissue-employee-temp-access", new { UserID = employee.UserID.Value });
            var message = "Failed to reissue temporary access.";
            var success = resp.IsSuccessStatusCode;

            try
            {
                var body = await resp.Content.ReadAsStringAsync();
                if (body.Contains("message", StringComparison.OrdinalIgnoreCase))
                {
                    var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        message = m.GetString() ?? message;
                }
            }
            catch { }

            TempData["ToastMessage"] = message;
            TempData["ToastSuccess"] = success;
            return RedirectToPage("/App/Employee/EditEmployee", new { id });
        }
        catch
        {
            TempData["ToastMessage"] = "Failed to reissue temporary access. Check that the API is running.";
            TempData["ToastSuccess"] = false;
            return RedirectToPage("/App/Employee/EditEmployee", new { id });
        }
    }

    private async Task<EditEmployeeDto?> LoadEmployeeAsync(int id)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return null;
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return null;

        try
        {
            var client = CreateAuthorizedApiClient(token);
            return await client.GetFromJsonAsync<EditEmployeeDto>(baseUrl + "/api/employees/" + id);
        }
        catch
        {
            return null;
        }
    }

    private async Task LoadFormOptionsAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return;
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return;

        try
        {
            var client = CreateAuthorizedApiClient(token);
            var departmentsTask = FetchDepartmentsAsync(client, baseUrl);
            var shiftsTask = FetchShiftTemplatesAsync(client, baseUrl);
            var locationsTask = FetchLocationsAsync(client, baseUrl);
            var deductionsTask = FetchDeductionRulesAsync(client, baseUrl);
            await Task.WhenAll(departmentsTask, shiftsTask, locationsTask, deductionsTask);
            Departments = departmentsTask.Result;
            ShiftTemplates = shiftsTask.Result;
            Locations = locationsTask.Result;
            DeductionRules = deductionsTask.Result;
        }
        catch
        {
            Departments = new List<DepartmentItem>();
            ShiftTemplates = new List<ShiftTemplateItem>();
            Locations = new List<LocationItem>();
            DeductionRules = new List<DeductionRuleItem>();
        }
    }

    private async Task LoadEmployeeDeductionsAsync(int id)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return;
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return;
        try
        {
            var client = CreateAuthorizedApiClient(token);
            var data = await client.GetFromJsonAsync<EmployeeDeductionsResponse>(baseUrl + "/api/employees/" + id + "/deductions");
            var selected = data?.Selected ?? new List<EmployeeDeductionSelectionItem>();
            SelectedDeductionRuleIds = selected.Select(x => x.DeductionRuleID).ToHashSet();
        }
        catch
        {
            SelectedDeductionRuleIds = new HashSet<int>();
        }
    }

    private async Task<List<DeductionRuleItem>> FetchDeductionRulesAsync(HttpClient client, string baseUrl)
    {
        var orgIdClaim = User.FindFirstValue("orgId");
        if (!int.TryParse(orgIdClaim, out var orgId))
            return new List<DeductionRuleItem>();

        var list = await client.GetFromJsonAsync<List<DeductionRuleItem>>(baseUrl + "/api/organizations/" + orgId + "/pay-components/deductions");
        return (list ?? new List<DeductionRuleItem>()).OrderBy(x => x.Name).ToList();
    }

    private static async Task<List<DepartmentItem>> FetchDepartmentsAsync(HttpClient client, string baseUrl)
    {
        var list = await client.GetFromJsonAsync<List<DepartmentItem>>(baseUrl + "/api/departments");
        return list ?? new List<DepartmentItem>();
    }

    private static async Task<List<ShiftTemplateItem>> FetchShiftTemplatesAsync(HttpClient client, string baseUrl)
    {
        var resp = await client.GetAsync(baseUrl + "/api/org-shift-templates/current");
        if (!resp.IsSuccessStatusCode) return new List<ShiftTemplateItem>();

        var json = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return new List<ShiftTemplateItem>();

        using var doc = JsonDocument.Parse(json);
        JsonElement templatesEl;
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            templatesEl = doc.RootElement;
        else if (doc.RootElement.TryGetProperty("templates", out var t1))
            templatesEl = t1;
        else if (doc.RootElement.TryGetProperty("Templates", out var t2))
            templatesEl = t2;
        else
            return new List<ShiftTemplateItem>();

        if (templatesEl.ValueKind != JsonValueKind.Array) return new List<ShiftTemplateItem>();

        var list = new List<ShiftTemplateItem>();
        foreach (var item in templatesEl.EnumerateArray())
        {
            var isActive = item.TryGetProperty("isActive", out var ia1) ? ia1.GetBoolean()
                : item.TryGetProperty("IsActive", out var ia2) && ia2.GetBoolean();
            if (!isActive) continue;

            var code = item.TryGetProperty("code", out var c1) ? c1.GetString() : item.TryGetProperty("Code", out var c2) ? c2.GetString() : null;
            var name = item.TryGetProperty("name", out var n1) ? n1.GetString() : item.TryGetProperty("Name", out var n2) ? n2.GetString() : null;
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

            list.Add(new ShiftTemplateItem
            {
                Code = code,
                DisplayLabel = name
            });
        }

        return list.OrderBy(x => x.DisplayLabel).ToList();
    }

    private static async Task<List<LocationItem>> FetchLocationsAsync(HttpClient client, string baseUrl)
    {
        var list = await client.GetFromJsonAsync<List<LocationItem>>(baseUrl + "/api/org-locations/current");
        return (list ?? new List<LocationItem>()).Where(x => x.IsActive).OrderBy(x => x.LocationName).ToList();
    }

    private static async Task<(string? idPictureUrl, string? signatureUrl)> UploadEmployeeImagesAsync(HttpClient client, string baseUrl, IFormFile? profilePicture, IFormFile? signature)
    {
        var profileTask = profilePicture != null && profilePicture.Length > 0
            ? UploadEmployeeImageAsync(client, baseUrl, profilePicture, "photo")
            : Task.FromResult<string?>(null);
        var signatureTask = signature != null && signature.Length > 0
            ? UploadEmployeeImageAsync(client, baseUrl, signature, "signature")
            : Task.FromResult<string?>(null);

        await Task.WhenAll(profileTask, signatureTask);
        return (await profileTask, await signatureTask);
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

    public class EditEmployeeDto
    {
        public int EmpID { get; set; }
        public int? UserID { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string? AssignedShiftTemplateCode { get; set; }
        public int? AssignedLocationId { get; set; }
        public string? EmpNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? Role { get; set; }
        public string? WorkState { get; set; }
        public string? EmploymentType { get; set; }
        public string? CompensationBasisOverride { get; set; }
        public decimal BasePayAmount { get; set; }
        public string? CustomUnitLabel { get; set; }
        public decimal? CustomWorkHours { get; set; }
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
        public string? AvatarUrl { get; set; }
        public string? IdPictureUrl { get; set; }
        public string? SignatureUrl { get; set; }
    }

    public class DepartmentItem
    {
        public int DepID { get; set; }
        public string DepartmentName { get; set; } = "";
        public string? DefaultShiftTemplateCode { get; set; }
        public int? DefaultLocationId { get; set; }
    }

    public class ShiftTemplateItem
    {
        public string Code { get; set; } = "";
        public string DisplayLabel { get; set; } = "";
    }

    public class LocationItem
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class DeductionRuleItem
    {
        public int DeductionRuleID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DeductionType { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? ComputeBasedOn { get; set; }
    }

    public class EmployeeDeductionSelectionItem
    {
        public int DeductionRuleID { get; set; }
        public string? Mode { get; set; }
        public decimal? Value { get; set; }
        public string? Notes { get; set; }
    }

    public class EmployeeDeductionsResponse
    {
        public List<DeductionRuleItem> Rules { get; set; } = new();
        public List<EmployeeDeductionSelectionItem> Selected { get; set; } = new();
    }
}
