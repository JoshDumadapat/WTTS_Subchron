using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Operations.ScanStation
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public CreateModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        [BindProperty]
        public int LocationID { get; set; }
        [BindProperty]
        public string StationName { get; set; } = string.Empty;
        [BindProperty]
        public bool QrEnabled { get; set; } = true;
        [BindProperty]
        public string ScheduleMode { get; set; } = "Always";

        public bool AllowManualEntry { get; set; }
        public string PrimaryMode { get; set; } = "QR";
        public bool IsScanStationAvailable { get; set; } = true;
        public List<LocationItem> Sites { get; set; } = new();
        public string? Error { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadDataAsync();
            if (!IsScanStationAvailable)
            {
                Error = "Attendance capture is set to Biometric + Geofencing. Scan Station is unavailable in this mode.";
                return RedirectToPage("/App/Operations/ScanStation");
            }
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
            {
                Error = "Not authenticated.";
                return Page();
            }

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var resp = await client.PostAsJsonAsync(baseUrl + "/api/scan-stations/current", new
                {
                    LocationID,
                    StationName,
                    QrEnabled,
                    ScheduleMode
                });
                if (!resp.IsSuccessStatusCode)
                {
                    Error = await ExtractMessage(resp) ?? "Unable to create station.";
                    return Page();
                }

                var payload = await resp.Content.ReadFromJsonAsync<CreatedStationResponse>();
                return RedirectToPage("/App/Operations/ScanStation/Details", new { id = payload?.ScanStationID ?? 0 });
            }
            catch
            {
                Error = "Unable to create station right now.";
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return;

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                Sites = await client.GetFromJsonAsync<List<LocationItem>>(baseUrl + "/api/org-locations/current") ?? new();
                var cfg = await client.GetFromJsonAsync<AttendanceConfigResponse>(baseUrl + "/api/org-attendance-settings/current");
                AllowManualEntry = cfg?.AllowManualEntry == true;
                PrimaryMode = cfg?.PrimaryMode ?? "QR";
                IsScanStationAvailable = !string.Equals(PrimaryMode, "Biometric", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                Sites = new();
                AllowManualEntry = false;
                PrimaryMode = "QR";
                IsScanStationAvailable = true;
            }
        }

        private static async Task<string?> ExtractMessage(HttpResponseMessage resp)
        {
            try
            {
                var body = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body) || !body.Contains("message", StringComparison.OrdinalIgnoreCase))
                    return null;
                var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var m))
                    return m.GetString();
            }
            catch { }
            return null;
        }

        public class LocationItem
        {
            public int LocationId { get; set; }
            public string LocationName { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }

        public class AttendanceConfigResponse
        {
            public bool AllowManualEntry { get; set; }
            public string PrimaryMode { get; set; } = "QR";
        }

        public class CreatedStationResponse
        {
            public int ScanStationID { get; set; }
        }
    }
}
