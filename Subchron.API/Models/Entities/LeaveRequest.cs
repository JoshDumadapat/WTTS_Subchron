using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

public class LeaveRequest
{
    public int LeaveRequestID { get; set; }

    public int OrgID { get; set; }
    public Organization Organization { get; set; } = null!;

    public int EmpID { get; set; }
    public Employee Employee { get; set; } = null!;

    [Required, MaxLength(40)]
    public string LeaveType { get; set; } = "Vacation"; // e.g. Vacation, Sick, Emergency

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Declined

    [MaxLength(500)]
    public string? Reason { get; set; }

    public int? ReviewedByUserID { get; set; }
    public User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(300)]
    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserID { get; set; }
}
