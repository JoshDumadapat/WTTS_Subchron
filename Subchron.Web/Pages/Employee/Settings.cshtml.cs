using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.Employee;

public class SettingsModel : PageModel
{
    private readonly IHttpClientFactory _http;

    public SettingsModel(IHttpClientFactory http)
    {
        _http = http;
    }

    public bool IsGoogleUser { get; set; }
    public string ProfileEmail { get; set; } = string.Empty;
    public string? ProfileAvatarUrl { get; set; }

    public string EmpNumber { get; set; } = string.Empty;
    public string EmpPhone { get; set; } = string.Empty;
    public string EmpAddressLine1 { get; set; } = string.Empty;
    public string EmpAddressLine2 { get; set; } = string.Empty;
    public string EmpCity { get; set; } = string.Empty;
    public string EmpStateProvince { get; set; } = string.Empty;
    public string EmpPostalCode { get; set; } = string.Empty;
    public string EmpCountry { get; set; } = string.Empty;
    public string EmpEmergencyContactName { get; set; } = string.Empty;
    public string EmpEmergencyContactPhone { get; set; } = string.Empty;
    public string EmpEmergencyContactRelation { get; set; } = string.Empty;
    public DateTime? EmpBirthDate { get; set; }
    public string EmpGender { get; set; } = string.Empty;
    public string EmpRole { get; set; } = string.Empty;
    public string EmpDepartmentName { get; set; } = string.Empty;
    public string EmpWorkState { get; set; } = string.Empty;
    public string EmpEmploymentType { get; set; } = string.Empty;
    public DateTime? EmpDateHired { get; set; }
    public string EmpBaseSalaryDisplay { get; set; } = "-";

    public int? EmpAge
    {
        get
        {
            if (!EmpBirthDate.HasValue) return null;
            var today = DateTime.Today;
            var age = today.Year - EmpBirthDate.Value.Year;
            if (EmpBirthDate.Value.Date > today.AddYears(-age)) age--;
            return age < 0 ? 0 : age;
        }
    }

    public async Task OnGetAsync()
    {
        await Task.WhenAll(LoadProfileAsync(), LoadEmployeeInfoAsync());
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

    public async Task<IActionResult> OnPostVerifyCurrentPasswordAsync([FromBody] VerifyCurrentPasswordRequest req)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return new JsonResult(new { ok = false, message = "Session expired. Please sign in again." }) { StatusCode = 401 };

        var currentPassword = req?.CurrentPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(currentPassword))
            return new JsonResult(new { ok = false, message = "Current password is required." }) { StatusCode = 400 };

        var client = CreateAuthorizedApiClient(token);
        var resp = await client.PostAsJsonAsync("api/auth/verify-current-password", new { currentPassword });
        var body = await resp.Content.ReadAsStringAsync();

        if (resp.IsSuccessStatusCode)
            return new JsonResult(new { ok = true });

        try
        {
            var je = JsonSerializer.Deserialize<JsonElement>(body);
            if (je.ValueKind == JsonValueKind.Object && je.TryGetProperty("message", out var msgEl))
            {
                var message = msgEl.GetString() ?? "Current password is incorrect.";
                return new JsonResult(new { ok = false, message }) { StatusCode = 400 };
            }
        }
        catch
        {
            // Fall through with generic message.
        }

        return new JsonResult(new { ok = false, message = "Current password is incorrect." }) { StatusCode = 400 };
    }

    public async Task<IActionResult> OnPostSendPasswordResetLinkAsync()
    {
        var email = await GetCurrentUserEmailAsync();
        if (string.IsNullOrWhiteSpace(email))
            return new JsonResult(new { ok = false, message = "Unable to determine your account email." }) { StatusCode = 400 };

        try
        {
            var client = _http.CreateClient("Subchron.API");
            var resp = await client.PostAsJsonAsync("api/auth/forgot-password", new { email, recaptchaToken = "" });
            var body = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
                return new JsonResult(new { ok = true, message = "Reset link sent. Please check your email." });

            try
            {
                var je = JsonSerializer.Deserialize<JsonElement>(body);
                if (je.ValueKind == JsonValueKind.Object && je.TryGetProperty("message", out var msgEl))
                    return new JsonResult(new { ok = false, message = msgEl.GetString() ?? "Failed to send reset link." }) { StatusCode = (int)resp.StatusCode };
            }
            catch
            {
                // Ignore parse error.
            }

            return new JsonResult(new { ok = false, message = "Failed to send reset link." }) { StatusCode = (int)resp.StatusCode };
        }
        catch
        {
            return new JsonResult(new { ok = false, message = "Network error while sending reset link." }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostUpdateEmployeeInfoAsync(
        IFormFile? avatarFile,
        string? Phone,
        string? AddressLine1,
        string? AddressLine2,
        string? City,
        string? StateProvince,
        string? PostalCode,
        string? Country,
        string? EmergencyContactName,
        string? EmergencyContactPhone,
        string? EmergencyContactRelation)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        try
        {
            var client = CreateAuthorizedApiClient(token);

            if (avatarFile != null && avatarFile.Length > 0)
            {
                var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
                if (!allowed.Contains(avatarFile.ContentType))
                {
                    TempData["ProfileError"] = "Only JPG/PNG/WEBP images are allowed.";
                    return RedirectToPage("/Employee/Settings");
                }

                using var form = new MultipartFormDataContent();
                await using var stream = avatarFile.OpenReadStream();
                using var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(avatarFile.ContentType);
                form.Add(fileContent, "file", avatarFile.FileName);

                var uploadResp = await client.PostAsync("api/auth/profile/avatar", form);
                if (!uploadResp.IsSuccessStatusCode)
                {
                    TempData["ProfileError"] = "Failed to upload profile image.";
                    return RedirectToPage("/Employee/Settings");
                }
            }

            var payload = new
            {
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

            var resp = await client.PutAsJsonAsync("api/auth/employee-info", payload);
            TempData[resp.IsSuccessStatusCode ? "ProfileMessage" : "ProfileError"] =
                resp.IsSuccessStatusCode ? "Profile updated." : "Failed to update profile information.";
        }
        catch
        {
            TempData["ProfileError"] = "Failed to update profile information.";
        }

        return RedirectToPage("/Employee/Settings");
    }

    private string? GetAccessToken() => User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;

    private HttpClient CreateAuthorizedApiClient(string token)
    {
        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string?> GetCurrentUserEmailAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            var client = CreateAuthorizedApiClient(token);
            var resp = await client.GetAsync("api/auth/profile");
            if (!resp.IsSuccessStatusCode) return null;
            var data = await resp.Content.ReadFromJsonAsync<ProfileResponse>();
            return data?.Email?.Trim();
        }
        catch
        {
            return null;
        }
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

            ProfileEmail = data.Email ?? string.Empty;
            ProfileAvatarUrl = data.AvatarUrl;
            IsGoogleUser = string.Equals(data.LoginProvider, "Google", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            IsGoogleUser = false;
        }
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

            EmpNumber = data.EmpNumber ?? string.Empty;
            EmpPhone = data.Phone ?? string.Empty;
            EmpAddressLine1 = data.AddressLine1 ?? string.Empty;
            EmpAddressLine2 = data.AddressLine2 ?? string.Empty;
            EmpCity = data.City ?? string.Empty;
            EmpStateProvince = data.StateProvince ?? string.Empty;
            EmpPostalCode = data.PostalCode ?? string.Empty;
            EmpCountry = data.Country ?? string.Empty;
            EmpEmergencyContactName = data.EmergencyContactName ?? string.Empty;
            EmpEmergencyContactPhone = data.EmergencyContactPhone ?? string.Empty;
            EmpEmergencyContactRelation = data.EmergencyContactRelation ?? string.Empty;
            EmpBirthDate = data.BirthDate;
            EmpGender = data.Gender ?? string.Empty;
            EmpRole = data.Role ?? "Employee";
            EmpDepartmentName = data.DepartmentName ?? "-";
            EmpWorkState = data.WorkState ?? "Active";
            EmpEmploymentType = data.EmploymentType ?? "Regular";
            EmpDateHired = data.DateHired;
            EmpBaseSalaryDisplay = string.IsNullOrWhiteSpace(data.BaseSalary) ? "-" : data.BaseSalary!;
        }
        catch
        {
            // Keep defaults.
        }
    }

    private sealed class ProfileResponse
    {
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string? LoginProvider { get; set; }
    }

    private sealed class EmployeeInfoResponse
    {
        public bool HasEmployee { get; set; }
        public string? EmpNumber { get; set; }
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
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? Role { get; set; }
        public string? DepartmentName { get; set; }
        public string? WorkState { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime? DateHired { get; set; }
        public string? BaseSalary { get; set; }
    }

    public sealed class VerifyCurrentPasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
    }
}
