using Subchron.API.Models.Organizations;

namespace Subchron.API.Models.Entities;

public class OrganizationSettings
{
    public int OrgID { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Timezone { get; set; } = "Asia/Manila";
    public string Currency { get; set; } = "PHP";

    /// <summary>
    /// Kept temporarily while the attendance module migrates to its dedicated table.
    /// </summary>
    public string AttendanceMode { get; set; } = "QR";

    /// <summary>
    /// Legacy default template reference. Full template definitions move to their own table next.
    /// </summary>
    public string? DefaultShiftTemplateCode { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
