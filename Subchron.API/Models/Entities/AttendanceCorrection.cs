namespace Subchron.API.Models.Entities;

public class AttendanceCorrection
{
    public int CorrectionID { get; set; }
    public int OrgID { get; set; }
    public int AttendanceID { get; set; }
    public int RequestedByUserID { get; set; }
    public string? Reasons { get; set; }
    public DateTime? ProposedTimeIn { get; set; }
    public DateTime? ProposedTimeOut { get; set; }
    public string Status { get; set; } = "Pending";
    public int? ReviewedByUserID { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
