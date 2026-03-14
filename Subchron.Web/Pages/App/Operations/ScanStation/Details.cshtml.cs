using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Operations.ScanStation
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public DetailsModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        [FromQuery(Name = "id")]
        public int StationId { get; set; }

        public StationItem? Station { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (StationId <= 0)
                return RedirectToPage("/App/Operations/ScanStation");

            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return RedirectToPage("/App/Operations/ScanStation");

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                Station = await client.GetFromJsonAsync<StationItem>(baseUrl + $"/api/scan-stations/current/{StationId}");
            }
            catch
            {
                Station = null;
            }

            if (Station is null)
                return RedirectToPage("/App/Operations/ScanStation");

            return Page();
        }

        public class StationItem
        {
            public int ScanStationID { get; set; }
            public string StationName { get; set; } = string.Empty;
            public string LocationName { get; set; } = string.Empty;
            public bool QrEnabled { get; set; }
            public bool IdEntryEnabled { get; set; }
        }
    }
}
