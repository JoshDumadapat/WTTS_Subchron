namespace Subchron.API.Models.Entities;

public class ExportJob
{
    public int ExportID { get; set; }
    public int OrgID { get; set; }
    public int ExportedByUserID { get; set; }
    public string ExportType { get; set; } = "CSV";
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}
