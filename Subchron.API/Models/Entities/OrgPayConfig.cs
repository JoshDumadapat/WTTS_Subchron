using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Entities;

public class OrgPayConfig
{
    [Key]
    public int OrgID { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "PHP";

    [MaxLength(30)]
    public string PayCycle { get; set; } = "SemiMonthly";

    public decimal HoursPerDay { get; set; } = 8m;

    /// <summary>JSON array describing cutoff windows and release lags.</summary>
    public string CutoffWindowsJson { get; set; } = "[]";

    public bool LockAttendanceAfterCutoff { get; set; } = false;

    [MaxLength(40)]
    public string ThirteenthMonthBasis { get; set; } = "Basic";

    [MaxLength(250)]
    public string ThirteenthMonthNotes { get; set; } = string.Empty;

    // Statutory toggles
    public bool EnableBIR { get; set; } = true;

    [MaxLength(30)]
    public string BIRPeriod { get; set; } = "SemiMonthly";

    public int BIRTableVersion { get; set; } = DateTime.UtcNow.Year;

    public bool EnableSSS { get; set; } = true;
    public decimal SSSEmployerPercent { get; set; } = 8.5m;

    public bool EnablePhilHealth { get; set; } = true;
    public decimal PhilHealthRate { get; set; } = 3m;

    public bool EnablePagIbig { get; set; } = true;
    public decimal PagIbigRate { get; set; } = 2m;

    public bool EnableIncomeTax { get; set; } = true;
    public bool ProrateNewHires { get; set; } = true;
    public bool ApplyTaxThreshold { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
