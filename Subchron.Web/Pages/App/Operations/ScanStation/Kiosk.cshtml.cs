using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Operations.ScanStation
{
    [IgnoreAntiforgeryToken]
    public class KioskModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public KioskModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        [FromQuery(Name = "id")]
        public int StationId { get; set; }

        public string StationName { get; set; } = "Scan Station";
        public string SiteName { get; set; } = "Assigned Site";
        public bool IdEntryEnabled { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (StationId <= 0)
                return RedirectToPage("/App/Operations/ScanStation");

            var station = await GetStationAsync();
            if (station is null)
                return RedirectToPage("/App/Operations/ScanStation");

            StationName = station.StationName;
            SiteName = station.LocationName;
            IdEntryEnabled = station.IdEntryEnabled;
            return Page();
        }

        public async Task<IActionResult> OnPostValidateLocationAsync()
        {
            var payload = await JsonSerializer.DeserializeAsync<LocationCheckRequest>(Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = await ProxyPostAsync($"/api/scan-stations/current/{StationId}/validate-location", payload ?? new LocationCheckRequest());
            return result;
        }

        public async Task<IActionResult> OnPostScanAsync()
        {
            var payload = await JsonSerializer.DeserializeAsync<ScanRequest>(Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = await ProxyPostAsync($"/api/scan-stations/current/{StationId}/scan", payload ?? new ScanRequest());
            return result;
        }

        private async Task<StationDto?> GetStationAsync()
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return null;
            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                return await client.GetFromJsonAsync<StationDto>(baseUrl + $"/api/scan-stations/current/{StationId}");
            }
            catch
            {
                return null;
            }
        }

        private async Task<IActionResult> ProxyPostAsync(string apiPath, object payload)
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return new JsonResult(new { ok = false, message = "Not authenticated." }) { StatusCode = 401 };

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await client.PostAsJsonAsync(baseUrl + apiPath, payload);
                var content = await response.Content.ReadAsStringAsync();
                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    ContentType = "application/json",
                    Content = string.IsNullOrWhiteSpace(content) ? "{}" : content
                };
            }
            catch
            {
                return new JsonResult(new { ok = false, message = "Scan request failed." }) { StatusCode = 500 };
            }
        }

        public class StationDto
        {
            public string StationName { get; set; } = string.Empty;
            public string LocationName { get; set; } = string.Empty;
            public bool IdEntryEnabled { get; set; }
        }

        public class LocationCheckRequest
        {
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
        }

        public class ScanRequest
        {
            public string? QrData { get; set; }
            public string? EmployeeIdInput { get; set; }
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
            public string? DeviceInfo { get; set; }
            public string? DeviceTimestamp { get; set; }
            public bool TestMode { get; set; }
        }
    }
}
