using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Operations
{
    public class ScanStationModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public ScanStationModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public List<StationItem> Stations { get; set; } = new();
        public bool IsScanStationAvailable { get; set; } = true;
        public string PrimaryMode { get; set; } = "QR";

        public async Task OnGetAsync()
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return;

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                Stations = await client.GetFromJsonAsync<List<StationItem>>(baseUrl + "/api/scan-stations/current") ?? new();
                var cfg = await client.GetFromJsonAsync<AttendanceSettingsItem>(baseUrl + "/api/org-attendance-settings/current");
                PrimaryMode = cfg?.PrimaryMode ?? "QR";
                IsScanStationAvailable = !string.Equals(PrimaryMode, "Biometric", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                Stations = new();
                IsScanStationAvailable = true;
            }
        }

        public class StationItem
        {
            public int ScanStationID { get; set; }
            public string StationName { get; set; } = string.Empty;
            public string LocationName { get; set; } = string.Empty;
            public bool QrEnabled { get; set; }
            public bool IdEntryEnabled { get; set; }
            public string ScheduleMode { get; set; } = "Always";
            public bool IsActive { get; set; }
        }

        public class AttendanceSettingsItem
        {
            public string PrimaryMode { get; set; } = "QR";
        }
    }
}
