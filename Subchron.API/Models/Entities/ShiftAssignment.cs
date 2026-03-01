using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

public class ShiftAssignment
{
    public int ShiftAssignmentID { get; set; }

    public int OrgID { get; set; }

    public int EmpID { get; set; }
    public Employee Employee { get; set; } = null!;

    [Required]
    public DateTime AssignmentDate { get; set; } // Calendar date

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserID { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserID { get; set; }
}
