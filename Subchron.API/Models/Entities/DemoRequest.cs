namespace Subchron.API.Models.Entities;

public class DemoRequest
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
    public int? ReviewedByUserID { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? OrgID { get; set; }
}
