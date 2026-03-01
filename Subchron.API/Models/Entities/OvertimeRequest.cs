namespace Subchron.API.Models.Entities;

public class OvertimeRequest
{
    public int OTRequestID { get; set; }
    public int OrgID { get; set; }
    public int EmpID { get; set; }
    public DateOnly OTDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal TotalHours { get; set; }
    public string? Reason { get; set; }
    public int? ApprovedByUserID { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? ApprovedAt { get; set; }
}
