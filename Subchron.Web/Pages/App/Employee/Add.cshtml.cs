using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;
using System.Security.Claims;

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
    public List<ShiftTemplateItem> ShiftTemplates { get; set; } = new();
    public List<LocationItem> Locations { get; set; } = new();
    public List<DeductionRuleItem> DeductionRules { get; set; } = new();
    public string? Error { get; set; }
    public string? ToastMessage { get; set; }
    public bool? ToastSuccess { get; set; }
    private string GetApiBaseUrl() => (_config["ApiBaseUrl"] ?? "").TrimEnd('/');

    // Employee Management - API client helpers
    private string? GetAccessToken() => User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;

    // Employee Management - API client helpers
    private HttpClient CreateAuthorizedApiClient(string token)
    {
        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task OnGetAsync()
    {
        await LoadFormOptionsAsync();
    }

    public async Task<IActionResult> OnGetNextEmpNumberAsync(string? role)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { empNumber = "" });
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return new JsonResult(new { empNumber = "" });
        try
        {
            var client = CreateAuthorizedApiClient(token);
            return new JsonResult(new { empNumber = await FetchNextEmpNumberAsync(client, baseUrl, role) });
        }
        catch
        {
            return new JsonResult(new { empNumber = "" });
        }
    }

    public async Task<IActionResult> OnGetCheckUniqueAsync(string type, string value, string? scope)
    {
        var token = GetAccessToken();
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
            var client = CreateAuthorizedApiClient(token);
            var result = await CheckUniqueAsync(client, baseUrl, kind, val, string.IsNullOrWhiteSpace(scope) ? null : scope.Trim());
            return new JsonResult(new { ok = result.ok, exists = result.exists });
        }
        catch
        {
            return new JsonResult(new { ok = false, exists = false });
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

    public async Task<IActionResult> OnPostCreateAsync(
        string firstName, string lastName, string? middleName,
        DateTime? birthDate, string? gender,
        string? empNumber, int? departmentID, DateTime? dateHired,
        string? assignedShiftTemplateCode, int? assignedLocationId,
        string? phone, string addressLine1, string? addressLine2,
        string city, string? stateProvince, string? postalCode, string country,
        string role, string employmentType,
        string? compensationBasisOverride, decimal? basePayAmount, string? customUnitLabel, decimal? customWorkHours,
        string? emergencyContactName, string? emergencyContactPhone, string? emergencyContactRelation,
        int[]? selectedDeductionRuleIds,
        bool createAccount, string? ContactEmail, string? Email, string? DefaultPassword,
        string? idPictureUrl, string? signatureUrl,
        IFormFile? profilePicture, IFormFile? signature)
    {
        await LoadFormOptionsAsync();

        var token = GetAccessToken();
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
        if (string.IsNullOrWhiteSpace(empNumber))
        {
            TempData["ToastMessage"] = "Employee ID is required.";
            TempData["ToastSuccess"] = false;
            return Page();
        }
        DateTime? normalizedBirthDate = null;
        if (birthDate.HasValue)
        {
            var bd = birthDate.Value.Date;
            if (bd > DateTime.Today)
            {
                TempData["ToastMessage"] = "Birthdate cannot be in the future.";
                TempData["ToastSuccess"] = false;
                return Page();
            }

            var ageYears = CalculateAge(bd, DateTime.Today);
            if (ageYears < 19 || ageYears > 70)
            {
                TempData["ToastMessage"] = "Birthdate must make employee between 19 and 70 years old.";
                TempData["ToastSuccess"] = false;
                await LoadFormOptionsAsync();
                return Page();
            }

            normalizedBirthDate = bd;
        }
        if (string.IsNullOrWhiteSpace(role)) role = "Employee";
        if (string.IsNullOrWhiteSpace(employmentType)) employmentType = "Regular";
        var dateHiredVal = dateHired ?? DateTime.Today;
        var client = CreateAuthorizedApiClient(token);

        var contactEmail = string.IsNullOrWhiteSpace(ContactEmail) ? null : ContactEmail.Trim();
        if (string.IsNullOrWhiteSpace(contactEmail))
        {
            TempData["ToastMessage"] = "Email is required.";
            TempData["ToastSuccess"] = false;
            return Page();
        }

        if (createAccount)
        {
            var loginEmail = string.IsNullOrWhiteSpace(Email) ? contactEmail : Email.Trim();
            var password = DefaultPassword?.Trim();
            if (string.IsNullOrEmpty(loginEmail) || string.IsNullOrEmpty(password))
            {
                TempData["ToastMessage"] = "Email and default password are required when creating a login account.";
                TempData["ToastSuccess"] = false;
                return Page();
            }

            var uniqueLoginCheck = await CheckUniqueAsync(client, baseUrl, "email", loginEmail, "global-user");
            if (!uniqueLoginCheck.ok)
            {
                TempData["ToastMessage"] = "Unable to validate login email right now. Please try again.";
                TempData["ToastSuccess"] = false;
                return Page();
            }

            if (uniqueLoginCheck.exists)
            {
                TempData["ToastMessage"] = "This email already has an existing account (possibly from another organization or SuperAdmin). Uncheck 'Create login account' to save employee only, or use a different email for the login account.";
                TempData["ToastSuccess"] = false;
                return Page();
            }
        }

        var resolvedIdPictureUrl = idPictureUrl?.Trim();
        var resolvedSignatureUrl = signatureUrl?.Trim();
        var uploadedImages = await UploadEmployeeImagesAsync(client, baseUrl, profilePicture, signature);
        if (!string.IsNullOrWhiteSpace(uploadedImages.idPictureUrl)) resolvedIdPictureUrl = uploadedImages.idPictureUrl;
        if (!string.IsNullOrWhiteSpace(uploadedImages.signatureUrl)) resolvedSignatureUrl = uploadedImages.signatureUrl;
        if (string.IsNullOrWhiteSpace(resolvedSignatureUrl))
        {
            TempData["ToastMessage"] = "Signature is required.";
            TempData["ToastSuccess"] = false;
            return Page();
        }
        if (string.IsNullOrWhiteSpace(resolvedIdPictureUrl))
        {
            TempData["ToastMessage"] = "Profile / ID picture is required.";
            TempData["ToastSuccess"] = false;
            return Page();
        }

        // 1) Create employee first (with or without UserID). This ensures the employee row is always saved.
        var payload = new
        {
            UserID = (int?)null,
            DepartmentID = departmentID,
            AssignedShiftTemplateCode = string.IsNullOrWhiteSpace(assignedShiftTemplateCode) ? null : assignedShiftTemplateCode.Trim(),
            AssignedLocationId = assignedLocationId,
            EmpNumber = string.IsNullOrWhiteSpace(empNumber) ? null : empNumber.Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            BirthDate = normalizedBirthDate,
            Gender = string.IsNullOrWhiteSpace(gender) ? null : gender.Trim(),
            Role = role.Trim(),
            WorkState = "Active",
            EmploymentType = employmentType.Trim(),
            CompensationBasisOverride = string.IsNullOrWhiteSpace(compensationBasisOverride) ? "UseOrgDefault" : compensationBasisOverride.Trim(),
            BasePayAmount = basePayAmount ?? 0,
            CustomUnitLabel = string.IsNullOrWhiteSpace(customUnitLabel) ? null : customUnitLabel.Trim(),
            CustomWorkHours = customWorkHours,
            DateHired = dateHiredVal,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Email = contactEmail,
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
        var deductionIds = (selectedDeductionRuleIds ?? Array.Empty<int>()).Distinct().ToArray();
        if (deductionIds.Length > 0)
        {
            await client.PutAsJsonAsync(baseUrl + "/api/employees/" + empId.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/deductions", new
            {
                Items = deductionIds.Select(x => new { DeductionRuleID = x, Mode = "UseRule" }).ToArray()
            });
        }

        // 2) If "Create account" was checked, create user and link to the new employee.
        if (createAccount)
        {
            var loginEmail = string.IsNullOrWhiteSpace(Email) ? contactEmail : Email.Trim();
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
                var accountConflict = userResp.StatusCode == System.Net.HttpStatusCode.Conflict;
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
                if (accountConflict)
                {
                    msg = "Login account creation failed because this email already has an existing account (possibly from another organization or SuperAdmin). Uncheck 'Create login account' to save employee only, or use a different login email.";
                }
                TempData["ToastMessage"] = msg;
                TempData["ToastSuccess"] = false;
                return Page();
            }

            int createdUserId;
            string? accountCreateMessage = null;
            bool? accountEmailSent = null;
            try
            {
                var userJson = await userResp.Content.ReadFromJsonAsync<JsonElement>();
                if (userJson.TryGetProperty("message", out var messageEl) && messageEl.ValueKind == JsonValueKind.String)
                    accountCreateMessage = messageEl.GetString();
                if (userJson.TryGetProperty("emailSent", out var emailSentEl) && (emailSentEl.ValueKind == JsonValueKind.True || emailSentEl.ValueKind == JsonValueKind.False))
                    accountEmailSent = emailSentEl.GetBoolean();
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

            if (!accountEmailSent.GetValueOrDefault(true))
            {
                TempData["ToastMessage"] = "Employee and login account were created, but temporary access email failed to send. Reissue temporary access from employee actions.";
                TempData["ToastSuccess"] = true;
                return RedirectToPage("/App/Employee/EmployeeManagement");
            }

            if (!string.IsNullOrWhiteSpace(accountCreateMessage))
            {
                TempData["ToastMessage"] = "Employee created. " + accountCreateMessage;
                TempData["ToastSuccess"] = true;
                return RedirectToPage("/App/Employee/EmployeeManagement", new { showId = empId });
            }
        }

        TempData["ToastMessage"] = "Employee created successfully.";
        TempData["ToastSuccess"] = true;
        return RedirectToPage("/App/Employee/EmployeeManagement", new { showId = empId });
    }

    private static int CalculateAge(DateTime birthDate, DateTime asOf)
    {
        var age = asOf.Year - birthDate.Year;
        if (birthDate.Date > asOf.AddYears(-age))
            age--;
        return age;
    }

    // Employee Management - fetching data API functions
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
            var type = item.TryGetProperty("type", out var ty1) ? ty1.GetString() : item.TryGetProperty("Type", out var ty2) ? ty2.GetString() : "Fixed";
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

            var time = BuildShiftTimeSummary(item, type ?? "Fixed");
            var required = BuildRequiredHoursSummary(item, type ?? "Fixed");
            list.Add(new ShiftTemplateItem
            {
                Code = code,
                Name = name,
                Type = type ?? "Fixed",
                TimeSummary = time,
                DisplayLabel = $"{name} ({type}) - {time} | {required}",
                IsActive = true
            });
        }

        return list.OrderBy(x => x.Name).ToList();
    }

    private static string BuildShiftTimeSummary(JsonElement item, string type)
    {
        if (type.Equals("Fixed", StringComparison.OrdinalIgnoreCase))
        {
            var fixedEl = item.TryGetProperty("fixed", out var f1) ? f1 : item.TryGetProperty("Fixed", out var f2) ? f2 : default;
            string? start = null;
            string? end = null;
            if (fixedEl.ValueKind == JsonValueKind.Object)
            {
                start = fixedEl.TryGetProperty("startTime", out var s1) ? s1.GetString() : fixedEl.TryGetProperty("StartTime", out var s2) ? s2.GetString() : null;
                end = fixedEl.TryGetProperty("endTime", out var e1) ? e1.GetString() : fixedEl.TryGetProperty("EndTime", out var e2) ? e2.GetString() : null;
            }
            return $"{(string.IsNullOrWhiteSpace(start) ? "--:--" : start)} - {(string.IsNullOrWhiteSpace(end) ? "--:--" : end)}";
        }

        if (type.Equals("Flexible", StringComparison.OrdinalIgnoreCase))
        {
            var flexEl = item.TryGetProperty("flexible", out var f1) ? f1 : item.TryGetProperty("Flexible", out var f2) ? f2 : default;
            string? start = null;
            string? end = null;
            if (flexEl.ValueKind == JsonValueKind.Object)
            {
                start = flexEl.TryGetProperty("earliestStart", out var s1) ? s1.GetString() : flexEl.TryGetProperty("EarliestStart", out var s2) ? s2.GetString() : null;
                end = flexEl.TryGetProperty("latestEnd", out var e1) ? e1.GetString() : flexEl.TryGetProperty("LatestEnd", out var e2) ? e2.GetString() : null;
            }
            return $"{(string.IsNullOrWhiteSpace(start) ? "--:--" : start)} - {(string.IsNullOrWhiteSpace(end) ? "--:--" : end)}";
        }

        return "No fixed time";
    }

    private static string BuildRequiredHoursSummary(JsonElement item, string type)
    {
        if (type.Equals("Fixed", StringComparison.OrdinalIgnoreCase))
        {
            var fixedEl = item.TryGetProperty("fixed", out var f1) ? f1 : item.TryGetProperty("Fixed", out var f2) ? f2 : default;
            if (fixedEl.ValueKind == JsonValueKind.Object)
            {
                if (TryGetDecimal(fixedEl, "requiredHours", "RequiredHours", out var requiredHours))
                    return $"Required: {requiredHours:0.##} hrs/day";

                var start = fixedEl.TryGetProperty("startTime", out var s1) ? s1.GetString() : fixedEl.TryGetProperty("StartTime", out var s2) ? s2.GetString() : null;
                var end = fixedEl.TryGetProperty("endTime", out var e1) ? e1.GetString() : fixedEl.TryGetProperty("EndTime", out var e2) ? e2.GetString() : null;
                var breakMinutes = TryGetInt(fixedEl, "breakMinutes", "BreakMinutes", out var bm) ? bm : 0;
                var computed = ComputeHoursFromWindow(start, end, breakMinutes);
                if (computed.HasValue)
                    return $"Required: {computed.Value:0.##} hrs/day";
            }
            return "Required: --";
        }

        if (type.Equals("Flexible", StringComparison.OrdinalIgnoreCase))
        {
            var flexEl = item.TryGetProperty("flexible", out var f1) ? f1 : item.TryGetProperty("Flexible", out var f2) ? f2 : default;
            if (flexEl.ValueKind == JsonValueKind.Object && TryGetDecimal(flexEl, "requiredDailyHours", "RequiredDailyHours", out var requiredDaily))
                return $"Required: {requiredDaily:0.##} hrs/day";
            return "Required: --";
        }

        var openEl = item.TryGetProperty("open", out var o1) ? o1 : item.TryGetProperty("Open", out var o2) ? o2 : default;
        if (openEl.ValueKind == JsonValueKind.Object && TryGetDecimal(openEl, "requiredWeeklyHours", "RequiredWeeklyHours", out var requiredWeekly))
            return $"Required: {requiredWeekly:0.##} hrs/week";
        return "Required: --";
    }

    private static bool TryGetDecimal(JsonElement element, string camel, string pascal, out decimal value)
    {
        value = 0;
        if (element.TryGetProperty(camel, out var c)) return TryParseDecimal(c, out value);
        if (element.TryGetProperty(pascal, out var p)) return TryParseDecimal(p, out value);
        return false;
    }

    private static bool TryGetInt(JsonElement element, string camel, string pascal, out int value)
    {
        value = 0;
        if (element.TryGetProperty(camel, out var c)) return TryParseInt(c, out value);
        if (element.TryGetProperty(pascal, out var p)) return TryParseInt(p, out value);
        return false;
    }

    private static bool TryParseDecimal(JsonElement el, out decimal value)
    {
        value = 0;
        if (el.ValueKind == JsonValueKind.Number) return el.TryGetDecimal(out value);
        if (el.ValueKind == JsonValueKind.String) return decimal.TryParse(el.GetString(), out value);
        return false;
    }

    private static bool TryParseInt(JsonElement el, out int value)
    {
        value = 0;
        if (el.ValueKind == JsonValueKind.Number) return el.TryGetInt32(out value);
        if (el.ValueKind == JsonValueKind.String) return int.TryParse(el.GetString(), out value);
        return false;
    }

    private static decimal? ComputeHoursFromWindow(string? start, string? end, int breakMinutes)
    {
        if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end)) return null;
        if (!TimeSpan.TryParse(start, out var s) || !TimeSpan.TryParse(end, out var e)) return null;
        var total = (e - s).TotalMinutes;
        if (total < 0) total += 24 * 60;
        total -= breakMinutes;
        if (total < 0) total = 0;
        return Math.Round((decimal)(total / 60.0), 2);
    }

    private static async Task<List<LocationItem>> FetchLocationsAsync(HttpClient client, string baseUrl)
    {
        var list = await client.GetFromJsonAsync<List<LocationItem>>(baseUrl + "/api/org-locations/current");
        return (list ?? new List<LocationItem>()).Where(x => x.IsActive).OrderBy(x => x.LocationName).ToList();
    }

    private async Task<List<DeductionRuleItem>> FetchDeductionRulesAsync(HttpClient client, string baseUrl)
    {
        var orgIdClaim = User.FindFirstValue("orgId");
        if (!int.TryParse(orgIdClaim, out var orgId))
            return new List<DeductionRuleItem>();

        var list = await client.GetFromJsonAsync<List<DeductionRuleItem>>(baseUrl + "/api/organizations/" + orgId + "/pay-components/deductions");
        return (list ?? new List<DeductionRuleItem>()).OrderBy(x => x.Name).ToList();
    }

    // Employee Management - fetching data API functions
    private static async Task<string> FetchNextEmpNumberAsync(HttpClient client, string baseUrl, string? role)
    {
        var url = baseUrl + "/api/employees/next-number" + (string.IsNullOrEmpty(role) ? "" : "?role=" + Uri.EscapeDataString(role));
        var resp = await client.GetFromJsonAsync<JsonElement>(url);
        return resp.TryGetProperty("empNumber", out var p) ? p.GetString() ?? "" : "";
    }

    // Employee Management - fetching data API functions
    private static async Task<(bool ok, bool exists)> CheckUniqueAsync(HttpClient client, string baseUrl, string type, string value, string? scope = null)
    {
        var url = baseUrl + "/api/employees/check-unique?type=" + Uri.EscapeDataString(type) + "&value=" + Uri.EscapeDataString(value);
        if (!string.IsNullOrWhiteSpace(scope))
            url += "&scope=" + Uri.EscapeDataString(scope);
        var resp = await client.GetFromJsonAsync<UniqueCheckResponse>(url);
        return (resp?.Ok == true, resp?.Exists == true);
    }

    // Employee Management - upload API functions
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
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Fixed";
        public string TimeSummary { get; set; } = "--:-- - --:--";
        public string DisplayLabel { get; set; } = "";
        public bool IsActive { get; set; }
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
        public bool IsActive { get; set; } = true;
    }

    private class UniqueCheckResponse
    {
        public bool Ok { get; set; }
        public bool Exists { get; set; }
    }
}
