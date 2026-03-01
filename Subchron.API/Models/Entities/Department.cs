namespace Subchron.API.Models.Entities;

public class Department
{
    public int DepID { get; set; }
    public int OrgID { get; set; }

    public string DepartmentName { get; set; } = null!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
