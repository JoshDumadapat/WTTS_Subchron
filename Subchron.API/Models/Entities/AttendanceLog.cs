namespace Subchron.API.Models.Entities;

public class AttendanceLog
{
    public int AttendanceID { get; set; }
    public int OrgID { get; set; }
    public int EmpID { get; set; }
    public DateOnly LogDate { get; set; }
    public DateTime? TimeIn { get; set; }
    public DateTime? TimeOut { get; set; }
    public string? MethodIn { get; set; }
    public string? MethodOut { get; set; }
    public decimal? GeoLat { get; set; }
    public decimal? GeoLong { get; set; }
    public string? GeoStatus { get; set; }
    public string? DeviceInfo { get; set; }
    public string? Remarks { get; set; }
}
