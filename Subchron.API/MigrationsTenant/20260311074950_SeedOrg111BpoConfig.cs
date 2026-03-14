using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subchron.API.MigrationsTenant
{
    /// <inheritdoc />
    public partial class SeedOrg111BpoConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @OrgID INT = 111;

-- OrgPayConfigs
IF EXISTS (SELECT 1 FROM OrgPayConfigs WHERE OrgID = @OrgID)
BEGIN
    UPDATE OrgPayConfigs
    SET Currency = 'PHP',
        PayCycle = 'SemiMonthly',
        CompensationBasis = 'Monthly',
        CustomUnitLabel = '',
        CustomWorkHours = NULL,
        HoursPerDay = 8,
        CutoffWindowsJson = '[{""name"":""1st Cutoff"",""fromDay"":1,""toDay"":15,""releaseLagDays"":2},{""name"":""2nd Cutoff"",""fromDay"":16,""toDay"":31,""releaseLagDays"":2}]',
        LockAttendanceAfterCutoff = 1,
        ThirteenthMonthBasis = 'Basic',
        ThirteenthMonthNotes = 'Computed pro-rata from basic pay per Philippine Labor Code.',
        EnableBIR = 1,
        BIRPeriod = 'SemiMonthly',
        BIRTableVersion = YEAR(SYSUTCDATETIME()),
        EnableSSS = 1,
        SSSEmployerPercent = 9.5,
        EnablePhilHealth = 1,
        PhilHealthRate = 4,
        EnablePagIbig = 1,
        PagIbigRate = 2,
        EnableIncomeTax = 1,
        ProrateNewHires = 1,
        ApplyTaxThreshold = 0,
        UpdatedAt = SYSUTCDATETIME()
    WHERE OrgID = @OrgID;
END
ELSE
BEGIN
    INSERT INTO OrgPayConfigs
        (OrgID, Currency, PayCycle, CompensationBasis, CustomUnitLabel, CustomWorkHours, HoursPerDay, CutoffWindowsJson,
         LockAttendanceAfterCutoff, ThirteenthMonthBasis, ThirteenthMonthNotes, EnableBIR, BIRPeriod, BIRTableVersion,
         EnableSSS, SSSEmployerPercent, EnablePhilHealth, PhilHealthRate, EnablePagIbig, PagIbigRate,
         EnableIncomeTax, ProrateNewHires, ApplyTaxThreshold, CreatedAt, UpdatedAt)
    VALUES
        (@OrgID, 'PHP', 'SemiMonthly', 'Monthly', '', NULL, 8,
         '[{""name"":""1st Cutoff"",""fromDay"":1,""toDay"":15,""releaseLagDays"":2},{""name"":""2nd Cutoff"",""fromDay"":16,""toDay"":31,""releaseLagDays"":2}]',
         1, 'Basic', 'Computed pro-rata from basic pay per Philippine Labor Code.', 1, 'SemiMonthly', YEAR(SYSUTCDATETIME()),
         1, 9.5, 1, 4, 1, 2,
         1, 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

-- OrgLeaveConfigs
IF EXISTS (SELECT 1 FROM OrgLeaveConfigs WHERE OrgID = @OrgID)
BEGIN
    UPDATE OrgLeaveConfigs
    SET FiscalYearStart = 1,
        BalanceResetRule = 1,
        ProratedForNewHires = 1,
        UpdatedAt = SYSUTCDATETIME()
    WHERE OrgID = @OrgID;
END
ELSE
BEGIN
    INSERT INTO OrgLeaveConfigs (OrgID, FiscalYearStart, BalanceResetRule, ProratedForNewHires, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

-- LeaveTypes
IF EXISTS (SELECT 1 FROM LeaveTypes WHERE OrgID = @OrgID AND TemplateKey = 'sil')
BEGIN
    UPDATE LeaveTypes
    SET LeaveTypeName = 'Service Incentive Leave', DefaultDaysPerYear = 5, IsPaid = 1, IsActive = 1,
        AccrualType = 1, CarryOverType = 1, CarryOverMaxDays = NULL, AppliesTo = 5,
        RequireApproval = 1, RequireDocument = 0, AllowNegativeBalance = 0,
        LeaveCategory = 2, CompensationSource = 1, PaidStatus = 1, StatutoryCode = 0,
        MinServiceMonths = 12, AdvanceFilingDays = 1, AllowRetroactiveFiling = 0, MaxConsecutiveDays = 5,
        FilingUnit = 1, DeductBalanceOn = 1, ApproverRole = 1, LeaveExpiryRule = 1,
        LeaveExpiryCustomMonths = NULL, AllowLeaveOnRestDay = 0, AllowLeaveOnHoliday = 0,
        RequiresLegalQualification = 0, RequiresHrValidation = 0, CanOrgOverride = 1, IsSystemProtected = 0,
        ColorHex = '#16a34a'
    WHERE OrgID = @OrgID AND TemplateKey = 'sil';
END
ELSE
BEGIN
    INSERT INTO LeaveTypes
        (OrgID, LeaveTypeName, DefaultDaysPerYear, IsPaid, IsActive, AccrualType, CarryOverType, CarryOverMaxDays,
         AppliesTo, RequireApproval, RequireDocument, AllowNegativeBalance, LeaveCategory, CompensationSource,
         PaidStatus, StatutoryCode, MinServiceMonths, AdvanceFilingDays, AllowRetroactiveFiling, MaxConsecutiveDays,
         FilingUnit, DeductBalanceOn, ApproverRole, LeaveExpiryRule, LeaveExpiryCustomMonths,
         AllowLeaveOnRestDay, AllowLeaveOnHoliday, RequiresLegalQualification, RequiresHrValidation,
         CanOrgOverride, IsSystemProtected, TemplateKey, ColorHex)
    VALUES
        (@OrgID, 'Service Incentive Leave', 5, 1, 1, 1, 1, NULL,
         5, 1, 0, 0, 2, 1,
         1, 0, 12, 1, 0, 5,
         1, 1, 1, 1, NULL,
         0, 0, 0, 0,
         1, 0, 'sil', '#16a34a');
END;

IF EXISTS (SELECT 1 FROM LeaveTypes WHERE OrgID = @OrgID AND TemplateKey = 'vl')
BEGIN
    UPDATE LeaveTypes
    SET LeaveTypeName = 'Vacation Leave', DefaultDaysPerYear = 10, IsPaid = 1, IsActive = 1,
        AccrualType = 2, CarryOverType = 2, CarryOverMaxDays = 5, AppliesTo = 5,
        RequireApproval = 1, RequireDocument = 0, AllowNegativeBalance = 0,
        LeaveCategory = 2, CompensationSource = 1, PaidStatus = 1, StatutoryCode = 0,
        MinServiceMonths = 6, AdvanceFilingDays = 3, AllowRetroactiveFiling = 0, MaxConsecutiveDays = 10,
        FilingUnit = 1, DeductBalanceOn = 1, ApproverRole = 2, LeaveExpiryRule = 2,
        LeaveExpiryCustomMonths = NULL, AllowLeaveOnRestDay = 0, AllowLeaveOnHoliday = 0,
        RequiresLegalQualification = 0, RequiresHrValidation = 0, CanOrgOverride = 1, IsSystemProtected = 0,
        ColorHex = '#2563eb'
    WHERE OrgID = @OrgID AND TemplateKey = 'vl';
END
ELSE
BEGIN
    INSERT INTO LeaveTypes
        (OrgID, LeaveTypeName, DefaultDaysPerYear, IsPaid, IsActive, AccrualType, CarryOverType, CarryOverMaxDays,
         AppliesTo, RequireApproval, RequireDocument, AllowNegativeBalance, LeaveCategory, CompensationSource,
         PaidStatus, StatutoryCode, MinServiceMonths, AdvanceFilingDays, AllowRetroactiveFiling, MaxConsecutiveDays,
         FilingUnit, DeductBalanceOn, ApproverRole, LeaveExpiryRule, LeaveExpiryCustomMonths,
         AllowLeaveOnRestDay, AllowLeaveOnHoliday, RequiresLegalQualification, RequiresHrValidation,
         CanOrgOverride, IsSystemProtected, TemplateKey, ColorHex)
    VALUES
        (@OrgID, 'Vacation Leave', 10, 1, 1, 2, 2, 5,
         5, 1, 0, 0, 2, 1,
         1, 0, 6, 3, 0, 10,
         1, 1, 2, 2, NULL,
         0, 0, 0, 0,
         1, 0, 'vl', '#2563eb');
END;

IF EXISTS (SELECT 1 FROM LeaveTypes WHERE OrgID = @OrgID AND TemplateKey = 'sl')
BEGIN
    UPDATE LeaveTypes
    SET LeaveTypeName = 'Sick Leave', DefaultDaysPerYear = 7, IsPaid = 1, IsActive = 1,
        AccrualType = 2, CarryOverType = 1, CarryOverMaxDays = NULL, AppliesTo = 1,
        RequireApproval = 1, RequireDocument = 1, AllowNegativeBalance = 0,
        LeaveCategory = 2, CompensationSource = 1, PaidStatus = 1, StatutoryCode = 0,
        MinServiceMonths = 0, AdvanceFilingDays = 0, AllowRetroactiveFiling = 1, MaxConsecutiveDays = 7,
        FilingUnit = 2, DeductBalanceOn = 1, ApproverRole = 1, LeaveExpiryRule = 1,
        LeaveExpiryCustomMonths = NULL, AllowLeaveOnRestDay = 0, AllowLeaveOnHoliday = 0,
        RequiresLegalQualification = 0, RequiresHrValidation = 0, CanOrgOverride = 1, IsSystemProtected = 0,
        ColorHex = '#dc2626'
    WHERE OrgID = @OrgID AND TemplateKey = 'sl';
END
ELSE
BEGIN
    INSERT INTO LeaveTypes
        (OrgID, LeaveTypeName, DefaultDaysPerYear, IsPaid, IsActive, AccrualType, CarryOverType, CarryOverMaxDays,
         AppliesTo, RequireApproval, RequireDocument, AllowNegativeBalance, LeaveCategory, CompensationSource,
         PaidStatus, StatutoryCode, MinServiceMonths, AdvanceFilingDays, AllowRetroactiveFiling, MaxConsecutiveDays,
         FilingUnit, DeductBalanceOn, ApproverRole, LeaveExpiryRule, LeaveExpiryCustomMonths,
         AllowLeaveOnRestDay, AllowLeaveOnHoliday, RequiresLegalQualification, RequiresHrValidation,
         CanOrgOverride, IsSystemProtected, TemplateKey, ColorHex)
    VALUES
        (@OrgID, 'Sick Leave', 7, 1, 1, 2, 1, NULL,
         1, 1, 1, 0, 2, 1,
         1, 0, 0, 0, 1, 7,
         2, 1, 1, 1, NULL,
         0, 0, 0, 0,
         1, 0, 'sl', '#dc2626');
END;

-- Shift templates
IF NOT EXISTS (SELECT 1 FROM OrgShiftTemplates WHERE OrgID = @OrgID AND Code = 'BPO-DAY')
BEGIN
    INSERT INTO OrgShiftTemplates (OrgID, Code, Name, Type, IsActive, DisabledReason, FixedStartTime, FixedEndTime, FixedBreakMinutes, FixedGraceMinutes, FlexibleEarliestStart, FlexibleLatestEnd, FlexibleRequiredDailyHours, FlexibleMaxDailyHours, OpenRequiredWeeklyHours, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'BPO-DAY', 'BPO Day Shift', 'Fixed', 1, NULL, '09:00', '18:00', 60, 10, NULL, NULL, NULL, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END
ELSE
BEGIN
    UPDATE OrgShiftTemplates
    SET Name = 'BPO Day Shift', Type = 'Fixed', IsActive = 1, DisabledReason = NULL,
        FixedStartTime = '09:00', FixedEndTime = '18:00', FixedBreakMinutes = 60, FixedGraceMinutes = 10,
        UpdatedAt = SYSUTCDATETIME()
    WHERE OrgID = @OrgID AND Code = 'BPO-DAY';
END;

IF NOT EXISTS (SELECT 1 FROM OrgShiftTemplates WHERE OrgID = @OrgID AND Code = 'BPO-NIGHT')
BEGIN
    INSERT INTO OrgShiftTemplates (OrgID, Code, Name, Type, IsActive, DisabledReason, FixedStartTime, FixedEndTime, FixedBreakMinutes, FixedGraceMinutes, FlexibleEarliestStart, FlexibleLatestEnd, FlexibleRequiredDailyHours, FlexibleMaxDailyHours, OpenRequiredWeeklyHours, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'BPO-NIGHT', 'BPO Night Shift', 'Fixed', 1, NULL, '22:00', '07:00', 60, 10, NULL, NULL, NULL, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END
ELSE
BEGIN
    UPDATE OrgShiftTemplates
    SET Name = 'BPO Night Shift', Type = 'Fixed', IsActive = 1, DisabledReason = NULL,
        FixedStartTime = '22:00', FixedEndTime = '07:00', FixedBreakMinutes = 60, FixedGraceMinutes = 10,
        UpdatedAt = SYSUTCDATETIME()
    WHERE OrgID = @OrgID AND Code = 'BPO-NIGHT';
END;

DECLARE @DayShiftId INT = (SELECT TOP 1 OrgShiftTemplateID FROM OrgShiftTemplates WHERE OrgID = @OrgID AND Code = 'BPO-DAY');
DECLARE @NightShiftId INT = (SELECT TOP 1 OrgShiftTemplateID FROM OrgShiftTemplates WHERE OrgID = @OrgID AND Code = 'BPO-NIGHT');

IF @DayShiftId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM OrgShiftTemplateWorkDays WHERE OrgShiftTemplateID = @DayShiftId)
    BEGIN
        INSERT INTO OrgShiftTemplateWorkDays (OrgShiftTemplateID, DayCode, SortOrder)
        VALUES (@DayShiftId, 'MON', 1), (@DayShiftId, 'TUE', 2), (@DayShiftId, 'WED', 3), (@DayShiftId, 'THU', 4), (@DayShiftId, 'FRI', 5);
    END;

    IF NOT EXISTS (SELECT 1 FROM OrgShiftTemplateBreaks WHERE OrgShiftTemplateID = @DayShiftId)
    BEGIN
        INSERT INTO OrgShiftTemplateBreaks (OrgShiftTemplateID, Name, StartTime, EndTime, IsPaid, SortOrder)
        VALUES (@DayShiftId, 'Meal Break', '13:00', '14:00', 0, 1);
    END;
END;

IF @NightShiftId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM OrgShiftTemplateWorkDays WHERE OrgShiftTemplateID = @NightShiftId)
    BEGIN
        INSERT INTO OrgShiftTemplateWorkDays (OrgShiftTemplateID, DayCode, SortOrder)
        VALUES (@NightShiftId, 'MON', 1), (@NightShiftId, 'TUE', 2), (@NightShiftId, 'WED', 3), (@NightShiftId, 'THU', 4), (@NightShiftId, 'FRI', 5);
    END;

    IF NOT EXISTS (SELECT 1 FROM OrgShiftTemplateBreaks WHERE OrgShiftTemplateID = @NightShiftId)
    BEGIN
        INSERT INTO OrgShiftTemplateBreaks (OrgShiftTemplateID, Name, StartTime, EndTime, IsPaid, SortOrder)
        VALUES (@NightShiftId, 'Meal Break', '02:00', '03:00', 0, 1);
    END;
END;

-- Attendance config
IF EXISTS (SELECT 1 FROM OrgAttendanceConfigs WHERE OrgID = @OrgID)
BEGIN
    UPDATE OrgAttendanceConfigs
    SET PrimaryMode = 'QR', AllowManualEntry = 1, ManualEntryAccessMode = 'SUPERVISOR',
        RequireGeo = 1, EnforceGeofence = 1, RestrictByIp = 0, PreventDoubleClockIn = 1,
        EarliestClockInMinutes = 30, LatestClockInMinutes = 15, AllowIncompleteLogs = 0,
        AutoFlagMissingPunch = 1, DefaultMissingPunchAction = 'FLAG', UseGracePeriodForLate = 1,
        MarkUndertimeBasedOnSchedule = 1, AutoAbsentWithoutLog = 1, AutoClockOutEnabled = 1,
        AutoClockOutMaxHours = 13, DefaultShiftTemplateCode = 'BPO-DAY', UpdatedAt = SYSUTCDATETIME()
    WHERE OrgID = @OrgID;
END
ELSE
BEGIN
    INSERT INTO OrgAttendanceConfigs
        (OrgID, PrimaryMode, AllowManualEntry, ManualEntryAccessMode, RequireGeo, EnforceGeofence, RestrictByIp,
         PreventDoubleClockIn, EarliestClockInMinutes, LatestClockInMinutes, AllowIncompleteLogs, AutoFlagMissingPunch,
         DefaultMissingPunchAction, UseGracePeriodForLate, MarkUndertimeBasedOnSchedule, AutoAbsentWithoutLog,
         AutoClockOutEnabled, AutoClockOutMaxHours, DefaultShiftTemplateCode, CreatedAt, UpdatedAt)
    VALUES
        (@OrgID, 'QR', 1, 'SUPERVISOR', 1, 1, 0,
         1, 30, 15, 0, 1,
         'FLAG', 1, 1, 1,
         1, 13, 'BPO-DAY', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

-- Overtime policy
DECLARE @OtPolicyId INT = (SELECT TOP 1 OrgAttendanceOvertimePolicyID FROM OrgAttendanceOvertimePolicies WHERE OrgID = @OrgID);
IF @OtPolicyId IS NULL
BEGIN
    INSERT INTO OrgAttendanceOvertimePolicies
        (OrgID, Enabled, Basis, RestHolidayOverride, DailyThresholdHours, WeeklyThresholdHours, EarlyOtAllowed,
         MicroOtBufferMinutes, RequireHoursMet, FilingMode, PreApprovalRequired, AllowPostFiling,
         ApprovalFlowType, AutoApprove, RoundingMinutes, RoundingDirection, MinimumBlockMinutes,
         MaxPerDayHours, MaxPerWeekHours, LimitMode, OverrideRole, ScopeMode,
         NightDiffEnabled, NightDiffWindowStart, NightDiffWindowEnd, NightDiffMinimumMinutes, NightDiffExcludeBreaks,
         CreatedAt, UpdatedAt)
    VALUES
        (@OrgID, 1, 'SHIFT_END', 1, 8, 40, 0,
         10, 1, 'AUTO', 0, 1,
         'SINGLE', 0, 15, 'NEAREST', 30,
         4, 12, 'SOFT', 'Manager', 'ALL',
         1, '22:00', '06:00', 30, 1,
         SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @OtPolicyId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE OrgAttendanceOvertimePolicies
    SET Enabled = 1, Basis = 'SHIFT_END', RestHolidayOverride = 1, DailyThresholdHours = 8, WeeklyThresholdHours = 40,
        EarlyOtAllowed = 0, MicroOtBufferMinutes = 10, RequireHoursMet = 1, FilingMode = 'AUTO',
        PreApprovalRequired = 0, AllowPostFiling = 1, ApprovalFlowType = 'SINGLE', AutoApprove = 0,
        RoundingMinutes = 15, RoundingDirection = 'NEAREST', MinimumBlockMinutes = 30,
        MaxPerDayHours = 4, MaxPerWeekHours = 12, LimitMode = 'SOFT', OverrideRole = 'Manager', ScopeMode = 'ALL',
        NightDiffEnabled = 1, NightDiffWindowStart = '22:00', NightDiffWindowEnd = '06:00',
        NightDiffMinimumMinutes = 30, NightDiffExcludeBreaks = 1, UpdatedAt = SYSUTCDATETIME()
    WHERE OrgAttendanceOvertimePolicyID = @OtPolicyId;
END;

IF @OtPolicyId IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM OrgAttendanceOvertimeBuckets WHERE OrgAttendanceOvertimePolicyID = @OtPolicyId AND [Key] = 'RegularOT')
        UPDATE OrgAttendanceOvertimeBuckets SET Enabled = 1, ThresholdHours = 0, MaxHours = 4, MinimumBlockMinutes = 30 WHERE OrgAttendanceOvertimePolicyID = @OtPolicyId AND [Key] = 'RegularOT';
    ELSE
        INSERT INTO OrgAttendanceOvertimeBuckets (OrgAttendanceOvertimePolicyID, [Key], Enabled, ThresholdHours, MaxHours, MinimumBlockMinutes)
        VALUES (@OtPolicyId, 'RegularOT', 1, 0, 4, 30);

    IF EXISTS (SELECT 1 FROM OrgAttendanceOvertimeBuckets WHERE OrgAttendanceOvertimePolicyID = @OtPolicyId AND [Key] = 'RestDayOT')
        UPDATE OrgAttendanceOvertimeBuckets SET Enabled = 1, ThresholdHours = 0, MaxHours = 6, MinimumBlockMinutes = 30 WHERE OrgAttendanceOvertimePolicyID = @OtPolicyId AND [Key] = 'RestDayOT';
    ELSE
        INSERT INTO OrgAttendanceOvertimeBuckets (OrgAttendanceOvertimePolicyID, [Key], Enabled, ThresholdHours, MaxHours, MinimumBlockMinutes)
        VALUES (@OtPolicyId, 'RestDayOT', 1, 0, 6, 30);

    IF EXISTS (SELECT 1 FROM OrgAttendanceOvertimeBuckets WHERE OrgAttendanceOvertimePolicyID = @OtPolicyId AND [Key] = 'HolidayOT')
        UPDATE OrgAttendanceOvertimeBuckets SET Enabled = 1, ThresholdHours = 0, MaxHours = 8, MinimumBlockMinutes = 30 WHERE OrgAttendanceOvertimePolicyID = @OtPolicyId AND [Key] = 'HolidayOT';
    ELSE
        INSERT INTO OrgAttendanceOvertimeBuckets (OrgAttendanceOvertimePolicyID, [Key], Enabled, ThresholdHours, MaxHours, MinimumBlockMinutes)
        VALUES (@OtPolicyId, 'HolidayOT', 1, 0, 8, 30);

    IF NOT EXISTS (SELECT 1 FROM OrgAttendanceOvertimeApprovalSteps WHERE OrgAttendanceOvertimePolicyID = @OtPolicyId AND [Order] = 1)
        INSERT INTO OrgAttendanceOvertimeApprovalSteps (OrgAttendanceOvertimePolicyID, [Order], Role, Required)
        VALUES (@OtPolicyId, 1, 'Manager', 1);
END;

-- Holidays (PH regular)
IF NOT EXISTS (SELECT 1 FROM OrgHolidayConfigs WHERE OrgID = @OrgID AND HolidayDate = '2026-01-01' AND Name = 'New''s Day')
    INSERT INTO OrgHolidayConfigs (OrgID, HolidayDate, Name, Type, Status, ScopeType, SourceTag, OverlapStrategy, Precedence, IncludeAttendance, NonWorkingDay, AllowWork, ApplyRestDayRules, AttendanceClassification, RestDayAttendanceClassification, IncludePayroll, UsePayRules, PaidWhenUnworked, PayrollClassification, RestDayPayrollClassification, PayrollRuleId, RestDayPayrollRuleId, ReferenceNo, ReferenceUrl, OfficialTag, ScopeValuesJson, EmployeeGroupScopeJson, PayrollNotes, Notes, IsSynced, CreatedAt, UpdatedAt)
    VALUES (@OrgID, '2026-01-01', 'New''s Day', 'RegularHoliday', 'Active', 'Nationwide', 'BpoSeed', 'HighestPrecedence', 100, 1, 1, 1, 1, 'Holiday', 'HolidayRestDay', 1, 1, 1, 'RegularHoliday', 'RestDayRegularHoliday', NULL, NULL, '', '', '', '[]', '[]', '', '', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM OrgHolidayConfigs WHERE OrgID = @OrgID AND HolidayDate = '2026-04-09' AND Name = 'Araw ng Kagitingan')
    INSERT INTO OrgHolidayConfigs (OrgID, HolidayDate, Name, Type, Status, ScopeType, SourceTag, OverlapStrategy, Precedence, IncludeAttendance, NonWorkingDay, AllowWork, ApplyRestDayRules, AttendanceClassification, RestDayAttendanceClassification, IncludePayroll, UsePayRules, PaidWhenUnworked, PayrollClassification, RestDayPayrollClassification, PayrollRuleId, RestDayPayrollRuleId, ReferenceNo, ReferenceUrl, OfficialTag, ScopeValuesJson, EmployeeGroupScopeJson, PayrollNotes, Notes, IsSynced, CreatedAt, UpdatedAt)
    VALUES (@OrgID, '2026-04-09', 'Araw ng Kagitingan', 'RegularHoliday', 'Active', 'Nationwide', 'BpoSeed', 'HighestPrecedence', 100, 1, 1, 1, 1, 'Holiday', 'HolidayRestDay', 1, 1, 1, 'RegularHoliday', 'RestDayRegularHoliday', NULL, NULL, '', '', '', '[]', '[]', '', '', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM OrgHolidayConfigs WHERE OrgID = @OrgID AND HolidayDate = '2026-06-12' AND Name = 'Independence Day')
    INSERT INTO OrgHolidayConfigs (OrgID, HolidayDate, Name, Type, Status, ScopeType, SourceTag, OverlapStrategy, Precedence, IncludeAttendance, NonWorkingDay, AllowWork, ApplyRestDayRules, AttendanceClassification, RestDayAttendanceClassification, IncludePayroll, UsePayRules, PaidWhenUnworked, PayrollClassification, RestDayPayrollClassification, PayrollRuleId, RestDayPayrollRuleId, ReferenceNo, ReferenceUrl, OfficialTag, ScopeValuesJson, EmployeeGroupScopeJson, PayrollNotes, Notes, IsSynced, CreatedAt, UpdatedAt)
    VALUES (@OrgID, '2026-06-12', 'Independence Day', 'RegularHoliday', 'Active', 'Nationwide', 'BpoSeed', 'HighestPrecedence', 100, 1, 1, 1, 1, 'Holiday', 'HolidayRestDay', 1, 1, 1, 'RegularHoliday', 'RestDayRegularHoliday', NULL, NULL, '', '', '', '[]', '[]', '', '', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM OrgHolidayConfigs WHERE OrgID = @OrgID AND HolidayDate = '2026-08-31' AND Name = 'National Heroes Day')
    INSERT INTO OrgHolidayConfigs (OrgID, HolidayDate, Name, Type, Status, ScopeType, SourceTag, OverlapStrategy, Precedence, IncludeAttendance, NonWorkingDay, AllowWork, ApplyRestDayRules, AttendanceClassification, RestDayAttendanceClassification, IncludePayroll, UsePayRules, PaidWhenUnworked, PayrollClassification, RestDayPayrollClassification, PayrollRuleId, RestDayPayrollRuleId, ReferenceNo, ReferenceUrl, OfficialTag, ScopeValuesJson, EmployeeGroupScopeJson, PayrollNotes, Notes, IsSynced, CreatedAt, UpdatedAt)
    VALUES (@OrgID, '2026-08-31', 'National Heroes Day', 'RegularHoliday', 'Active', 'Nationwide', 'BpoSeed', 'HighestPrecedence', 100, 1, 1, 1, 1, 'Holiday', 'HolidayRestDay', 1, 1, 1, 'RegularHoliday', 'RestDayRegularHoliday', NULL, NULL, '', '', '', '[]', '[]', '', '', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM OrgHolidayConfigs WHERE OrgID = @OrgID AND HolidayDate = '2026-11-30' AND Name = 'Bonifacio Day')
    INSERT INTO OrgHolidayConfigs (OrgID, HolidayDate, Name, Type, Status, ScopeType, SourceTag, OverlapStrategy, Precedence, IncludeAttendance, NonWorkingDay, AllowWork, ApplyRestDayRules, AttendanceClassification, RestDayAttendanceClassification, IncludePayroll, UsePayRules, PaidWhenUnworked, PayrollClassification, RestDayPayrollClassification, PayrollRuleId, RestDayPayrollRuleId, ReferenceNo, ReferenceUrl, OfficialTag, ScopeValuesJson, EmployeeGroupScopeJson, PayrollNotes, Notes, IsSynced, CreatedAt, UpdatedAt)
    VALUES (@OrgID, '2026-11-30', 'Bonifacio Day', 'RegularHoliday', 'Active', 'Nationwide', 'BpoSeed', 'HighestPrecedence', 100, 1, 1, 1, 1, 'Holiday', 'HolidayRestDay', 1, 1, 1, 'RegularHoliday', 'RestDayRegularHoliday', NULL, NULL, '', '', '', '[]', '[]', '', '', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM OrgHolidayConfigs WHERE OrgID = @OrgID AND HolidayDate = '2026-12-25' AND Name = 'Christmas Day')
    INSERT INTO OrgHolidayConfigs (OrgID, HolidayDate, Name, Type, Status, ScopeType, SourceTag, OverlapStrategy, Precedence, IncludeAttendance, NonWorkingDay, AllowWork, ApplyRestDayRules, AttendanceClassification, RestDayAttendanceClassification, IncludePayroll, UsePayRules, PaidWhenUnworked, PayrollClassification, RestDayPayrollClassification, PayrollRuleId, RestDayPayrollRuleId, ReferenceNo, ReferenceUrl, OfficialTag, ScopeValuesJson, EmployeeGroupScopeJson, PayrollNotes, Notes, IsSynced, CreatedAt, UpdatedAt)
    VALUES (@OrgID, '2026-12-25', 'Christmas Day', 'RegularHoliday', 'Active', 'Nationwide', 'BpoSeed', 'HighestPrecedence', 100, 1, 1, 1, 1, 'Holiday', 'HolidayRestDay', 1, 1, 1, 'RegularHoliday', 'RestDayRegularHoliday', NULL, NULL, '', '', '', '[]', '[]', '', '', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM OrgHolidayConfigs WHERE OrgID = @OrgID AND HolidayDate = '2026-12-30' AND Name = 'Rizal Day')
    INSERT INTO OrgHolidayConfigs (OrgID, HolidayDate, Name, Type, Status, ScopeType, SourceTag, OverlapStrategy, Precedence, IncludeAttendance, NonWorkingDay, AllowWork, ApplyRestDayRules, AttendanceClassification, RestDayAttendanceClassification, IncludePayroll, UsePayRules, PaidWhenUnworked, PayrollClassification, RestDayPayrollClassification, PayrollRuleId, RestDayPayrollRuleId, ReferenceNo, ReferenceUrl, OfficialTag, ScopeValuesJson, EmployeeGroupScopeJson, PayrollNotes, Notes, IsSynced, CreatedAt, UpdatedAt)
    VALUES (@OrgID, '2026-12-30', 'Rizal Day', 'RegularHoliday', 'Active', 'Nationwide', 'BpoSeed', 'HighestPrecedence', 100, 1, 1, 1, 1, 'Holiday', 'HolidayRestDay', 1, 1, 1, 'RegularHoliday', 'RestDayRegularHoliday', NULL, NULL, '', '', '', '[]', '[]', '', '', 0, SYSUTCDATETIME(), SYSUTCDATETIME());

-- Pay components
IF EXISTS (SELECT 1 FROM EarningRules WHERE OrgID = @OrgID AND Name = 'Regular OT 125%')
    UPDATE EarningRules SET AppliesTo='OT', DayType='RegularDay', HolidayCombo='Standard', RestDayHandling='FollowAttendance', Scope='AllEmployees', ScopeTagsJson='[]', RateType='Multiplier', RateValue=1.25, IsTaxable=1, IncludeInBenefitBase=0, RequiresApproval=0, Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='Regular OT 125%';
ELSE
    INSERT INTO EarningRules (OrgID, Name, AppliesTo, DayType, HolidayCombo, RestDayHandling, Scope, ScopeTagsJson, EffectiveFrom, EffectiveTo, RateType, RateValue, IsTaxable, IncludeInBenefitBase, RequiresApproval, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Regular OT 125%', 'OT', 'RegularDay', 'Standard', 'FollowAttendance', 'AllEmployees', '[]', NULL, NULL, 'Multiplier', 1.25, 1, 0, 0, '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM EarningRules WHERE OrgID = @OrgID AND Name = 'Rest Day OT 130%')
    UPDATE EarningRules SET AppliesTo='RestDay', DayType='RestDay', HolidayCombo='Standard', RestDayHandling='FollowAttendance', Scope='AllEmployees', ScopeTagsJson='[]', RateType='Multiplier', RateValue=1.30, IsTaxable=1, IncludeInBenefitBase=0, RequiresApproval=0, Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='Rest Day OT 130%';
ELSE
    INSERT INTO EarningRules (OrgID, Name, AppliesTo, DayType, HolidayCombo, RestDayHandling, Scope, ScopeTagsJson, EffectiveFrom, EffectiveTo, RateType, RateValue, IsTaxable, IncludeInBenefitBase, RequiresApproval, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Rest Day OT 130%', 'RestDay', 'RestDay', 'Standard', 'FollowAttendance', 'AllEmployees', '[]', NULL, NULL, 'Multiplier', 1.30, 1, 0, 0, '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM EarningRules WHERE OrgID = @OrgID AND Name = 'Night Differential 10%')
    UPDATE EarningRules SET AppliesTo='NightDiff', DayType='Any', HolidayCombo='Standard', RestDayHandling='FollowAttendance', Scope='AllEmployees', ScopeTagsJson='[]', RateType='Percentage', RateValue=10, IsTaxable=1, IncludeInBenefitBase=0, RequiresApproval=0, Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='Night Differential 10%';
ELSE
    INSERT INTO EarningRules (OrgID, Name, AppliesTo, DayType, HolidayCombo, RestDayHandling, Scope, ScopeTagsJson, EffectiveFrom, EffectiveTo, RateType, RateValue, IsTaxable, IncludeInBenefitBase, RequiresApproval, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Night Differential 10%', 'NightDiff', 'Any', 'Standard', 'FollowAttendance', 'AllEmployees', '[]', NULL, NULL, 'Percentage', 10, 1, 0, 0, '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM EarningRules WHERE OrgID = @OrgID AND Name = 'Regular Holiday 200%')
    UPDATE EarningRules SET AppliesTo='Holiday', DayType='RegularHoliday', HolidayCombo='Standard', RestDayHandling='FollowAttendance', Scope='AllEmployees', ScopeTagsJson='[]', RateType='Multiplier', RateValue=2.00, IsTaxable=1, IncludeInBenefitBase=1, RequiresApproval=0, Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='Regular Holiday 200%';
ELSE
    INSERT INTO EarningRules (OrgID, Name, AppliesTo, DayType, HolidayCombo, RestDayHandling, Scope, ScopeTagsJson, EffectiveFrom, EffectiveTo, RateType, RateValue, IsTaxable, IncludeInBenefitBase, RequiresApproval, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Regular Holiday 200%', 'Holiday', 'RegularHoliday', 'Standard', 'FollowAttendance', 'AllEmployees', '[]', NULL, NULL, 'Multiplier', 2.00, 1, 1, 0, '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM EarningRules WHERE OrgID = @OrgID AND Name = 'Special Non-Working Holiday 130%')
    UPDATE EarningRules SET AppliesTo='Holiday', DayType='SpecialHoliday', HolidayCombo='Standard', RestDayHandling='FollowAttendance', Scope='AllEmployees', ScopeTagsJson='[]', RateType='Multiplier', RateValue=1.30, IsTaxable=1, IncludeInBenefitBase=1, RequiresApproval=0, Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='Special Non-Working Holiday 130%';
ELSE
    INSERT INTO EarningRules (OrgID, Name, AppliesTo, DayType, HolidayCombo, RestDayHandling, Scope, ScopeTagsJson, EffectiveFrom, EffectiveTo, RateType, RateValue, IsTaxable, IncludeInBenefitBase, RequiresApproval, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Special Non-Working Holiday 130%', 'Holiday', 'SpecialHoliday', 'Standard', 'FollowAttendance', 'AllEmployees', '[]', NULL, NULL, 'Multiplier', 1.30, 1, 1, 0, '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM OrgAllowanceRules WHERE OrgID = @OrgID AND Name = 'Rice Allowance')
    UPDATE OrgAllowanceRules SET AllowanceType='FixedPerPayroll', Category='DeMinimis', Amount=1500, IsTaxable=0, AttendanceDependent=0, ProrateIfPartialPeriod=0, IsActive=1, ScopeTagsJson='[]', ComplianceNotes='', UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='Rice Allowance';
ELSE
    INSERT INTO OrgAllowanceRules (OrgID, Name, AllowanceType, Category, Amount, IsTaxable, AttendanceDependent, ProrateIfPartialPeriod, EffectiveFrom, EffectiveTo, IsActive, ScopeTagsJson, ComplianceNotes, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Rice Allowance', 'FixedPerPayroll', 'DeMinimis', 1500, 0, 0, 0, NULL, NULL, 1, '[]', '', SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM OrgAllowanceRules WHERE OrgID = @OrgID AND Name = 'Night Shift Premium Allowance')
    UPDATE OrgAllowanceRules SET AllowanceType='FixedPerPayroll', Category='Operational', Amount=1200, IsTaxable=1, AttendanceDependent=1, ProrateIfPartialPeriod=1, IsActive=1, ScopeTagsJson='[]', ComplianceNotes='', UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='Night Shift Premium Allowance';
ELSE
    INSERT INTO OrgAllowanceRules (OrgID, Name, AllowanceType, Category, Amount, IsTaxable, AttendanceDependent, ProrateIfPartialPeriod, EffectiveFrom, EffectiveTo, IsActive, ScopeTagsJson, ComplianceNotes, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Night Shift Premium Allowance', 'FixedPerPayroll', 'Operational', 1200, 1, 1, 1, NULL, NULL, 1, '[]', '', SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM DeductionRules WHERE OrgID = @OrgID AND Name = 'SSS')
    UPDATE DeductionRules SET Category='Statutory', DeductionType='Percentage', Amount=4.5, FormulaExpression=NULL, HasEmployerShare=1, HasEmployeeShare=1, EmployerSharePercent=50, EmployeeSharePercent=50, AutoCompute=1, ComputeBasedOn='BasicPay', MaxDeductionAmount=NULL, ScopeTagsJson='[]', Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='SSS';
ELSE
    INSERT INTO DeductionRules (OrgID, Name, Category, DeductionType, Amount, FormulaExpression, HasEmployerShare, HasEmployeeShare, EmployerSharePercent, EmployeeSharePercent, AutoCompute, ComputeBasedOn, EffectiveFrom, MaxDeductionAmount, ScopeTagsJson, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'SSS', 'Statutory', 'Percentage', 4.5, NULL, 1, 1, 50, 50, 1, 'BasicPay', NULL, NULL, '[]', '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM DeductionRules WHERE OrgID = @OrgID AND Name = 'PhilHealth')
    UPDATE DeductionRules SET Category='Statutory', DeductionType='Percentage', Amount=2, FormulaExpression=NULL, HasEmployerShare=1, HasEmployeeShare=1, EmployerSharePercent=50, EmployeeSharePercent=50, AutoCompute=1, ComputeBasedOn='BasicPay', MaxDeductionAmount=NULL, ScopeTagsJson='[]', Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='PhilHealth';
ELSE
    INSERT INTO DeductionRules (OrgID, Name, Category, DeductionType, Amount, FormulaExpression, HasEmployerShare, HasEmployeeShare, EmployerSharePercent, EmployeeSharePercent, AutoCompute, ComputeBasedOn, EffectiveFrom, MaxDeductionAmount, ScopeTagsJson, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'PhilHealth', 'Statutory', 'Percentage', 2, NULL, 1, 1, 50, 50, 1, 'BasicPay', NULL, NULL, '[]', '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM DeductionRules WHERE OrgID = @OrgID AND Name = 'Pag-IBIG')
    UPDATE DeductionRules SET Category='Statutory', DeductionType='Percentage', Amount=2, FormulaExpression=NULL, HasEmployerShare=1, HasEmployeeShare=1, EmployerSharePercent=50, EmployeeSharePercent=50, AutoCompute=1, ComputeBasedOn='BasicPay', MaxDeductionAmount=NULL, ScopeTagsJson='[]', Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='Pag-IBIG';
ELSE
    INSERT INTO DeductionRules (OrgID, Name, Category, DeductionType, Amount, FormulaExpression, HasEmployerShare, HasEmployeeShare, EmployerSharePercent, EmployeeSharePercent, AutoCompute, ComputeBasedOn, EffectiveFrom, MaxDeductionAmount, ScopeTagsJson, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Pag-IBIG', 'Statutory', 'Percentage', 2, NULL, 1, 1, 50, 50, 1, 'BasicPay', NULL, NULL, '[]', '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF EXISTS (SELECT 1 FROM DeductionRules WHERE OrgID = @OrgID AND Name = 'Withholding Tax')
    UPDATE DeductionRules SET Category='Tax', DeductionType='Formula', Amount=NULL, FormulaExpression='progressive_tax(taxable)', HasEmployerShare=0, HasEmployeeShare=1, EmployerSharePercent=NULL, EmployeeSharePercent=NULL, AutoCompute=1, ComputeBasedOn='TaxablePay', MaxDeductionAmount=NULL, ScopeTagsJson='[]', Notes='', IsActive=1, UpdatedAt=SYSUTCDATETIME() WHERE OrgID = @OrgID AND Name='Withholding Tax';
ELSE
    INSERT INTO DeductionRules (OrgID, Name, Category, DeductionType, Amount, FormulaExpression, HasEmployerShare, HasEmployeeShare, EmployerSharePercent, EmployeeSharePercent, AutoCompute, ComputeBasedOn, EffectiveFrom, MaxDeductionAmount, ScopeTagsJson, Notes, IsActive, CreatedAt, UpdatedAt)
    VALUES (@OrgID, 'Withholding Tax', 'Tax', 'Formula', NULL, 'progressive_tax(taxable)', 0, 1, NULL, NULL, 1, 'TaxablePay', NULL, NULL, '[]', '', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @OrgID INT = 111;

DELETE FROM OrgAttendanceOvertimeApprovalSteps WHERE OrgAttendanceOvertimePolicyID IN (SELECT OrgAttendanceOvertimePolicyID FROM OrgAttendanceOvertimePolicies WHERE OrgID = @OrgID);
DELETE FROM OrgAttendanceOvertimeBuckets WHERE OrgAttendanceOvertimePolicyID IN (SELECT OrgAttendanceOvertimePolicyID FROM OrgAttendanceOvertimePolicies WHERE OrgID = @OrgID);
DELETE FROM OrgAttendanceOvertimePolicies WHERE OrgID = @OrgID;
DELETE FROM OrgAttendanceConfigs WHERE OrgID = @OrgID;

DELETE FROM OrgShiftTemplateBreaks WHERE OrgShiftTemplateID IN (SELECT OrgShiftTemplateID FROM OrgShiftTemplates WHERE OrgID = @OrgID);
DELETE FROM OrgShiftTemplateWorkDays WHERE OrgShiftTemplateID IN (SELECT OrgShiftTemplateID FROM OrgShiftTemplates WHERE OrgID = @OrgID);
DELETE FROM OrgShiftTemplates WHERE OrgID = @OrgID AND Code IN ('BPO-DAY', 'BPO-NIGHT');

DELETE FROM OrgHolidayConfigs WHERE OrgID = @OrgID AND SourceTag = 'BpoSeed';
DELETE FROM EarningRules WHERE OrgID = @OrgID AND Name IN ('Regular OT 125%', 'Rest Day OT 130%', 'Night Differential 10%', 'Regular Holiday 200%', 'Special Non-Working Holiday 130%');
DELETE FROM OrgAllowanceRules WHERE OrgID = @OrgID AND Name IN ('Rice Allowance', 'Night Shift Premium Allowance');
DELETE FROM DeductionRules WHERE OrgID = @OrgID AND Name IN ('SSS', 'PhilHealth', 'Pag-IBIG', 'Withholding Tax');
DELETE FROM LeaveTypes WHERE OrgID = @OrgID AND TemplateKey IN ('sil', 'vl', 'sl');
DELETE FROM OrgLeaveConfigs WHERE OrgID = @OrgID;
DELETE FROM OrgPayConfigs WHERE OrgID = @OrgID;
");
        }
    }
}
