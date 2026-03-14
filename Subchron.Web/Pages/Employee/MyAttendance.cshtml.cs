using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.Employee
{
    public class MyAttendanceModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public MyAttendanceModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnGetRecordAsync(DateTime? from, DateTime? to)
            => await ProxyGetAsync($"/api/shift-schedule/my/attendance?from={(from ?? DateTime.UtcNow.Date.AddDays(-14)):O}&to={(to ?? DateTime.UtcNow.Date):O}");

        public async Task<IActionResult> OnGetScheduleAsync(DateTime? from, DateTime? to)
            => await ProxyGetAsync($"/api/shift-schedule/my/schedule?from={(from ?? DateTime.UtcNow.Date):O}&to={(to ?? DateTime.UtcNow.Date.AddDays(6)):O}");

        public async Task<IActionResult> OnGetAssignedShiftsAsync(DateTime? from, DateTime? to)
            => await ProxyGetAsync($"/api/shift-schedule/my/assigned-shifts?from={(from ?? DateTime.UtcNow.Date):O}&to={(to ?? DateTime.UtcNow.Date.AddDays(30)):O}");

        private async Task<IActionResult> ProxyGetAsync(string path)
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return new JsonResult(new List<object>());

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var resp = await client.GetAsync(baseUrl + path);
                var body = await resp.Content.ReadAsStringAsync();
                return new ContentResult { StatusCode = (int)resp.StatusCode, ContentType = "application/json", Content = string.IsNullOrWhiteSpace(body) ? "[]" : body };
            }
            catch
            {
                return new JsonResult(new List<object>());
            }
        }
    }
}
