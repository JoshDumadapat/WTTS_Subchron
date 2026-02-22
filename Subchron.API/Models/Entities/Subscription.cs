    namespace Subchron.API.Models.Entities;

    public class Subscription
    {
        public int SubscriptionID { get; set; }

        public int OrgID { get; set; }
        public Organization Organization { get; set; } = null!;

        public int PlanID { get; set; }
        public Plan Plan { get; set; } = null!;

        public string AttendanceMode { get; set; } = "QR";
        public decimal BasePrice { get; set; }
        public decimal ModePrice { get; set; }
        public decimal FinalPrice { get; set; }

        public string BillingCycle { get; set; } = "Monthly";
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Trial";
    }
