using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;
using Subchron.Web.Services;

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
    public string OrgName { get; set; } = "Your Company";
    public string? OrgLogoUrl { get; set; }

    public async Task OnGetAsync()
    {
        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        Error = null;
        Employees = new List<EmployeeItem>();
        Departments = new List<DepartmentItem>();

        var claimOrgName = User.FindFirst(CompleteLoginModel.OrgNameClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(claimOrgName))
            OrgName = claimOrgName.Trim();

        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            var branding = await GetOrgBrandingAsync(token);
            OrgName = branding.OrgName;
            OrgLogoUrl = branding.OrgLogoUrl;
        }
    }

    public async Task<IActionResult> OnGetDataAsync()
    {
        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        await Task.WhenAll(
            LoadDepartmentsAsync(),
            LoadEmployeesAsync(archivedOnly: false)
        );
        return new JsonResult(new { employees = Employees, departments = Departments });
    }

    private string GetApiBaseUrl() => (_config["ApiBaseUrl"] ?? "").TrimEnd('/');

    private HttpClient CreateAuthorizedApiClient(string token)
    {
        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
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
        catch { }
        return null;
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
        catch
        {
            Departments = new List<DepartmentItem>();
            Error ??= "Unable to load departments right now.";
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
            var client = CreateAuthorizedApiClient(token);
            var url = baseUrl + "/api/employees" + (archivedOnly ? "?archivedOnly=true" : "");
            var list = await client.GetFromJsonAsync<List<EmployeeItem>>(url);
            Employees = list ?? new List<EmployeeItem>();
        }
        catch
        {
            Employees = new List<EmployeeItem>();
            Error ??= "Unable to load employees right now.";
        }
    }

    private async Task<(string OrgName, string? OrgLogoUrl)> GetOrgBrandingAsync(string token)
    {
        var orgName = User.FindFirst(CompleteLoginModel.OrgNameClaimType)?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(orgName))
            orgName = "Your Company";
        string? orgLogo = null;

        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return (orgName, orgLogo);

        try
        {
            var client = CreateAuthorizedApiClient(token);
            var profile = await client.GetFromJsonAsync<OrgProfileDto>(baseUrl + "/api/org-profile/current");
            if (profile != null)
            {
                if (!string.IsNullOrWhiteSpace(profile.OrgName))
                    orgName = profile.OrgName.Trim();
                if (!string.IsNullOrWhiteSpace(profile.LogoUrl))
                    orgLogo = profile.LogoUrl;
            }
        }
        catch { }

        return (orgName, orgLogo);
    }

    private async Task<byte[]?> DownloadImageAsync(HttpClient client, string? url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var target = url.Trim();
        if (!target.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(baseUrl))
            target = baseUrl.TrimEnd('/') + (target.StartsWith("/") ? string.Empty : "/") + target;

        try
        {
            var resp = await client.GetAsync(target);
            if (!resp.IsSuccessStatusCode)
                return null;
            return await resp.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            return null;
        }
    }

    // Proxies API GET /api/employees/{id}/attendance-qr and returns PNG for the Generate QR modal.
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
            var client = CreateAuthorizedApiClient(token);
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
            var client = CreateAuthorizedApiClient(token);
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

    /// <summary>
    /// Generates and returns employee ID card as PDF (front + back) using QuestPDF. Triggered by Print / PDF button.
    /// </summary>
    public async Task<IActionResult> OnGetDownloadIdPdfAsync(int id)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token))
            return NotFound();
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            return NotFound();
        try
        {
            var client = CreateAuthorizedApiClient(token);
            var empResp = await client.GetFromJsonAsync<EmployeeItem>(baseUrl + "/api/employees/" + id);
            if (empResp is null)
                return NotFound();

            var branding = await GetOrgBrandingAsync(token);

            byte[]? qrBytes = null;
            var qrResp = await client.GetAsync(baseUrl + $"/api/employees/{id}/attendance-qr");
            if (qrResp.IsSuccessStatusCode)
                qrBytes = await qrResp.Content.ReadAsByteArrayAsync();
            var photoBytes = await DownloadImageAsync(client, empResp.IdPictureUrl ?? empResp.AvatarUrl, baseUrl);
            var signatureBytes = await DownloadImageAsync(client, empResp.SignatureUrl, baseUrl);
            byte[]? orgLogoBytes = null;
            if (!string.IsNullOrWhiteSpace(branding.OrgLogoUrl))
                orgLogoBytes = await DownloadImageAsync(client, branding.OrgLogoUrl, baseUrl);

            var fullName = string.Join(" ", new[] { empResp.FirstName, empResp.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = "—";

            var phoneFormatted = "—";
            if (!string.IsNullOrWhiteSpace(empResp.Phone))
            {
                var p = empResp.Phone.Trim();
                phoneFormatted = p.StartsWith("+") ? p : "+63 " + p;
            }

            var address = string.Join(", ", new[] { empResp.AddressLine1, empResp.City, empResp.Country }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            if (string.IsNullOrWhiteSpace(address))
                address = "—";

            var data = new EmployeeIdCardPdf.EmployeeIdCardData
            {
                OrgName = branding.OrgName,
                OrgLogoBytes = orgLogoBytes,
                FullName = fullName,
                EmpNumber = empResp.EmpNumber ?? "—",
                Role = empResp.Role ?? "—",
                Email = string.IsNullOrWhiteSpace(empResp.Email) ? "—" : empResp.Email,
                PhoneFormatted = phoneFormatted,
                EmergencyContactName = string.IsNullOrWhiteSpace(empResp.EmergencyContactName) ? "—" : empResp.EmergencyContactName,
                EmergencyContactPhone = string.IsNullOrWhiteSpace(empResp.EmergencyContactPhone) ? "—" : empResp.EmergencyContactPhone,
                EmergencyContactRelation = string.IsNullOrWhiteSpace(empResp.EmergencyContactRelation) ? "—" : empResp.EmergencyContactRelation,
                Address = address,
                PhotoBytes = photoBytes,
                QrBytes = qrBytes,
                SignatureBytes = signatureBytes
            };

            var pdfBytes = EmployeeIdCardPdf.Generate(data);
            var fileName = "employee-id-" + (empResp.EmpNumber ?? id.ToString()) + ".pdf";
            Response.Headers.Append("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Generates PDF from front/back card images captured via html2canvas so the PDF matches the modal exactly.
    /// </summary>
    public async Task<IActionResult> OnPostDownloadIdPdfFromImagesAsync(int id, string? frontImageBase64, string? backImageBase64)
    {
        if (string.IsNullOrWhiteSpace(frontImageBase64) || string.IsNullOrWhiteSpace(backImageBase64))
            return BadRequest("Missing front or back image.");

        byte[] frontBytes = DecodeBase64Image(frontImageBase64);
        byte[] backBytes = DecodeBase64Image(backImageBase64);
        if (frontBytes == null || frontBytes.Length == 0 || backBytes == null || backBytes.Length == 0)
            return BadRequest("Invalid image data.");

        try
        {
            var pdfBytes = EmployeeIdCardPdf.GenerateFromImages(frontBytes, backBytes);
            var fileName = "employee-id-" + id + ".pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch
        {
            return StatusCode(500);
        }
    }

    public IActionResult OnPostDownloadBatchIdsFromImages(string? cardsJson)
    {
        if (string.IsNullOrWhiteSpace(cardsJson))
            return BadRequest("Missing cards payload.");

        List<BatchIdCardImageDto>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<List<BatchIdCardImageDto>>(cardsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return BadRequest("Invalid cards payload.");
        }

        if (payload == null || payload.Count == 0)
            return BadRequest("No cards provided.");

        var cardImages = new List<EmployeeIdCardPdf.CardImagePair>();
        foreach (var card in payload)
        {
            var front = DecodeBase64Image(card.FrontImageBase64 ?? string.Empty);
            var back = DecodeBase64Image(card.BackImageBase64 ?? string.Empty);
            if (front is { Length: > 0 } && back is { Length: > 0 })
                cardImages.Add(new EmployeeIdCardPdf.CardImagePair(front, back));
        }

        if (cardImages.Count == 0)
            return BadRequest("No valid cards to render.");

        try
        {
            var pdfBytes = EmployeeIdCardPdf.GenerateBatchFromImages(cardImages);
            var fileName = "employee-ids-batch-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch
        {
            return StatusCode(500);
        }
    }

    private static byte[]? DecodeBase64Image(string data)
    {
        if (string.IsNullOrWhiteSpace(data)) return null;
        var s = data.Trim();
        var idx = s.IndexOf(',');
        if (idx >= 0) s = s.Substring(idx + 1);
        try { return Convert.FromBase64String(s); }
        catch { return null; }
    }

    private sealed class BatchIdCardImageDto
    {
        public int Id { get; set; }
        public string? FrontImageBase64 { get; set; }
        public string? BackImageBase64 { get; set; }
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
            var client = CreateAuthorizedApiClient(token);
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
                var msg = await TryGetMessageAsync(resp) ?? "Failed to update employee.";
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
            var client = CreateAuthorizedApiClient(token);
            var resp = await client.PutAsJsonAsync(baseUrl + "/api/employees/" + id, new { WorkState = ws });
            if (!resp.IsSuccessStatusCode)
            {
                var msg = await TryGetMessageAsync(resp) ?? "Failed to update work state.";
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
            var client = CreateAuthorizedApiClient(token);
            var resp = await client.PatchAsJsonAsync(baseUrl + $"/api/employees/{id}/archive", new { Reason = r });
            if (!resp.IsSuccessStatusCode)
            {
                var msg = await TryGetMessageAsync(resp) ?? "Failed to archive employee.";
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
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public string? AvatarUrl { get; set; }
        public string? IdPictureUrl { get; set; }
        public string? SignatureUrl { get; set; }
        public string? Email { get; set; }
        public bool IsArchived { get; set; }
    }

    public class DepartmentItem
    {
        public int DepID { get; set; }
        public string DepartmentName { get; set; } = "";
    }

    private class OrgProfileDto
    {
        public string? OrgName { get; set; }
        public string? LogoUrl { get; set; }
    }
}
