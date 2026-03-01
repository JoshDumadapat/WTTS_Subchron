namespace Subchron.API.Models.Organizations;

public class OrgLocationUpdateRequest
{
    public string LocationName { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int RadiusMeters { get; set; } = 50;
    public string? PinColor { get; set; }
}
