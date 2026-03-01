namespace Subchron.API.Models.Organizations;

public class OrgLocationStatusRequest
{
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
}
