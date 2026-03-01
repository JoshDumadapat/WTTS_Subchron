namespace Subchron.API.Models.Entities;

public class LeaveType
{
    public int LeaveTypeID { get; set; }
    public int OrgID { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public decimal DefaultDaysPerYear { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
