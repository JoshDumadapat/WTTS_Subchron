using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;

namespace Subchron.API.Controllers;

[ApiController]
[Authorize]
[Route("api/payroll-processing")]
public class PayrollProcessingController : ControllerBase
{
    private readonly TenantDbContext _db;

    public PayrollProcessingController(TenantDbContext db)
    {
        _db = db;
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] PayrollPreviewRequest req, CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var computed = await ComputePayrollAsync(orgId.Value, req, ct);
        return Ok(new PayrollPreviewResponse
        {
            PeriodStart = req.PeriodStart.Date,
            PeriodEnd = req.PeriodEnd.Date,
            Mode = req.Mode,
            Items = computed,
            TotalGross = computed.Sum(x => x.GrossPay),
            TotalDeductions = computed.Sum(x => x.Deductions),
            TotalNet = computed.Sum(x => x.NetPay)
        });
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] PayrollPreviewRequest req, CancellationToken ct)
    {
        var orgId = GetOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var items = await ComputePayrollAsync(orgId.Value, req, ct);
        if (items.Count == 0)
            return BadRequest(new { ok = false, message = "No employees to process for the selected period." });

        var payConfig = await _db.OrgPayConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value, ct);
        var run = new PayrollRun
        {
            OrgID = orgId.Value,
            PeriodStart = req.PeriodStart.Date,
            PeriodEnd = req.PeriodEnd.Date,
            PayCycle = payConfig?.PayCycle ?? "SemiMonthly",
            CompensationBasis = payConfig?.CompensationBasis ?? "Monthly",
            Status = "Processed",
            ProcessedCount = items.Count,
            TotalGrossPay = items.Sum(x => x.GrossPay),
            TotalDeductions = items.Sum(x => x.Deductions),
            TotalNetPay = items.Sum(x => x.NetPay),
            ProcessedAt = DateTime.UtcNow,
            ProcessedByUserID = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Employees = items.Select(i => new PayrollRunEmployee
            {
                OrgID = orgId.Value,
                EmpID = i.EmpID,
                EmpNumber = i.EmpNumber,
                EmployeeName = i.EmployeeName,
                DepartmentName = i.DepartmentName,
                WorkedHours = i.WorkedHours,
                OvertimeHours = i.OvertimeHours,
                BasePay = i.BasePay,
                OvertimePay = i.OvertimePay,
                Allowances = i.Allowances,
                GrossPay = i.GrossPay,
                Deductions = i.Deductions,
                Tax = i.Tax,
                NetPay = i.NetPay,
                FormulaSummary = i.FormulaSummary,
                BreakdownJson = JsonSerializer.Serialize(i.Breakdown),
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        _db.PayrollRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            ok = true,
            runId = run.PayrollRunID,
            processedCount = run.ProcessedCount,
            totalGross = run.TotalGrossPay,
            totalDeductions = run.TotalDeductions,
            totalNet = run.TotalNetPay,
            processedAt = run.ProcessedAt
        });
    }

    [HttpGet("runs")]
    public async Task<IActionResult> Runs(CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var rows = await _db.PayrollRuns.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value)
            .OrderByDescending(x => x.ProcessedAt)
            .Take(100)
            .Select(x => new
            {
                x.PayrollRunID,
                x.PeriodStart,
                x.PeriodEnd,
                x.PayCycle,
                x.ProcessedCount,
                x.TotalGrossPay,
                x.TotalDeductions,
                x.TotalNetPay,
                x.ProcessedAt,
                x.Status
            })
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("runs/{runId:int}")]
    public async Task<IActionResult> RunDetails(int runId, CancellationToken ct)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var run = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PayrollRunID == runId && x.OrgID == orgId.Value, ct);
        if (run == null)
            return NotFound(new { ok = false, message = "Payroll run not found." });

        var employees = await _db.PayrollRunEmployees.AsNoTracking()
            .Where(x => x.PayrollRunID == runId && x.OrgID == orgId.Value)
            .OrderBy(x => x.EmployeeName)
            .ToListAsync(ct);

        return Ok(new
        {
            run,
            employees
        });
    }

    [HttpGet("my/history")]
    public async Task<IActionResult> MyHistory(CancellationToken ct)
    {
        var orgId = GetOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue || !userId.HasValue)
            return Forbid();

        var empId = await _db.Employees.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.UserID == userId.Value && !x.IsArchived)
            .Select(x => (int?)x.EmpID)
            .FirstOrDefaultAsync(ct);
        if (!empId.HasValue)
            return Ok(new List<object>());

        var rows = await _db.PayrollRunEmployees.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.EmpID == empId.Value)
            .Join(_db.PayrollRuns.AsNoTracking(),
                pre => pre.PayrollRunID,
                run => run.PayrollRunID,
                (pre, run) => new
                {
                    run.PayrollRunID,
                    run.PeriodStart,
                    run.PeriodEnd,
                    run.Status,
                    pre.GrossPay,
                    pre.Deductions,
                    pre.Tax,
                    pre.NetPay,
                    pre.WorkedHours,
                    pre.OvertimeHours,
                    pre.BasePay,
                    pre.OvertimePay,
                    pre.Allowances,
                    pre.FormulaSummary,
                    pre.BreakdownJson,
                    run.ProcessedAt
                })
            .OrderByDescending(x => x.ProcessedAt)
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("my/payslip")]
    public async Task<IActionResult> MyPayslip([FromQuery] int runId, CancellationToken ct)
    {
        var orgId = GetOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue || !userId.HasValue)
            return Forbid();

        var empId = await _db.Employees.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.UserID == userId.Value && !x.IsArchived)
            .Select(x => (int?)x.EmpID)
            .FirstOrDefaultAsync(ct);
        if (!empId.HasValue)
            return NotFound(new { ok = false, message = "Employee profile not found." });

        var run = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.PayrollRunID == runId, ct);
        if (run == null)
            return NotFound(new { ok = false, message = "Payroll run not found." });

        var item = await _db.PayrollRunEmployees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.PayrollRunID == runId && x.EmpID == empId.Value, ct);
        if (item == null)
            return NotFound(new { ok = false, message = "Payslip not found." });

        return Ok(new { run, item });
    }

    private async Task<List<PayrollComputedItem>> ComputePayrollAsync(int orgId, PayrollPreviewRequest req, CancellationToken ct)
    {
        var periodStart = req.PeriodStart.Date;
        var periodEnd = req.PeriodEnd.Date;
        if (periodEnd < periodStart)
            (periodStart, periodEnd) = (periodEnd, periodStart);

        var employeesQuery = _db.Employees.AsNoTracking().Where(x => x.OrgID == orgId && !x.IsArchived);
        if (string.Equals(req.Mode, "individual", StringComparison.OrdinalIgnoreCase) && req.EmpID.HasValue)
            employeesQuery = employeesQuery.Where(x => x.EmpID == req.EmpID.Value);

        var employees = await employeesQuery.ToListAsync(ct);
        if (employees.Count == 0)
            return new List<PayrollComputedItem>();

        var empIds = employees.Select(x => x.EmpID).ToList();
        var departments = await _db.Departments.AsNoTracking().Where(x => x.OrgID == orgId)
            .ToDictionaryAsync(x => x.DepID, x => x.DepartmentName, ct);
        var attendance = await _db.AttendanceLogs.AsNoTracking()
            .Where(x => x.OrgID == orgId && empIds.Contains(x.EmpID) && x.LogDate >= DateOnly.FromDateTime(periodStart) && x.LogDate <= DateOnly.FromDateTime(periodEnd))
            .ToListAsync(ct);
        var approvedOt = await _db.OvertimeRequests.AsNoTracking()
            .Where(x => x.OrgID == orgId && empIds.Contains(x.EmpID) && x.Status == "Approved" && x.OTDate >= DateOnly.FromDateTime(periodStart) && x.OTDate <= DateOnly.FromDateTime(periodEnd))
            .ToListAsync(ct);
        var allowances = await _db.OrgAllowanceRules.AsNoTracking().Where(x => x.OrgID == orgId && x.IsActive).ToListAsync(ct);
        var deductionRules = await _db.DeductionRules.AsNoTracking().Where(x => x.OrgID == orgId && x.IsActive).ToListAsync(ct);
        var deductionProfiles = await _db.EmployeeDeductionProfiles.AsNoTracking().Where(x => x.OrgID == orgId && x.IsActive && empIds.Contains(x.EmpID)).ToListAsync(ct);
        var payConfig = await _db.OrgPayConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId, ct);

        var daysInRange = Enumerable.Range(0, (periodEnd - periodStart).Days + 1).Select(d => periodStart.AddDays(d)).ToList();
        var workingDays = daysInRange.Count(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday);
        var hoursPerDay = payConfig?.HoursPerDay > 0 ? payConfig.HoursPerDay : 8m;
        var expectedHours = Math.Max(1m, workingDays * hoursPerDay);

        var output = new List<PayrollComputedItem>();
        foreach (var emp in employees)
        {
            var empAttendance = attendance.Where(x => x.EmpID == emp.EmpID).ToList();
            var workedHours = empAttendance.Sum(a =>
            {
                if (!a.TimeIn.HasValue || !a.TimeOut.HasValue) return 0m;
                var mins = (decimal)(a.TimeOut.Value - a.TimeIn.Value).TotalMinutes;
                return mins > 0 ? mins / 60m : 0m;
            });
            var approvedOtHours = approvedOt.Where(x => x.EmpID == emp.EmpID).Sum(x => x.TotalHours);
            var regularHours = Math.Min(workedHours, expectedHours);

            var basis = NormalizeBasis(emp.CompensationBasisOverride, payConfig?.CompensationBasis);
            var hourlyRate = ComputeHourlyRate(emp, basis, hoursPerDay);
            var basePay = basis switch
            {
                "Hourly" => regularHours * Math.Max(0m, emp.BasePayAmount),
                "Daily" => (workedHours / hoursPerDay) * Math.Max(0m, emp.BasePayAmount),
                "Custom" => (emp.CustomWorkHours.HasValue && emp.CustomWorkHours.Value > 0m)
                    ? (regularHours / emp.CustomWorkHours.Value) * Math.Max(0m, emp.BasePayAmount)
                    : regularHours * hourlyRate,
                _ => Math.Max(0m, emp.BasePayAmount) * (regularHours / expectedHours)
            };

            var overtimePay = approvedOtHours * hourlyRate * 1.25m;

            var daysWorked = empAttendance.Where(x => x.TimeIn.HasValue && x.TimeOut.HasValue).Select(x => x.LogDate).Distinct().Count();
            var allowancePay = allowances.Sum(a =>
            {
                var amt = a.Amount;
                if (a.AttendanceDependent && daysWorked == 0) return 0m;
                if (a.ProrateIfPartialPeriod && workingDays > 0)
                    return amt * (daysWorked / (decimal)workingDays);
                return amt;
            });

            var gross = basePay + overtimePay + allowancePay;
            var selectedProfiles = deductionProfiles.Where(x => x.EmpID == emp.EmpID).ToList();
            var selectedRuleIds = selectedProfiles.Select(x => x.DeductionRuleID).ToHashSet();
            var applicableRules = selectedRuleIds.Count > 0
                ? deductionRules.Where(x => selectedRuleIds.Contains(x.DeductionRuleID)).ToList()
                : deductionRules;

            decimal tax = 0m;
            decimal deductions = 0m;
            var breakdown = new List<PayrollLineItem>();

            foreach (var rule in applicableRules)
            {
                var profile = selectedProfiles.FirstOrDefault(x => x.DeductionRuleID == rule.DeductionRuleID);
                var val = ComputeDeduction(rule, profile, basePay, gross);
                if (val <= 0m) continue;
                deductions += val;
                if (string.Equals(rule.Category, "Tax", StringComparison.OrdinalIgnoreCase) || string.Equals(rule.Name, "Withholding Tax", StringComparison.OrdinalIgnoreCase))
                    tax += val;
                breakdown.Add(new PayrollLineItem { Name = rule.Name, Amount = Math.Round(val, 2), Type = "Deduction" });
            }

            var net = gross - deductions;

            breakdown.Insert(0, new PayrollLineItem { Name = "Base Pay", Amount = Math.Round(basePay, 2), Type = "Earning" });
            breakdown.Insert(1, new PayrollLineItem { Name = "Overtime Pay", Amount = Math.Round(overtimePay, 2), Type = "Earning" });
            breakdown.Insert(2, new PayrollLineItem { Name = "Allowances", Amount = Math.Round(allowancePay, 2), Type = "Earning" });

            output.Add(new PayrollComputedItem
            {
                EmpID = emp.EmpID,
                EmpNumber = emp.EmpNumber,
                EmployeeName = (emp.FirstName + " " + emp.LastName).Trim(),
                DepartmentName = emp.DepartmentID.HasValue && departments.TryGetValue(emp.DepartmentID.Value, out var dn) ? dn : "Unassigned",
                WorkedHours = Math.Round(workedHours, 2),
                OvertimeHours = Math.Round(approvedOtHours, 2),
                BasePay = Math.Round(basePay, 2),
                OvertimePay = Math.Round(overtimePay, 2),
                Allowances = Math.Round(allowancePay, 2),
                GrossPay = Math.Round(gross, 2),
                Deductions = Math.Round(deductions, 2),
                Tax = Math.Round(tax, 2),
                NetPay = Math.Round(net, 2),
                FormulaSummary = "Net = (Base + OT + Allowances) - Deductions",
                Breakdown = breakdown
            });
        }

        return output.OrderBy(x => x.EmployeeName).ToList();
    }

    private static decimal ComputeDeduction(DeductionRule rule, EmployeeDeductionProfile? profile, decimal basePay, decimal grossPay)
    {
        var mode = profile?.Mode ?? "UseRule";
        var amount = rule.Amount ?? 0m;
        if (string.Equals(mode, "Fixed", StringComparison.OrdinalIgnoreCase) && profile?.Value is > 0)
            return profile.Value.Value;
        if (string.Equals(mode, "Percent", StringComparison.OrdinalIgnoreCase) && profile?.Value is > 0)
        {
            var basis = string.Equals(rule.ComputeBasedOn, "GrossPay", StringComparison.OrdinalIgnoreCase) ? grossPay : basePay;
            return basis * (profile.Value.Value / 100m);
        }

        if (string.Equals(rule.DeductionType, "Fixed", StringComparison.OrdinalIgnoreCase))
            return amount;
        if (string.Equals(rule.DeductionType, "Percentage", StringComparison.OrdinalIgnoreCase))
        {
            var basis = string.Equals(rule.ComputeBasedOn, "GrossPay", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.ComputeBasedOn, "TaxablePay", StringComparison.OrdinalIgnoreCase)
                ? grossPay : basePay;
            return basis * (amount / 100m);
        }
        if (string.Equals(rule.DeductionType, "Formula", StringComparison.OrdinalIgnoreCase))
        {
            var taxable = grossPay;
            var threshold = 10416.67m;
            return taxable <= threshold ? 0m : (taxable - threshold) * 0.2m;
        }

        return 0m;
    }

    private static decimal ComputeHourlyRate(Employee emp, string basis, decimal hoursPerDay)
    {
        var baseAmt = Math.Max(0m, emp.BasePayAmount);
        if (basis == "Hourly") return baseAmt;
        if (basis == "Daily") return baseAmt / Math.Max(1m, hoursPerDay);
        if (basis == "Custom")
            return emp.CustomWorkHours.HasValue && emp.CustomWorkHours.Value > 0 ? baseAmt / emp.CustomWorkHours.Value : baseAmt / Math.Max(1m, hoursPerDay);
        return baseAmt / Math.Max(1m, (22m * hoursPerDay));
    }

    private static string NormalizeBasis(string? overrideBasis, string? orgBasis)
    {
        if (!string.IsNullOrWhiteSpace(overrideBasis) && !string.Equals(overrideBasis, "UseOrgDefault", StringComparison.OrdinalIgnoreCase))
            return overrideBasis;
        return string.IsNullOrWhiteSpace(orgBasis) ? "Monthly" : orgBasis;
    }

    private int? GetOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}

public class PayrollPreviewRequest
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Mode { get; set; } = "batch";
    public int? EmpID { get; set; }
}

public class PayrollLineItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Earning";
    public decimal Amount { get; set; }
}

public class PayrollComputedItem
{
    public int EmpID { get; set; }
    public string EmpNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal WorkedHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal BasePay { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Allowances { get; set; }
    public decimal GrossPay { get; set; }
    public decimal Deductions { get; set; }
    public decimal Tax { get; set; }
    public decimal NetPay { get; set; }
    public string FormulaSummary { get; set; } = string.Empty;
    public List<PayrollLineItem> Breakdown { get; set; } = new();
}

public class PayrollPreviewResponse
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Mode { get; set; } = "batch";
    public decimal TotalGross { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNet { get; set; }
    public List<PayrollComputedItem> Items { get; set; } = new();
}
