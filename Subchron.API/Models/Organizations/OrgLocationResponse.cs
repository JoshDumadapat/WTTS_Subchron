namespace Subchron.API.Models.Organizations;

public class OrgLocationResponse
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public bool IsActive { get; set; }
    public string? DeactivationReason { get; set; }
    public string PinColor { get; set; } = "emerald";
}
