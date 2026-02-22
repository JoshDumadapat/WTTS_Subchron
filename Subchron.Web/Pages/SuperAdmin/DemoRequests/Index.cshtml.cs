using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.SuperAdmin.DemoRequests
{
    public class IndexModel : PageModel
    {
        public List<DemoRequestViewModel> DemoRequests { get; set; } = new();
   public string ViewMode { get; set; } = "list"; // list or calendar
     public DateTime CurrentMonth { get; set; } = DateTime.Now;
        public List<CalendarDay> CalendarDays { get; set; } = new();

 public void OnGet(string view = "list", int year = 0, int month = 0)
        {
  ViewMode = view;
     
 if (year > 0 && month > 0)
    CurrentMonth = new DateTime(year, month, 1);
   
            LoadDemoRequests();
            
         if (ViewMode == "calendar")
   GenerateCalendarData();
        }

      private void LoadDemoRequests()
      {
     // Mock data - replace with actual repository calls
   DemoRequests = new List<DemoRequestViewModel>
  {
    new()
  {
       DemoRequestID = 1,
     OrgName = "TechFlow Solutions",
    ContactName = "John Smith",
     Email = "john.smith@techflow.com",
         Phone = "+1-555-0101",
         OrgSize = "50-100",
       DesiredMode = "Biometric",
            Message = "We need a comprehensive time tracking system for our growing team.",
    Status = "Pending",
    CreatedAt = DateTime.Now.AddDays(-2)
            },
new()
       {
      DemoRequestID = 2,
        OrgName = "Digital Marketing Pro",
                ContactName = "Sarah Johnson",
         Email = "sarah@digitalmp.com",
                Phone = "+1-555-0102",
            OrgSize = "10-50",
        DesiredMode = "QR Code",
            Message = "Looking for a simple solution for remote team tracking.",
                Status = "Approved",
               CreatedAt = DateTime.Now.AddDays(-5),
          ReviewedAt = DateTime.Now.AddDays(-1),
    OrgID = 123
          },
  new()
         {
        DemoRequestID = 3,
    OrgName = "Construction Corp",
  ContactName = "Mike Wilson",
        Email = "mike@constructioncorp.com",
       Phone = "+1-555-0103",
       OrgSize = "100+",
     DesiredMode = "Geo Location",
     Message = "Need location tracking for field workers.",
  Status = "Pending",
        CreatedAt = DateTime.Now.AddDays(-1)
                },
  new()
          {
   DemoRequestID = 4,
           OrgName = "StartupXYZ",
           ContactName = "Emily Chen",
    Email = "emily@startupxyz.com",
          Phone = "+1-555-0104",
              OrgSize = "10-50",
       DesiredMode = "QR Code",
   Message = "Small team looking for affordable tracking solution.",
  Status = "Rejected",
            CreatedAt = DateTime.Now.AddDays(-7),
  ReviewedAt = DateTime.Now.AddDays(-3)
  },
            new()
       {
          DemoRequestID = 5,
             OrgName = "Global Services Inc",
     ContactName = "David Brown",
                    Email = "david@globalservices.com",
        Phone = "+1-555-0105",
          OrgSize = "100+",
     DesiredMode = "Biometric",
         Message = "Enterprise solution needed for multiple locations.",
          Status = "Pending",
    CreatedAt = DateTime.Now.AddDays(-10)
       }
  };
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
  public string Phone { get; set; } = string.Empty;
  public string OrgSize { get; set; } = string.Empty;
        public string DesiredMode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
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