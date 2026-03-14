using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

public class SuperAdminExpense
{
    public int Id { get; set; }

    public DateTime OccurredAt { get; set; }

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [Required, MaxLength(100)]
    public string ReferenceNumber { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Tin { get; set; }

    public decimal TaxAmount { get; set; }

    [MaxLength(80)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
