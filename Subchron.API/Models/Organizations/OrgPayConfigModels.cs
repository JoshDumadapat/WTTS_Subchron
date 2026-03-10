using System.ComponentModel.DataAnnotations;

namespace Subchron.API.Models.Organizations;

public class OrgPayConfigRequest
{
    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "PHP";

    [Required]
    [MaxLength(30)]
    public string PayCycle { get; set; } = "SemiMonthly";

    [Range(1, 24)]
    public decimal HoursPerDay { get; set; } = 8m;

    public string? CutoffWindowsJson { get; set; }
        = "[]";

    public bool LockAttendanceAfterCutoff { get; set; }
        = false;

    [Required]
    [MaxLength(40)]
    public string ThirteenthMonthBasis { get; set; } = "Basic";

    public string? ThirteenthMonthNotes { get; set; } = string.Empty;

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
}

public class OrgPayConfigResponse
{
    public string Currency { get; set; } = "PHP";
    public string PayCycle { get; set; } = "SemiMonthly";
    public decimal HoursPerDay { get; set; } = 8m;
    public string CutoffWindowsJson { get; set; } = "[]";
    public bool LockAttendanceAfterCutoff { get; set; }
        = false;
    public string ThirteenthMonthBasis { get; set; } = "Basic";
    public string ThirteenthMonthNotes { get; set; } = string.Empty;
    public bool EnableBIR { get; set; } = true;
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
}
