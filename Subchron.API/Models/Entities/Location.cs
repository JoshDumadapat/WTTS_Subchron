namespace Subchron.API.Models.Entities;

public class Location
{
    public int LocationID { get; set; }
    public int OrgID { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public decimal GeoLat { get; set; }
    public decimal GeoLong { get; set; }
    public int RadiusMeters { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DeactivationReason { get; set; }
    public string PinColor { get; set; } = "blue";
}
