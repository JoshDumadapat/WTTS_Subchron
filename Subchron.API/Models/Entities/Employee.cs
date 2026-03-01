using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

public class Employee
{
    public int EmpID { get; set; }

    public int OrgID { get; set; }

    public int? UserID { get; set; }

    public int? DepartmentID { get; set; }

    [Required, MaxLength(40)]
    public string EmpNumber { get; set; } = null!;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = null!;

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = null!;

    [MaxLength(80)]
    public string? MiddleName { get; set; }

    public int? Age { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [Required, MaxLength(40)]
    public string Role { get; set; } = "Employee";

    [Required, MaxLength(40)]
    public string EmploymentType { get; set; } = "Regular";

    [Required, MaxLength(40)]
    public string WorkState { get; set; } = "Active";

    public DateTime? DateHired { get; set; }

    [MaxLength(120)]
    public string? AddressLine1 { get; set; }

    [MaxLength(120)]
    public string? AddressLine2 { get; set; }

    [MaxLength(80)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string? StateProvince { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(80)]
    public string Country { get; set; } = "Philippines";

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? PhoneNormalized { get; set; }

    [MaxLength(120)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(30)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(60)]
    public string? EmergencyContactRelation { get; set; }

    public bool IsArchived { get; set; } = false;

    public DateTime? ArchivedAt { get; set; }

    [MaxLength(200)]
    public string? ArchivedReason { get; set; }

    public int? ArchivedByUserId { get; set; }

    public DateTime? RestoredAt { get; set; }

    [MaxLength(200)]
    public string? RestoreReason { get; set; }

    public int? RestoredByUserId { get; set; }

    [MaxLength(64)]
    public string? AttendanceQrToken { get; set; }
    public DateTime? AttendanceQrIssuedAt { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(500)]
    public string? IdPictureUrl { get; set; }

    [MaxLength(500)]
    public string? SignatureUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
}