using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Subchron.Web.Pages.App.Settings;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public IndexModel(IHttpClientFactory http, IConfiguration config, IWebHostEnvironment env)
    {
        _http = http;
        _config = config;
        _env = env;
    }
    public const string TabProfile = "Profile";
    public const string TabSecurity = "Security";
    public const string TabUpgradePlan = "UpgradePlan";
    public const string TabPersonalization = "Personalization";
    public const string TabResources = "Resources";

    public string CurrentTab { get; set; } = TabProfile;
    public string? LoadError { get; set; }

    public string ApiBaseUrl { get; set; } = "";

    public string ProfileName { get; set; } = "";
    public string ProfileEmail { get; set; } = "";
    public string? ProfileAvatarUrl { get; set; }

    public bool HasEmployee { get; set; }
    public string EmpFirstName { get; set; } = "";
    public string EmpMiddleName { get; set; } = "";
    public string EmpLastName { get; set; } = "";
    public string EmpNumber { get; set; } = "";
    public int? EmpDepartmentID { get; set; }
    public string EmpEmploymentType { get; set; } = "Regular";
    public string EmpEmploymentStatus { get; set; } = "Active";
    public DateTime? EmpDateHired { get; set; }
    public string EmpRole { get; set; } = "";
    public string EmpPhone { get; set; } = "";
    public string EmpAddressLine1 { get; set; } = "";
    public string EmpAddressLine2 { get; set; } = "";
    public string EmpCity { get; set; } = "";
    public string EmpStateProvince { get; set; } = "";
    public string EmpPostalCode { get; set; } = "";
    public string EmpCountry { get; set; } = "";
    public string EmpEmergencyContactName { get; set; } = "";
    public string EmpEmergencyContactPhone { get; set; } = "";
    public string EmpEmergencyContactRelation { get; set; } = "";

    public string PlanName { get; set; } = "Standard";
    public int EmployeesUsed { get; set; } = 42;
    public int EmployeeLimit { get; set; } = 500;
    public string BillingCycle { get; set; } = "Monthly";
    public string NextBillingDate { get; set; } = "—";

    /// <summary>True when the user signed in with Google (or another external provider); password change / forgot password are not available.</summary>
    public bool IsGoogleUser { get; set; }

    public Task OnGetAsync(string? tab = null)
    {
        if (!string.IsNullOrWhiteSpace(tab))
        {
            var t = tab.Trim();
            if (t == TabSecurity || t == TabUpgradePlan || t == TabPersonalization || t == TabProfile || t == TabResources)
                CurrentTab = t;
        }
        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        LoadError = null;
        return Task.CompletedTask;
    }

    public async Task<IActionResult> OnGetBillingCurrentAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Unauthorized" }) { StatusCode = 401 };

        try
        {
            var client = CreateAuthorizedApiClient(token);
            var resp = await client.GetAsync("api/billing/current");
            var body = await resp.Content.ReadAsStringAsync();
            return new ContentResult { Content = body, ContentType = "application/json", StatusCode = (int)resp.StatusCode };
        }
        catch
        {
            return new JsonResult(new { ok = false, message = "Could not fetch billing information." }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostUpdateBillingAsync([FromBody] JsonElement req)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Unauthorized" }) { StatusCode = 401 };

        try
        {
            var client = CreateAuthorizedApiClient(token);
            // Proxy the payload to API (PUT /api/billing)
            var content = new StringContent(req.GetRawText(), System.Text.Encoding.UTF8, "application/json");
            var resp = await client.PutAsync("api/billing", content);
            var body = await resp.Content.ReadAsStringAsync();
            return new ContentResult { Content = body, ContentType = "application/json", StatusCode = (int)resp.StatusCode };
        }
        catch
        {
            return new JsonResult(new { ok = false, message = "Could not update billing information." }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnGetDataAsync(string? tab = null)
    {
        if (!string.IsNullOrWhiteSpace(tab))
        {
            var t = tab.Trim();
            if (t == TabSecurity || t == TabUpgradePlan || t == TabPersonalization || t == TabProfile || t == TabResources)
                CurrentTab = t;
        }
        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        LoadError = null;
        await Task.WhenAll(LoadUsageAsync(), LoadProfileAsync());
        if (CurrentTab == TabProfile)
            await LoadEmployeeInfoAsync();
        return new JsonResult(new
        {
            currentTab = CurrentTab,
            loadError = LoadError,
            isGoogleUser = IsGoogleUser,
            profileName = ProfileName,
            profileEmail = ProfileEmail,
            profileAvatarUrl = ProfileAvatarUrl,
            planName = PlanName,
            employeesUsed = EmployeesUsed,
            employeeLimit = EmployeeLimit,
            billingCycle = BillingCycle,
            nextBillingDate = NextBillingDate,
            hasEmployee = HasEmployee,
            empFirstName = EmpFirstName,
            empMiddleName = EmpMiddleName,
            empLastName = EmpLastName,
            empNumber = EmpNumber,
            empDepartmentID = EmpDepartmentID,
            empEmploymentType = EmpEmploymentType,
            empEmploymentStatus = EmpEmploymentStatus,
            empDateHired = EmpDateHired,
            empRole = EmpRole,
            empPhone = EmpPhone,
            empAddressLine1 = EmpAddressLine1,
            empAddressLine2 = EmpAddressLine2,
            empCity = EmpCity,
            empStateProvince = EmpStateProvince,
            empPostalCode = EmpPostalCode,
            empCountry = EmpCountry,
            empEmergencyContactName = EmpEmergencyContactName,
            empEmergencyContactPhone = EmpEmergencyContactPhone,
            empEmergencyContactRelation = EmpEmergencyContactRelation
        });
    }

    private string? GetAccessToken() => User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;

    private HttpClient CreateAuthorizedApiClient(string token)
    {
        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task LoadProfileAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return;
        try
        {
            var client = CreateAuthorizedApiClient(token);
            var resp = await client.GetAsync("api/auth/profile");
            if (!resp.IsSuccessStatusCode) return;
            var data = await resp.Content.ReadFromJsonAsync<ProfileResponse>();
            if (data == null) return;
            ProfileName = data.Name ?? "";
            ProfileEmail = data.Email ?? "";
            ProfileAvatarUrl = data.AvatarUrl;
            IsGoogleUser = string.Equals(data.LoginProvider, "Google", StringComparison.OrdinalIgnoreCase);
        }
        catch { LoadError ??= "Some profile details could not be loaded."; }
    }

    private sealed class ProfileResponse
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string? LoginProvider { get; set; }
    }

    private async Task LoadEmployeeInfoAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return;
        try
        {
            var client = CreateAuthorizedApiClient(token);
            var resp = await client.GetAsync("api/auth/employee-info");
            if (!resp.IsSuccessStatusCode) return;
            var data = await resp.Content.ReadFromJsonAsync<EmployeeInfoResponse>();
            if (data == null || !data.HasEmployee) return;
            HasEmployee = true;
            EmpFirstName = data.FirstName ?? "";
            EmpMiddleName = data.MiddleName ?? "";
            EmpLastName = data.LastName ?? "";
            EmpNumber = data.EmpNumber ?? "";
            EmpDepartmentID = data.DepartmentID;
            EmpEmploymentType = data.EmploymentType ?? "Regular";
            EmpEmploymentStatus = data.WorkState ?? "Active";
            EmpDateHired = data.DateHired;
            EmpRole = data.Role ?? "";
            EmpPhone = data.Phone ?? "";
            EmpAddressLine1 = data.AddressLine1 ?? "";
            EmpAddressLine2 = data.AddressLine2 ?? "";
            EmpCity = data.City ?? "";
            EmpStateProvince = data.StateProvince ?? "";
            EmpPostalCode = data.PostalCode ?? "";
            EmpCountry = data.Country ?? "";
            EmpEmergencyContactName = data.EmergencyContactName ?? "";
            EmpEmergencyContactPhone = data.EmergencyContactPhone ?? "";
            EmpEmergencyContactRelation = data.EmergencyContactRelation ?? "";
        }
        catch { LoadError ??= "Some employee details could not be loaded."; }
    }

    private sealed class EmployeeInfoResponse
    {
        public bool HasEmployee { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? EmpNumber { get; set; }
        public int? DepartmentID { get; set; }
        public string? EmploymentType { get; set; }
        public string? WorkState { get; set; }
        public DateTime? DateHired { get; set; }
        public string? Role { get; set; }
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
    }

    private async Task LoadUsageAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return;
        try
        {
            var client = CreateAuthorizedApiClient(token);
            var resp = await client.GetAsync("api/billing/usage");
            if (!resp.IsSuccessStatusCode) return;
            var data = await resp.Content.ReadFromJsonAsync<UsageResponse>();
            if (data == null) return;
            PlanName = data.PlanName ?? "—";
            EmployeesUsed = data.EmployeesUsed;
            EmployeeLimit = data.EmployeeLimit;
            BillingCycle = data.BillingCycle ?? "—";
            NextBillingDate = data.NextBillingDate ?? "—";
        }
        catch { LoadError ??= "Billing usage information could not be loaded."; }
    }

    private sealed class UsageResponse
    {
        public string? PlanName { get; set; }
        public int EmployeesUsed { get; set; }
        public int EmployeeLimit { get; set; }
        public string? BillingCycle { get; set; }
        public string? NextBillingDate { get; set; }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Session expired. Please sign in again." }) { StatusCode = 401 };

        if (string.IsNullOrWhiteSpace(currentPassword))
            return new JsonResult(new { ok = false, message = "Current password is required." }) { StatusCode = 400 };
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            return new JsonResult(new { ok = false, message = "New password must be at least 8 characters." }) { StatusCode = 400 };

        var client = CreateAuthorizedApiClient(token);
        var resp = await client.PostAsJsonAsync("api/auth/change-password", new { currentPassword, newPassword });
        var body = await resp.Content.ReadAsStringAsync();
        var statusCode = (int)resp.StatusCode;
        object? json = null;
        if (!string.IsNullOrEmpty(body))
        {
            try { json = JsonSerializer.Deserialize<JsonElement>(body); }
            catch { json = new { ok = resp.IsSuccessStatusCode }; }
        }
        return new JsonResult(json ?? new { ok = resp.IsSuccessStatusCode }) { StatusCode = statusCode };
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync(string name, IFormFile? avatarFile)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Session expired. Please sign in again." }) { StatusCode = 401 };

        var client = CreateAuthorizedApiClient(token);

        // 1) If there is a file, upload it to API -> Cloudinary
        string? newAvatarUrl = null;

        if (avatarFile != null && avatarFile.Length > 0)
        {
            // Optional safety checks on Web side (API also checks)
            if (avatarFile.Length > 5 * 1024 * 1024)
                return new JsonResult(new { ok = false, message = "File too large (max 5MB)." }) { StatusCode = 400 };

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(avatarFile.ContentType))
                return new JsonResult(new { ok = false, message = "Only JPG/PNG/WEBP images are allowed." }) { StatusCode = 400 };

            using var form = new MultipartFormDataContent();
            await using var stream = avatarFile.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(avatarFile.ContentType);

            // "file" must match API parameter name: UploadAvatar([FromForm] IFormFile file)
            form.Add(fileContent, "file", avatarFile.FileName);

            var uploadResp = await client.PostAsync("api/auth/profile/avatar", form);
            var uploadBody = await uploadResp.Content.ReadAsStringAsync();

            if (!uploadResp.IsSuccessStatusCode)
            {
                // Try return API error JSON if it is JSON
                try
                {
                    var je = JsonSerializer.Deserialize<JsonElement>(uploadBody);
                    return new JsonResult(je) { StatusCode = (int)uploadResp.StatusCode };
                }
                catch
                {
                    return new JsonResult(new { ok = false, message = "Avatar upload failed." })
                    { StatusCode = (int)uploadResp.StatusCode };
                }
            }

            // Parse { ok, avatarUrl }
            try
            {
                var je = JsonSerializer.Deserialize<JsonElement>(uploadBody);
                if (je.TryGetProperty("avatarUrl", out var urlEl))
                    newAvatarUrl = urlEl.GetString();
            }
            catch
            {
                // If parsing fails, treat as failure
                return new JsonResult(new { ok = false, message = "Avatar upload returned invalid response." }) { StatusCode = 500 };
            }
        }

        // 2) Update profile name (and optionally avatarUrl)
        // Since POST /profile/avatar already saves AvatarUrl, we can send avatarUrl only if we want to clear it.
        var trimmedName = name?.Trim();

        object payload;
        if (newAvatarUrl != null)
        {
            // update name + avatar (not strictly necessary, but keeps response consistent)
            payload = new { name = trimmedName, avatarUrl = newAvatarUrl };
        }
        else
        {
            // keep existing avatar as-is; only change name
            payload = new { name = trimmedName };
        }

        var resp = await client.PutAsJsonAsync("api/auth/profile", payload);
        var body = await resp.Content.ReadAsStringAsync();
        var statusCode = (int)resp.StatusCode;

        object? json = null;
        if (!string.IsNullOrEmpty(body))
        {
            try { json = JsonSerializer.Deserialize<JsonElement>(body); }
            catch { json = new { ok = resp.IsSuccessStatusCode }; }
        }

        if (resp.IsSuccessStatusCode && json is JsonElement je2)
        {
            if (je2.TryGetProperty("name", out var nameEl)) ProfileName = nameEl.GetString() ?? ProfileName;
            if (je2.TryGetProperty("avatarUrl", out var urlEl)) ProfileAvatarUrl = urlEl.GetString();
        }
        else if (resp.IsSuccessStatusCode && newAvatarUrl != null)
        {
            // fallback if API didn't echo it
            ProfileAvatarUrl = newAvatarUrl;
        }

        return new JsonResult(json ?? new { ok = resp.IsSuccessStatusCode }) { StatusCode = statusCode };
    }

    public async Task<IActionResult> OnPostUpdateEmployeeInfoAsync(
        IFormFile? avatarFile,
        string? FirstName, string? MiddleName, string? LastName,
        int? DepartmentID, string? EmploymentType, string? WorkState, DateTime? DateHired,
        string? Phone,
        string? AddressLine1, string? AddressLine2, string? City, string? StateProvince, string? PostalCode, string? Country,
        string? EmergencyContactName, string? EmergencyContactPhone, string? EmergencyContactRelation)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = CreateAuthorizedApiClient(token);

        // 1) Upload avatar FIRST (so we fail fast before updating other data)
        if (avatarFile != null && avatarFile.Length > 0)
        {
            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(avatarFile.ContentType))
            {
                TempData["ProfileError"] = "Only JPG/PNG/WEBP images are allowed.";
                return RedirectToPage("/App/Settings/Index", new { tab = TabProfile });
            }

            using var form = new MultipartFormDataContent();
            await using var stream = avatarFile.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(avatarFile.ContentType);

            // "file" MUST match UploadAvatar([FromForm] IFormFile file)
            form.Add(fileContent, "file", avatarFile.FileName);

            var uploadResp = await client.PostAsync("api/auth/profile/avatar", form);
            if (!uploadResp.IsSuccessStatusCode)
            {
                TempData["ProfileError"] = "Failed to upload avatar.";
                return RedirectToPage("/App/Settings/Index", new { tab = TabProfile });
            }
        }

        // 2) Update employee info
        var body = new
        {
            firstName = FirstName,
            middleName = MiddleName,
            lastName = LastName,
            departmentID = DepartmentID,
            employmentType = EmploymentType,
            workState = WorkState,
            dateHired = DateHired,
            phone = Phone,
            addressLine1 = AddressLine1,
            addressLine2 = AddressLine2,
            city = City,
            stateProvince = StateProvince,
            postalCode = PostalCode,
            country = Country,
            emergencyContactName = EmergencyContactName,
            emergencyContactPhone = EmergencyContactPhone,
            emergencyContactRelation = EmergencyContactRelation
        };

        var resp = await client.PutAsJsonAsync("api/auth/employee-info", body);

        if (resp.IsSuccessStatusCode)
            TempData["ProfileMessage"] = "Profile updated.";
        else
            TempData["ProfileError"] = "Failed to update employee information.";

        return RedirectToPage("/App/Settings/Index", new { tab = TabProfile });
    }

    public async Task<IActionResult> OnPostBeginTotpAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Session expired." }) { StatusCode = 401 };

        var client = CreateAuthorizedApiClient(token);

        var resp = await client.PostAsync("api/auth/totp/begin", null);
        var body = await resp.Content.ReadAsStringAsync();
        return new ContentResult { Content = body, ContentType = "application/json", StatusCode = (int)resp.StatusCode };
    }

    public async Task<IActionResult> OnPostVerifyEnableTotpAsync([FromBody] JsonElement req)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Session expired." }) { StatusCode = 401 };

        var totpCode = req.TryGetProperty("totpCode", out var el) ? el.GetString() : null;

        var client = CreateAuthorizedApiClient(token);

        var resp = await client.PostAsJsonAsync("api/auth/totp/verify-enable", new { totpCode });
        var body = await resp.Content.ReadAsStringAsync();
        return new ContentResult { Content = body, ContentType = "application/json", StatusCode = (int)resp.StatusCode };
    }

    public async Task<IActionResult> OnPostDisableTotpAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Session expired." }) { StatusCode = 401 };

        var client = CreateAuthorizedApiClient(token);

        var resp = await client.PostAsync("api/auth/totp/disable", null);
        var body = await resp.Content.ReadAsStringAsync();
        return new ContentResult { Content = body, ContentType = "application/json", StatusCode = (int)resp.StatusCode };
    }

    public async Task<IActionResult> OnGetTotpStatusAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Session expired." }) { StatusCode = 401 };

        var client = CreateAuthorizedApiClient(token);

        var resp = await client.GetAsync("api/auth/totp/status");
        var body = await resp.Content.ReadAsStringAsync();
        return new ContentResult { Content = body, ContentType = "application/json", StatusCode = (int)resp.StatusCode };
    }
}
