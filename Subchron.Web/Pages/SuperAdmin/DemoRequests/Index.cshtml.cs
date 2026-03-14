using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin.DemoRequests
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public IndexModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public List<DemoRequestViewModel> DemoRequests { get; set; } = new();
        public string ViewMode { get; set; } = "list";
        public DateTime CurrentMonth { get; set; } = DateTime.Now;
        public List<CalendarDay> CalendarDays { get; set; } = new();

        [TempData]
        public string? FlashMessage { get; set; }

        public async Task OnGetAsync(string view = "list", int year = 0, int month = 0)
        {
            ViewMode = view;
            if (year > 0 && month > 0)
                CurrentMonth = new DateTime(year, month, 1);

            await LoadDemoRequestsAsync();
            if (ViewMode == "calendar")
                GenerateCalendarData();
        }

        private async Task LoadDemoRequestsAsync()
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return;

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var rows = await client.GetFromJsonAsync<List<DemoRequestViewModel>>(baseUrl + "/api/demo-requests");
                DemoRequests = rows ?? new List<DemoRequestViewModel>();
            }
            catch
            {
                DemoRequests = new List<DemoRequestViewModel>();
            }
        }

        private void GenerateCalendarData()
        {
            CalendarDays.Clear();

            var firstDayOfMonth = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var firstDayOfCalendar = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);
            var lastDayOfCalendar = lastDayOfMonth.AddDays(6 - (int)lastDayOfMonth.DayOfWeek);

            for (var date = firstDayOfCalendar; date <= lastDayOfCalendar; date = date.AddDays(1))
            {
                var dayRequests = DemoRequests.Where(r => r.CreatedAt.Date == date.Date).ToList();
                CalendarDays.Add(new CalendarDay
                {
                    Date = date,
                    Day = date.Day,
                    IsCurrentMonth = date.Month == CurrentMonth.Month,
                    Requests = dayRequests
                });
            }
        }
    }

    public class DemoRequestViewModel
    {
        public int DemoRequestID { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? OrgSize { get; set; }
        public string? DesiredMode { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserID { get; set; }
        public int? OrgID { get; set; }
    }

    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public int Day { get; set; }
        public bool IsCurrentMonth { get; set; }
        public List<DemoRequestViewModel> Requests { get; set; } = new();
    }
}
