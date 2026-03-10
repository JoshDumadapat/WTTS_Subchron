using System.Net.Http.Json;
using System.Text.Json;
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
    public List<ShiftOption> ShiftOptions { get; set; } = new();
    public List<LocationOption> LocationOptions { get; set; } = new();
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string ApiBaseUrl { get; set; } = "";

    public async Task OnGetAsync()
    {
        ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        await LoadDropdownOptionsAsync();
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

    private async Task LoadDropdownOptionsAsync()
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrEmpty(token)) return;
        var baseUrl = GetApiBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return;

        try
        {
            var client = CreateAuthorizedApiClient(token);
            var shiftsTask = FetchShiftOptionsAsync(client, baseUrl);
            var locationsTask = client.GetFromJsonAsync<List<LocationOption>>(baseUrl + "/api/org-locations/current");
            await Task.WhenAll(shiftsTask, locationsTask);

            ShiftOptions = shiftsTask.Result;

            LocationOptions = (locationsTask.Result ?? new List<LocationOption>())
                .Where(l => l.IsActive)
                .OrderBy(l => l.LocationName)
                .ToList();
        }
        catch
        {
            ShiftOptions = new List<ShiftOption>();
            LocationOptions = new List<LocationOption>();
        }
    }

    private static async Task<List<ShiftOption>> FetchShiftOptionsAsync(HttpClient client, string baseUrl)
    {
        var resp = await client.GetAsync(baseUrl + "/api/org-shift-templates/current");
        if (!resp.IsSuccessStatusCode) return new List<ShiftOption>();

        var json = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return new List<ShiftOption>();

        using var doc = JsonDocument.Parse(json);
        JsonElement templatesEl;
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            templatesEl = doc.RootElement;
        }
        else if (doc.RootElement.TryGetProperty("templates", out var t1))
        {
            templatesEl = t1;
        }
        else if (doc.RootElement.TryGetProperty("Templates", out var t2))
        {
            templatesEl = t2;
        }
        else
        {
            return new List<ShiftOption>();
        }

        if (templatesEl.ValueKind != JsonValueKind.Array) return new List<ShiftOption>();

        var list = new List<ShiftOption>();
        foreach (var item in templatesEl.EnumerateArray())
        {
            var isActive = item.TryGetProperty("isActive", out var ia1) ? ia1.GetBoolean()
                : item.TryGetProperty("IsActive", out var ia2) && ia2.GetBoolean();
            if (!isActive) continue;

            var code = item.TryGetProperty("code", out var c1) ? c1.GetString()
                : item.TryGetProperty("Code", out var c2) ? c2.GetString() : null;
            var name = item.TryGetProperty("name", out var n1) ? n1.GetString()
                : item.TryGetProperty("Name", out var n2) ? n2.GetString() : null;
            var type = item.TryGetProperty("type", out var ty1) ? ty1.GetString()
                : item.TryGetProperty("Type", out var ty2) ? ty2.GetString() : "Fixed";

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

            var timeSummary = BuildTimeSummary(item, type ?? "Fixed");
            var requiredHoursSummary = BuildRequiredHoursSummary(item, type ?? "Fixed");
            list.Add(new ShiftOption
            {
                Code = code,
                Name = name,
                Type = type ?? "Fixed",
                TimeSummary = timeSummary,
                DisplayLabel = $"{name} ({type}) - {timeSummary} | {requiredHoursSummary}",
                IsActive = true
            });
        }

        return list.OrderBy(x => x.Name).ToList();
    }

    private static string BuildTimeSummary(JsonElement item, string type)
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

    public async Task<IActionResult> OnPostCreateAsync(string? departmentName, string? defaultShiftTemplateCode, int? defaultLocationId)
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
            var resp = await client.PostAsJsonAsync(baseUrl + "/api/departments", new
            {
                DepartmentName = departmentName.Trim(),
                DefaultShiftTemplateCode = string.IsNullOrWhiteSpace(defaultShiftTemplateCode) ? null : defaultShiftTemplateCode.Trim(),
                DefaultLocationId = defaultLocationId
            });
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

    public async Task<IActionResult> OnPostUpdateAsync(int id, string? departmentName, string? defaultShiftTemplateCode, int? defaultLocationId)
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
            var resp = await client.PutAsJsonAsync(baseUrl + $"/api/departments/{id}", new
            {
                DepartmentName = departmentName.Trim(),
                DefaultShiftTemplateCode = string.IsNullOrWhiteSpace(defaultShiftTemplateCode) ? null : defaultShiftTemplateCode.Trim(),
                DefaultLocationId = defaultLocationId
            });
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
            var trimmedReason = (reason ?? string.Empty).Trim();
            if (trimmedReason.Length > 60)
            {
                TempData["ToastMessage"] = "Reason must be 60 characters or fewer.";
                TempData["ToastSuccess"] = false;
                return RedirectToPage();
            }

            var client = CreateAuthorizedApiClient(token);
            var payload = new { IsActive = isActive, Reason = trimmedReason };
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
        public string? DefaultShiftTemplateCode { get; set; }
        public int? DefaultLocationId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int EmployeeCount { get; set; }
    }

    public class ShiftOption
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Fixed";
        public string TimeSummary { get; set; } = "--:-- - --:--";
        public string DisplayLabel { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class LocationOption
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
