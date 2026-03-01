namespace Subchron.API.Models.Organizations;

public class OrgProfileResponse
{
    public int OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? PrimaryUserEmail { get; set; }
    public string? BillingEmail { get; set; }
    public string? BillingPhone { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
