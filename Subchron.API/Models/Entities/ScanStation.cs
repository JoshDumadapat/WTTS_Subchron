namespace Subchron.API.Models.Entities;

public class ScanStation
{
    public int ScanStationID { get; set; }
    public int OrgID { get; set; }
    public int LocationID { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public bool QrEnabled { get; set; } = true;
    public bool IdEntryEnabled { get; set; }
    public string ScheduleMode { get; set; } = "Always";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserID { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedByUserID { get; set; }
}
