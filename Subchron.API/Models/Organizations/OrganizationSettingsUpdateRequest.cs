namespace Subchron.API.Models.Organizations;

public class OrganizationSettingsUpdateRequest
{
    public string Timezone { get; set; } = "Asia/Manila";
    public string Currency { get; set; } = "PHP";
    public string AttendanceMode { get; set; } = "QR";
    public string? DefaultShiftTemplateCode { get; set; }
}
