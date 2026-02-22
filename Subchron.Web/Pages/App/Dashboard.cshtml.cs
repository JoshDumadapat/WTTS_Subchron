using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        // KPI Stats
        public int TotalEmployees { get; set; }
        public int PresentToday { get; set; }
        public int LateArrivals { get; set; }
        public int PendingOTRequests { get; set; }
        
        // Progress metrics
        public int AttendanceRate { get; set; }
        public int OnTimeRate { get; set; }
        public int OTUtilization { get; set; }

        public List<AttendanceSummary> RecentActivity { get; set; } = new();

        public void OnGet()
        {
            LoadMockData();
        }

        private void LoadMockData()
        {
            TotalEmployees = 156;
            PresentToday = 142;
            LateArrivals = 8;
            PendingOTRequests = 14;

            AttendanceRate = 91;
            OnTimeRate = 85;
            OTUtilization = 12;

            RecentActivity = new List<AttendanceSummary>
            {
                new() { Name = "John Doe", Time = "08:15 AM", Status = "Present", Role = "Developer" },
                new() { Name = "Sarah Smith", Time = "08:45 AM", Status = "Late", Role = "Designer" },
                new() { Name = "Mike Johnson", Time = "08:05 AM", Status = "Present", Role = "Manager" },
                new() { Name = "Jane Williams", Time = "09:12 AM", Status = "Late", Role = "QA Engineer" },
                new() { Name = "Robert Brown", Time = "08:10 AM", Status = "Present", Role = "Developer" }
            };
        }
    }

    public class AttendanceSummary
    {
        public string Name { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
