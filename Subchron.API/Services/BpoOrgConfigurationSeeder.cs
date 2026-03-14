using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Models.LeaveSettings;
using Subchron.API.Models.LeaveTypes;

namespace Subchron.API.Services;

public static class BpoOrgConfigurationSeeder
{
    private const int TargetOrgId = 111;

    public static async Task SeedAsync(TenantDbContext db, ILogger logger, CancellationToken ct = default)
    {
        await SeedPayConfigAsync(db, ct);
        await SeedLeaveConfigAsync(db, ct);
        await SeedLeaveTypesAsync(db, ct);
        await SeedShiftTemplatesAsync(db, ct);
        await SeedAttendanceAndOvertimeAsync(db, ct);
        await SeedHolidayConfigAsync(db, ct);
        await SeedPayComponentsAsync(db, ct);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("BPO seed applied for org {OrgId}.", TargetOrgId);
    }

    private static async Task SeedPayConfigAsync(TenantDbContext db, CancellationToken ct)
    {
        var config = await db.OrgPayConfigs.FirstOrDefaultAsync(x => x.OrgID == TargetOrgId, ct);
        if (config == null)
        {
            config = new OrgPayConfig { OrgID = TargetOrgId };
            db.OrgPayConfigs.Add(config);
        }

        config.Currency = "PHP";
        config.PayCycle = "SemiMonthly";
        config.CompensationBasis = "Monthly";
        config.CustomUnitLabel = string.Empty;
        config.CustomWorkHours = null;
        config.HoursPerDay = 8m;
        config.CutoffWindowsJson = "[{\"name\":\"1st Cutoff\",\"fromDay\":1,\"toDay\":15,\"releaseLagDays\":2},{\"name\":\"2nd Cutoff\",\"fromDay\":16,\"toDay\":31,\"releaseLagDays\":2}]";
        config.LockAttendanceAfterCutoff = true;
        config.ThirteenthMonthBasis = "Basic";
        config.ThirteenthMonthNotes = "Computed pro-rata from basic pay per Philippine Labor Code.";
        config.EnableBIR = true;
        config.BIRPeriod = "SemiMonthly";
        config.BIRTableVersion = DateTime.UtcNow.Year;
        config.EnableSSS = true;
        config.SSSEmployerPercent = 9.5m;
        config.EnablePhilHealth = true;
        config.PhilHealthRate = 4m;
        config.EnablePagIbig = true;
        config.PagIbigRate = 2m;
        config.EnableIncomeTax = true;
        config.ProrateNewHires = true;
        config.ApplyTaxThreshold = false;
        config.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task SeedLeaveConfigAsync(TenantDbContext db, CancellationToken ct)
    {
        var cfg = await db.OrgLeaveConfigs.FirstOrDefaultAsync(x => x.OrgID == TargetOrgId, ct);
        if (cfg == null)
        {
            cfg = new OrgLeaveConfig { OrgID = TargetOrgId };
            db.OrgLeaveConfigs.Add(cfg);
        }

        cfg.FiscalYearStart = LeaveFiscalYearStart.January1;
        cfg.BalanceResetRule = LeaveBalanceResetRule.FiscalYearStart;
        cfg.ProratedForNewHires = true;
        cfg.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task SeedLeaveTypesAsync(TenantDbContext db, CancellationToken ct)
    {
        await UpsertLeaveTypeAsync(db, ct, new LeaveType
        {
            OrgID = TargetOrgId,
            LeaveTypeName = "Service Incentive Leave",
            DefaultDaysPerYear = 5,
            IsPaid = true,
            IsActive = true,
            AccrualType = LeaveAccrualType.LumpSum,
            CarryOverType = LeaveCarryOverType.None,
            AppliesTo = LeaveAppliesTo.Regular,
            LeaveCategory = LeaveCategory.CompanyPolicy,
            CompensationSource = LeaveCompensationSource.CompanyPaid,
            PaidStatus = LeavePaidStatus.Paid,
            StatutoryCode = LeaveStatutoryCode.None,
            MinServiceMonths = 12,
            AdvanceFilingDays = 1,
            AllowRetroactiveFiling = false,
            MaxConsecutiveDays = 5,
            FilingUnit = LeaveFilingUnit.FullDay,
            DeductBalanceOn = LeaveDeductionTiming.UponApproval,
            ApproverRole = LeaveApproverRole.Supervisor,
            LeaveExpiryRule = LeaveExpiryRule.Never,
            AllowLeaveOnRestDay = false,
            AllowLeaveOnHoliday = false,
            TemplateKey = "sil",
            ColorHex = "#16a34a"
        });

        await UpsertLeaveTypeAsync(db, ct, new LeaveType
        {
            OrgID = TargetOrgId,
            LeaveTypeName = "Vacation Leave",
            DefaultDaysPerYear = 10,
            IsPaid = true,
            IsActive = true,
            AccrualType = LeaveAccrualType.Monthly,
            CarryOverType = LeaveCarryOverType.MaxDays,
            CarryOverMaxDays = 5,
            AppliesTo = LeaveAppliesTo.Regular,
            LeaveCategory = LeaveCategory.CompanyPolicy,
            CompensationSource = LeaveCompensationSource.CompanyPaid,
            PaidStatus = LeavePaidStatus.Paid,
            StatutoryCode = LeaveStatutoryCode.None,
            MinServiceMonths = 6,
            AdvanceFilingDays = 3,
            AllowRetroactiveFiling = false,
            MaxConsecutiveDays = 10,
            FilingUnit = LeaveFilingUnit.FullDay,
            DeductBalanceOn = LeaveDeductionTiming.UponApproval,
            ApproverRole = LeaveApproverRole.Manager,
            LeaveExpiryRule = LeaveExpiryRule.FiscalYearEnd,
            TemplateKey = "vl",
            ColorHex = "#2563eb"
        });

        await UpsertLeaveTypeAsync(db, ct, new LeaveType
        {
            OrgID = TargetOrgId,
            LeaveTypeName = "Sick Leave",
            DefaultDaysPerYear = 7,
            IsPaid = true,
            IsActive = true,
            AccrualType = LeaveAccrualType.Monthly,
            CarryOverType = LeaveCarryOverType.None,
            AppliesTo = LeaveAppliesTo.All,
            RequireDocument = true,
            LeaveCategory = LeaveCategory.CompanyPolicy,
            CompensationSource = LeaveCompensationSource.CompanyPaid,
            PaidStatus = LeavePaidStatus.Paid,
            StatutoryCode = LeaveStatutoryCode.None,
            MinServiceMonths = 0,
            AdvanceFilingDays = 0,
            AllowRetroactiveFiling = true,
            MaxConsecutiveDays = 7,
            FilingUnit = LeaveFilingUnit.HalfDay,
            DeductBalanceOn = LeaveDeductionTiming.UponApproval,
            ApproverRole = LeaveApproverRole.Supervisor,
            LeaveExpiryRule = LeaveExpiryRule.Never,
            TemplateKey = "sl",
            ColorHex = "#dc2626"
        });
    }

    private static async Task UpsertLeaveTypeAsync(TenantDbContext db, CancellationToken ct, LeaveType source)
    {
        var existing = await db.LeaveTypes.FirstOrDefaultAsync(x => x.OrgID == source.OrgID && x.TemplateKey == source.TemplateKey, ct)
            ?? await db.LeaveTypes.FirstOrDefaultAsync(x => x.OrgID == source.OrgID && x.LeaveTypeName == source.LeaveTypeName, ct);
        if (existing == null)
        {
            db.LeaveTypes.Add(source);
            return;
        }

        existing.LeaveTypeName = source.LeaveTypeName;
        existing.DefaultDaysPerYear = source.DefaultDaysPerYear;
        existing.IsPaid = source.IsPaid;
        existing.IsActive = source.IsActive;
        existing.AccrualType = source.AccrualType;
        existing.CarryOverType = source.CarryOverType;
        existing.CarryOverMaxDays = source.CarryOverMaxDays;
        existing.AppliesTo = source.AppliesTo;
        existing.RequireApproval = source.RequireApproval;
        existing.RequireDocument = source.RequireDocument;
        existing.AllowNegativeBalance = source.AllowNegativeBalance;
        existing.LeaveCategory = source.LeaveCategory;
        existing.CompensationSource = source.CompensationSource;
        existing.PaidStatus = source.PaidStatus;
        existing.StatutoryCode = source.StatutoryCode;
        existing.MinServiceMonths = source.MinServiceMonths;
        existing.AdvanceFilingDays = source.AdvanceFilingDays;
        existing.AllowRetroactiveFiling = source.AllowRetroactiveFiling;
        existing.MaxConsecutiveDays = source.MaxConsecutiveDays;
        existing.FilingUnit = source.FilingUnit;
        existing.DeductBalanceOn = source.DeductBalanceOn;
        existing.ApproverRole = source.ApproverRole;
        existing.LeaveExpiryRule = source.LeaveExpiryRule;
        existing.LeaveExpiryCustomMonths = source.LeaveExpiryCustomMonths;
        existing.AllowLeaveOnRestDay = source.AllowLeaveOnRestDay;
        existing.AllowLeaveOnHoliday = source.AllowLeaveOnHoliday;
        existing.TemplateKey = source.TemplateKey;
        existing.ColorHex = source.ColorHex;
    }

    private static async Task SeedShiftTemplatesAsync(TenantDbContext db, CancellationToken ct)
    {
        var dayShift = await db.OrgShiftTemplates
            .Include(x => x.WorkDays)
            .Include(x => x.Breaks)
            .FirstOrDefaultAsync(x => x.OrgID == TargetOrgId && x.Code == "BPO-DAY", ct);
        if (dayShift == null)
        {
            dayShift = new OrgShiftTemplate
            {
                OrgID = TargetOrgId,
                Code = "BPO-DAY",
                Name = "BPO Day Shift",
                Type = "Fixed",
                IsActive = true,
                FixedStartTime = "09:00",
                FixedEndTime = "18:00",
                FixedBreakMinutes = 60,
                FixedGraceMinutes = 10
            };
            db.OrgShiftTemplates.Add(dayShift);
        }
        else
        {
            dayShift.Name = "BPO Day Shift";
            dayShift.Type = "Fixed";
            dayShift.IsActive = true;
            dayShift.FixedStartTime = "09:00";
            dayShift.FixedEndTime = "18:00";
            dayShift.FixedBreakMinutes = 60;
            dayShift.FixedGraceMinutes = 10;
        }

        if (!dayShift.WorkDays.Any())
        {
            var days = new[] { "MON", "TUE", "WED", "THU", "FRI" };
            for (var i = 0; i < days.Length; i++)
                dayShift.WorkDays.Add(new OrgShiftTemplateWorkDay { DayCode = days[i], SortOrder = i + 1 });
        }
        if (!dayShift.Breaks.Any())
            dayShift.Breaks.Add(new OrgShiftTemplateBreak { Name = "Meal Break", StartTime = "13:00", EndTime = "14:00", IsPaid = false, SortOrder = 1 });

        var nightShift = await db.OrgShiftTemplates
            .Include(x => x.WorkDays)
            .Include(x => x.Breaks)
            .FirstOrDefaultAsync(x => x.OrgID == TargetOrgId && x.Code == "BPO-NIGHT", ct);
        if (nightShift == null)
        {
            nightShift = new OrgShiftTemplate
            {
                OrgID = TargetOrgId,
                Code = "BPO-NIGHT",
                Name = "BPO Night Shift",
                Type = "Fixed",
                IsActive = true,
                FixedStartTime = "22:00",
                FixedEndTime = "07:00",
                FixedBreakMinutes = 60,
                FixedGraceMinutes = 10
            };
            db.OrgShiftTemplates.Add(nightShift);
        }
        else
        {
            nightShift.Name = "BPO Night Shift";
            nightShift.Type = "Fixed";
            nightShift.IsActive = true;
            nightShift.FixedStartTime = "22:00";
            nightShift.FixedEndTime = "07:00";
            nightShift.FixedBreakMinutes = 60;
            nightShift.FixedGraceMinutes = 10;
        }

        if (!nightShift.WorkDays.Any())
        {
            var days = new[] { "MON", "TUE", "WED", "THU", "FRI" };
            for (var i = 0; i < days.Length; i++)
                nightShift.WorkDays.Add(new OrgShiftTemplateWorkDay { DayCode = days[i], SortOrder = i + 1 });
        }
        if (!nightShift.Breaks.Any())
            nightShift.Breaks.Add(new OrgShiftTemplateBreak { Name = "Meal Break", StartTime = "02:00", EndTime = "03:00", IsPaid = false, SortOrder = 1 });
    }

    private static async Task SeedAttendanceAndOvertimeAsync(TenantDbContext db, CancellationToken ct)
    {
        var attendance = await db.OrgAttendanceConfigs.FirstOrDefaultAsync(x => x.OrgID == TargetOrgId, ct);
        if (attendance == null)
        {
            attendance = new OrgAttendanceConfig { OrgID = TargetOrgId };
            db.OrgAttendanceConfigs.Add(attendance);
        }

        attendance.PrimaryMode = "QR";
        attendance.AllowManualEntry = true;
        attendance.ManualEntryAccessMode = "SUPERVISOR";
        attendance.RequireGeo = true;
        attendance.EnforceGeofence = true;
        attendance.RestrictByIp = false;
        attendance.PreventDoubleClockIn = true;
        attendance.EarliestClockInMinutes = 30;
        attendance.LatestClockInMinutes = 15;
        attendance.AllowIncompleteLogs = false;
        attendance.AutoFlagMissingPunch = true;
        attendance.DefaultMissingPunchAction = "FLAG";
        attendance.UseGracePeriodForLate = true;
        attendance.MarkUndertimeBasedOnSchedule = true;
        attendance.AutoAbsentWithoutLog = true;
        attendance.AutoClockOutEnabled = true;
        attendance.AutoClockOutMaxHours = 13m;
        attendance.DefaultShiftTemplateCode = "BPO-DAY";
        attendance.UpdatedAt = DateTime.UtcNow;

        var policy = await db.OrgAttendanceOvertimePolicies
            .Include(x => x.Buckets)
            .Include(x => x.ApprovalSteps)
            .FirstOrDefaultAsync(x => x.OrgID == TargetOrgId, ct);
        if (policy == null)
        {
            policy = new OrgAttendanceOvertimePolicy { OrgID = TargetOrgId };
            db.OrgAttendanceOvertimePolicies.Add(policy);
        }

        policy.Enabled = true;
        policy.Basis = "SHIFT_END";
        policy.RestHolidayOverride = true;
        policy.DailyThresholdHours = 8m;
        policy.WeeklyThresholdHours = 40m;
        policy.EarlyOtAllowed = false;
        policy.MicroOtBufferMinutes = 10;
        policy.RequireHoursMet = true;
        policy.FilingMode = "AUTO";
        policy.PreApprovalRequired = false;
        policy.AllowPostFiling = true;
        policy.ApprovalFlowType = "SINGLE";
        policy.AutoApprove = false;
        policy.RoundingMinutes = 15;
        policy.RoundingDirection = "NEAREST";
        policy.MinimumBlockMinutes = 30;
        policy.MaxPerDayHours = 4m;
        policy.MaxPerWeekHours = 12m;
        policy.LimitMode = "SOFT";
        policy.OverrideRole = "Manager";
        policy.ScopeMode = "ALL";
        policy.NightDiffEnabled = true;
        policy.NightDiffWindowStart = "22:00";
        policy.NightDiffWindowEnd = "06:00";
        policy.NightDiffMinimumMinutes = 30;
        policy.NightDiffExcludeBreaks = true;
        policy.UpdatedAt = DateTime.UtcNow;

        UpsertBucket(policy, "RegularOT", true, 0m, 4m, 30);
        UpsertBucket(policy, "RestDayOT", true, 0m, 6m, 30);
        UpsertBucket(policy, "HolidayOT", true, 0m, 8m, 30);

        if (!policy.ApprovalSteps.Any())
            policy.ApprovalSteps.Add(new OrgAttendanceOvertimeApprovalStep { Order = 1, Role = "Manager", Required = true });
    }

    private static void UpsertBucket(OrgAttendanceOvertimePolicy policy, string key, bool enabled, decimal? threshold, decimal? maxHours, int? minimumBlock)
    {
        var bucket = policy.Buckets.FirstOrDefault(x => x.Key == key);
        if (bucket == null)
        {
            bucket = new OrgAttendanceOvertimeBucket { Key = key };
            policy.Buckets.Add(bucket);
        }

        bucket.Enabled = enabled;
        bucket.ThresholdHours = threshold;
        bucket.MaxHours = maxHours;
        bucket.MinimumBlockMinutes = minimumBlock;
    }

    private static async Task SeedHolidayConfigAsync(TenantDbContext db, CancellationToken ct)
    {
        var seeds = new[]
        {
            new { Date = new DateTime(2026, 1, 1), Name = "New Year's Day", Type = "RegularHoliday" },
            new { Date = new DateTime(2026, 4, 9), Name = "Araw ng Kagitingan", Type = "RegularHoliday" },
            new { Date = new DateTime(2026, 6, 12), Name = "Independence Day", Type = "RegularHoliday" },
            new { Date = new DateTime(2026, 8, 31), Name = "National Heroes Day", Type = "RegularHoliday" },
            new { Date = new DateTime(2026, 11, 30), Name = "Bonifacio Day", Type = "RegularHoliday" },
            new { Date = new DateTime(2026, 12, 25), Name = "Christmas Day", Type = "RegularHoliday" },
            new { Date = new DateTime(2026, 12, 30), Name = "Rizal Day", Type = "RegularHoliday" }
        };

        foreach (var s in seeds)
        {
            var row = await db.OrgHolidayConfigs.FirstOrDefaultAsync(x => x.OrgID == TargetOrgId && x.HolidayDate == s.Date && x.Name == s.Name, ct);
            if (row == null)
            {
                row = new OrgHolidayConfig
                {
                    OrgID = TargetOrgId,
                    HolidayDate = s.Date,
                    Name = s.Name
                };
                db.OrgHolidayConfigs.Add(row);
            }

            row.Type = s.Type;
            row.Status = "Active";
            row.ScopeType = "Nationwide";
            row.SourceTag = "BpoSeed";
            row.OverlapStrategy = "HighestPrecedence";
            row.Precedence = 100;
            row.IncludeAttendance = true;
            row.NonWorkingDay = true;
            row.AllowWork = true;
            row.ApplyRestDayRules = true;
            row.AttendanceClassification = "Holiday";
            row.RestDayAttendanceClassification = "HolidayRestDay";
            row.IncludePayroll = true;
            row.UsePayRules = true;
            row.PaidWhenUnworked = true;
            row.PayrollClassification = "RegularHoliday";
            row.RestDayPayrollClassification = "RestDayRegularHoliday";
            row.PayrollRuleId = null;
            row.RestDayPayrollRuleId = null;
            row.ScopeValuesJson = "[]";
            row.EmployeeGroupScopeJson = "[]";
            row.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static async Task SeedPayComponentsAsync(TenantDbContext db, CancellationToken ct)
    {
        await UpsertEarningAsync(db, ct, "Regular OT 125%", "OT", "RegularDay", "Multiplier", 1.25m, true, false);
        await UpsertEarningAsync(db, ct, "Rest Day OT 130%", "RestDay", "RestDay", "Multiplier", 1.30m, true, false);
        await UpsertEarningAsync(db, ct, "Night Differential 10%", "NightDiff", "Any", "Percentage", 10m, true, false);
        await UpsertEarningAsync(db, ct, "Regular Holiday 200%", "Holiday", "RegularHoliday", "Multiplier", 2.00m, true, true);
        await UpsertEarningAsync(db, ct, "Special Non-Working Holiday 130%", "Holiday", "SpecialHoliday", "Multiplier", 1.30m, true, true);

        await UpsertAllowanceAsync(db, ct, "Rice Allowance", "FixedPerPayroll", "DeMinimis", 1500m, false, false, true);
        await UpsertAllowanceAsync(db, ct, "Night Shift Premium Allowance", "FixedPerPayroll", "Operational", 1200m, true, true, true);

        await UpsertDeductionAsync(db, ct, "SSS", "Statutory", "Percentage", 4.5m, true, true, true, "BasicPay");
        await UpsertDeductionAsync(db, ct, "PhilHealth", "Statutory", "Percentage", 2m, true, true, true, "BasicPay");
        await UpsertDeductionAsync(db, ct, "Pag-IBIG", "Statutory", "Percentage", 2m, true, true, true, "BasicPay");
        await UpsertDeductionAsync(db, ct, "Withholding Tax", "Tax", "Formula", null, false, true, true, "TaxablePay", "progressive_tax(taxable)");
    }

    private static async Task UpsertEarningAsync(TenantDbContext db, CancellationToken ct, string name, string appliesTo, string dayType, string rateType, decimal rateValue, bool taxable, bool includeBenefitBase)
    {
        var row = await db.EarningRules.FirstOrDefaultAsync(x => x.OrgID == TargetOrgId && x.Name == name, ct);
        if (row == null)
        {
            row = new EarningRule
            {
                OrgID = TargetOrgId,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };
            db.EarningRules.Add(row);
        }

        row.AppliesTo = appliesTo;
        row.DayType = dayType;
        row.HolidayCombo = "Standard";
        row.RestDayHandling = "FollowAttendance";
        row.Scope = "AllEmployees";
        row.ScopeTagsJson = "[]";
        row.RateType = rateType;
        row.RateValue = rateValue;
        row.IsTaxable = taxable;
        row.IncludeInBenefitBase = includeBenefitBase;
        row.RequiresApproval = false;
        row.IsActive = true;
        row.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task UpsertAllowanceAsync(TenantDbContext db, CancellationToken ct, string name, string allowanceType, string category, decimal amount, bool attendanceDependent, bool prorate, bool isTaxable)
    {
        var row = await db.OrgAllowanceRules.FirstOrDefaultAsync(x => x.OrgID == TargetOrgId && x.Name == name, ct);
        if (row == null)
        {
            row = new OrgAllowanceRule
            {
                OrgID = TargetOrgId,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };
            db.OrgAllowanceRules.Add(row);
        }

        row.AllowanceType = allowanceType;
        row.Category = category;
        row.Amount = amount;
        row.AttendanceDependent = attendanceDependent;
        row.ProrateIfPartialPeriod = prorate;
        row.IsTaxable = isTaxable;
        row.IsActive = true;
        row.ScopeTagsJson = "[]";
        row.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task UpsertDeductionAsync(TenantDbContext db, CancellationToken ct, string name, string category, string deductionType, decimal? amount, bool hasEmployerShare, bool hasEmployeeShare, bool autoCompute, string computeBasedOn, string? formula = null)
    {
        var row = await db.DeductionRules.FirstOrDefaultAsync(x => x.OrgID == TargetOrgId && x.Name == name, ct);
        if (row == null)
        {
            row = new DeductionRule
            {
                OrgID = TargetOrgId,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };
            db.DeductionRules.Add(row);
        }

        row.Category = category;
        row.DeductionType = deductionType;
        row.Amount = amount;
        row.FormulaExpression = formula;
        row.HasEmployerShare = hasEmployerShare;
        row.HasEmployeeShare = hasEmployeeShare;
        row.EmployerSharePercent = hasEmployerShare ? 50m : null;
        row.EmployeeSharePercent = hasEmployeeShare ? 50m : null;
        row.AutoCompute = autoCompute;
        row.ComputeBasedOn = computeBasedOn;
        row.IsActive = true;
        row.ScopeTagsJson = "[]";
        row.UpdatedAt = DateTime.UtcNow;
    }
}
