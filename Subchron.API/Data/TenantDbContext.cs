using Microsoft.EntityFrameworkCore;
using Subchron.API.Models.Entities;

namespace Subchron.API.Data;

/// <summary>Tenant DB: Employees, Departments, LeaveRequests, ShiftAssignments, TenantAuditLogs, Locations, AttendanceLogs, AttendanceCorrections, ExportJobs, LeaveTypes, OvertimeRequests, SignupPendings. Soft refs (OrgID, UserID) to Platform; no cross-DB FKs.</summary>
public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<TenantAuditLog> TenantAuditLogs => Set<TenantAuditLog>();

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
    public DbSet<AttendanceCorrection> AttendanceCorrections => Set<AttendanceCorrection>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<OrgLeaveConfig> OrgLeaveConfigs => Set<OrgLeaveConfig>();
    public DbSet<OrgHolidayConfig> OrgHolidayConfigs => Set<OrgHolidayConfig>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<SignupPending> SignupPendings => Set<SignupPending>();
    public DbSet<OrgAttendanceConfig> OrgAttendanceConfigs => Set<OrgAttendanceConfig>();
    public DbSet<OrgAttendanceOvertimePolicy> OrgAttendanceOvertimePolicies => Set<OrgAttendanceOvertimePolicy>();
    public DbSet<OrgAttendanceOvertimeBucket> OrgAttendanceOvertimeBuckets => Set<OrgAttendanceOvertimeBucket>();
    public DbSet<OrgAttendanceOvertimeApprovalStep> OrgAttendanceOvertimeApprovalSteps => Set<OrgAttendanceOvertimeApprovalStep>();
    public DbSet<OrgAttendanceOvertimeScopeFilter> OrgAttendanceOvertimeScopeFilters => Set<OrgAttendanceOvertimeScopeFilter>();
    public DbSet<OrgAttendanceNightDiffExclusion> OrgAttendanceNightDiffExclusions => Set<OrgAttendanceNightDiffExclusion>();
    public DbSet<OrgShiftTemplate> OrgShiftTemplates => Set<OrgShiftTemplate>();
    public DbSet<OrgShiftTemplateWorkDay> OrgShiftTemplateWorkDays => Set<OrgShiftTemplateWorkDay>();
    public DbSet<OrgShiftTemplateBreak> OrgShiftTemplateBreaks => Set<OrgShiftTemplateBreak>();
    public DbSet<OrgShiftTemplateDayOverride> OrgShiftTemplateDayOverrides => Set<OrgShiftTemplateDayOverride>();
    public DbSet<OrgShiftTemplateOverrideWindow> OrgShiftTemplateOverrideWindows => Set<OrgShiftTemplateOverrideWindow>();
    public DbSet<OrgPayConfig> OrgPayConfigs => Set<OrgPayConfig>();
    public DbSet<EarningRule> EarningRules => Set<EarningRule>();
    public DbSet<DeductionRule> DeductionRules => Set<DeductionRule>();
    public DbSet<OrgAllowanceRule> OrgAllowanceRules => Set<OrgAllowanceRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- Employee: OrgID, UserID scalar (soft refs to Platform); FK to Department (same DB) -----
        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("Employees");
            e.HasKey(x => x.EmpID);
            e.Property(x => x.EmpNumber).HasMaxLength(40).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            e.Property(x => x.MiddleName).HasMaxLength(80);
            e.Property(x => x.BirthDate).HasColumnType("date");
            e.Property(x => x.Gender).HasMaxLength(20);
            e.Property(x => x.Role).HasMaxLength(40).IsRequired();
            e.Property(x => x.WorkState).HasMaxLength(40).IsRequired();
            e.Property(x => x.EmploymentType).HasMaxLength(40).IsRequired();
            e.Property(x => x.AssignedShiftTemplateCode).HasMaxLength(60);
            e.Property(x => x.AddressLine1).HasMaxLength(120);
            e.Property(x => x.AddressLine2).HasMaxLength(120);
            e.Property(x => x.City).HasMaxLength(80);
            e.Property(x => x.StateProvince).HasMaxLength(80);
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.Country).HasMaxLength(80);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.PhoneNormalized).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.EmergencyContactName).HasMaxLength(120);
            e.Property(x => x.EmergencyContactPhone).HasMaxLength(30);
            e.Property(x => x.EmergencyContactRelation).HasMaxLength(60);
            e.Property(x => x.ArchivedReason).HasMaxLength(200);
            e.Property(x => x.RestoreReason).HasMaxLength(200);
            e.Property(x => x.IsArchived).HasDefaultValue(false);
            e.Property(x => x.AttendanceQrToken).HasMaxLength(64);
            e.Property(x => x.AvatarUrl).HasMaxLength(500);
            e.Property(x => x.IdPictureUrl).HasMaxLength(500);
            e.Property(x => x.SignatureUrl).HasMaxLength(500);
            e.HasIndex(x => x.AttendanceQrToken).IsUnique().HasFilter("[AttendanceQrToken] IS NOT NULL");
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.UserID);
            e.HasIndex(x => new { x.OrgID, x.EmpNumber }).IsUnique();
            e.HasIndex(x => new { x.OrgID, x.PhoneNormalized }).IsUnique().HasFilter("[PhoneNormalized] IS NOT NULL AND [PhoneNormalized] != ''");
            e.HasIndex(x => new { x.OrgID, x.Email }).IsUnique().HasFilter("[Email] IS NOT NULL AND [Email] != ''");
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentID).OnDelete(DeleteBehavior.SetNull);
        });

        // ----- Department: OrgID scalar (soft ref) -----
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("Departments");
            e.HasKey(x => x.DepID);
            e.Property(x => x.DepartmentName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.DefaultShiftTemplateCode).HasMaxLength(60);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => new { x.OrgID, x.DepartmentName }).IsUnique();
        });

        // ----- LeaveRequest: FK to Employee; OrgID, ReviewedByUserID, CreatedByUserID scalar -----
        modelBuilder.Entity<LeaveRequest>(e =>
        {
            e.ToTable("LeaveRequests");
            e.HasKey(x => x.LeaveRequestID);
            e.Property(x => x.LeaveType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.ReviewNotes).HasMaxLength(300);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmpID).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.EmpID);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.StartDate);
        });

        // ----- ShiftAssignment: FK to Employee; OrgID scalar -----
        modelBuilder.Entity<ShiftAssignment>(e =>
        {
            e.ToTable("ShiftAssignments");
            e.HasKey(x => x.ShiftAssignmentID);
            e.Property(x => x.Notes).HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmpID).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.OrgID, x.AssignmentDate });
            e.HasIndex(x => x.EmpID);
        });

        // ----- TenantAuditLog: OrgID, UserID scalar (soft refs) -----
        modelBuilder.Entity<TenantAuditLog>(e =>
        {
            e.ToTable("TenantAuditLogs");
            e.HasKey(x => x.TenantAuditLogID);
            e.Property(x => x.Action).HasMaxLength(80).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(60);
            e.Property(x => x.Details).HasMaxLength(500);
            e.Property(x => x.Meta).HasMaxLength(1000);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.UserID);
            e.HasIndex(x => x.CreatedAt);
        });

        // ----- Location: OrgID scalar (soft ref) -----
        modelBuilder.Entity<Location>(e =>
        {
            e.ToTable("Locations");
            e.HasKey(x => x.LocationID);
            e.Property(x => x.LocationName).HasMaxLength(50).IsRequired();
            e.Property(x => x.GeoLat).HasColumnType("decimal(9,6)");
            e.Property(x => x.GeoLong).HasColumnType("decimal(9,6)");
            e.Property(x => x.RadiusMeters);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.DeactivationReason).HasMaxLength(200);
            e.Property(x => x.PinColor).HasMaxLength(20).HasDefaultValue("blue");
            e.HasIndex(x => x.OrgID);
        });

        // ----- AttendanceLog: OrgID scalar; EmpID FK to Employee -----
        modelBuilder.Entity<AttendanceLog>(e =>
        {
            e.ToTable("AttendanceLogs");
            e.HasKey(x => x.AttendanceID);
            e.Property(x => x.MethodIn).HasMaxLength(20);
            e.Property(x => x.MethodOut).HasMaxLength(20);
            e.Property(x => x.GeoLat).HasColumnType("decimal(9,6)");
            e.Property(x => x.GeoLong).HasColumnType("decimal(9,6)");
            e.Property(x => x.GeoStatus).HasMaxLength(20);
            e.Property(x => x.DeviceInfo).HasMaxLength(120);
            e.Property(x => x.Remarks).HasMaxLength(120);
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.EmpID);
            e.HasIndex(x => x.LogDate);
        });

        // ----- AttendanceCorrection: OrgID, RequestedByUserID, ReviewedByUserID scalar; AttendanceID FK to AttendanceLog -----
        modelBuilder.Entity<AttendanceCorrection>(e =>
        {
            e.ToTable("AttendanceCorrections");
            e.HasKey(x => x.CorrectionID);
            e.Property(x => x.Reasons).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.AttendanceID);
            e.HasIndex(x => x.RequestedByUserID);
        });

        // ----- LeaveType: OrgID scalar -----
        modelBuilder.Entity<LeaveType>(e =>
        {
            e.ToTable("LeaveTypes");
            e.HasKey(x => x.LeaveTypeID);
            e.Property(x => x.LeaveTypeName).HasMaxLength(50).IsRequired();
            e.Property(x => x.DefaultDaysPerYear).HasColumnType("decimal(5,2)");
            e.Property(x => x.IsPaid).HasDefaultValue(true);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.AccrualType).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.CarryOverType).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.CarryOverMaxDays);
            e.Property(x => x.AppliesTo).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.RequireApproval).HasDefaultValue(true);
            e.Property(x => x.RequireDocument).HasDefaultValue(false);
            e.Property(x => x.AllowNegativeBalance).HasDefaultValue(false);
            e.Property(x => x.LeaveCategory).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.CompensationSource).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.PaidStatus).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.StatutoryCode).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.MinServiceMonths).HasDefaultValue(0);
            e.Property(x => x.AdvanceFilingDays).HasDefaultValue(0);
            e.Property(x => x.AllowRetroactiveFiling).HasDefaultValue(false);
            e.Property(x => x.MaxConsecutiveDays).HasDefaultValue(0);
            e.Property(x => x.FilingUnit).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.DeductBalanceOn).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.ApproverRole).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.LeaveExpiryRule).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.LeaveExpiryCustomMonths);
            e.Property(x => x.AllowLeaveOnRestDay).HasDefaultValue(false);
            e.Property(x => x.AllowLeaveOnHoliday).HasDefaultValue(false);
            e.Property(x => x.RequiresLegalQualification).HasDefaultValue(false);
            e.Property(x => x.RequiresHrValidation).HasDefaultValue(false);
            e.Property(x => x.CanOrgOverride).HasDefaultValue(true);
            e.Property(x => x.IsSystemProtected).HasDefaultValue(false);
            e.Property(x => x.TemplateKey).HasMaxLength(60);
            e.Property(x => x.ColorHex).HasMaxLength(12);
            e.HasIndex(x => x.OrgID);
        });

        modelBuilder.Entity<OrgLeaveConfig>(e =>
        {
            e.ToTable("OrgLeaveConfigs");
            e.HasKey(x => x.OrgID);
            e.Property(x => x.OrgID).ValueGeneratedNever();
            e.Property(x => x.FiscalYearStart).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.BalanceResetRule).HasConversion<int>().HasColumnType("int").IsRequired();
            e.Property(x => x.ProratedForNewHires).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgID).IsUnique();
        });

        modelBuilder.Entity<OrgHolidayConfig>(e =>
        {
            e.ToTable("OrgHolidayConfigs");
            e.HasKey(x => x.OrgHolidayConfigID);
            e.HasIndex(x => new { x.OrgID, x.HolidayDate });

            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Type).HasMaxLength(40).IsRequired();
            e.Property(x => x.Status).HasMaxLength(40).HasDefaultValue("Active");
            e.Property(x => x.ScopeType).HasMaxLength(40).HasDefaultValue("Nationwide");
            e.Property(x => x.SourceTag).HasMaxLength(40).HasDefaultValue("ManualEntry");
            e.Property(x => x.OverlapStrategy).HasMaxLength(40).HasDefaultValue("HighestPrecedence");
            e.Property(x => x.AttendanceClassification).HasMaxLength(60).HasDefaultValue("Holiday");
            e.Property(x => x.RestDayAttendanceClassification).HasMaxLength(60);
            e.Property(x => x.PayrollClassification).HasMaxLength(60).HasDefaultValue("RegularHoliday");
            e.Property(x => x.RestDayPayrollClassification).HasMaxLength(60);
            e.Property(x => x.PayrollRuleId).HasMaxLength(80);
            e.Property(x => x.RestDayPayrollRuleId).HasMaxLength(80);
            e.Property(x => x.ReferenceNo).HasMaxLength(100);
            e.Property(x => x.ReferenceUrl).HasMaxLength(200);
            e.Property(x => x.OfficialTag).HasMaxLength(80);
            e.Property(x => x.ScopeValuesJson).HasColumnType("NVARCHAR(MAX)").HasDefaultValue("[]");
            e.Property(x => x.EmployeeGroupScopeJson).HasColumnType("NVARCHAR(MAX)").HasDefaultValue("[]");
            e.Property(x => x.PayrollNotes).HasColumnType("NVARCHAR(MAX)").HasDefaultValue(string.Empty);
            e.Property(x => x.Notes).HasColumnType("NVARCHAR(MAX)").HasDefaultValue(string.Empty);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ----- ExportJob: OrgID, ExportedByUserID scalar -----
        modelBuilder.Entity<ExportJob>(e =>
        {
            e.ToTable("ExportJobs");
            e.HasKey(x => x.ExportID);
            e.Property(x => x.ExportType).HasMaxLength(10).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(120);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.ExportedByUserID);
        });

        // ----- OvertimeRequest: OrgID, ApprovedByUserID scalar; EmpID FK to Employee -----
        modelBuilder.Entity<OvertimeRequest>(e =>
        {
            e.ToTable("OvertimeRequests");
            e.HasKey(x => x.OTRequestID);
            e.Property(x => x.Reason).HasMaxLength(100);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.TotalHours).HasColumnType("decimal(5,2)");
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.EmpID);
            e.HasIndex(x => x.OTDate);
        });

        // ----- SignupPending: no OrgID (pre-org signup data) -----
        modelBuilder.Entity<SignupPending>(e =>
        {
            e.ToTable("SignupPendings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).HasMaxLength(256).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
        });

        // ----- OrgAttendanceConfig -----
        modelBuilder.Entity<OrgAttendanceConfig>(e =>
        {
            e.ToTable("OrgAttendanceConfigs");
            e.HasKey(x => x.OrgID);
            e.Property(x => x.OrgID).ValueGeneratedNever();
            e.Property(x => x.PrimaryMode).HasMaxLength(20).IsRequired();
            e.Property(x => x.ManualEntryAccessMode).HasMaxLength(30).HasDefaultValue("SUPERVISOR");
            e.Property(x => x.DefaultShiftTemplateCode).HasMaxLength(60);
            e.Property(x => x.DefaultMissingPunchAction).HasMaxLength(30).HasDefaultValue("IGNORE");
            e.Property(x => x.AutoClockOutMaxHours).HasColumnType("decimal(5,2)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgID).IsUnique();
        });

        // ----- OrgAttendanceOvertimePolicy -----
        modelBuilder.Entity<OrgAttendanceOvertimePolicy>(e =>
        {
            e.ToTable("OrgAttendanceOvertimePolicies");
            e.HasKey(x => x.OrgAttendanceOvertimePolicyID);
            e.Property(x => x.Basis).HasMaxLength(20).IsRequired();
            e.Property(x => x.FilingMode).HasMaxLength(10).IsRequired();
            e.Property(x => x.ApprovalFlowType).HasMaxLength(20).IsRequired();
            e.Property(x => x.RoundingDirection).HasMaxLength(12).IsRequired();
            e.Property(x => x.LimitMode).HasMaxLength(10).IsRequired();
            e.Property(x => x.ScopeMode).HasMaxLength(20).IsRequired();
            e.Property(x => x.NightDiffWindowStart).HasMaxLength(5).IsRequired();
            e.Property(x => x.NightDiffWindowEnd).HasMaxLength(5).IsRequired();
            e.Property(x => x.OverrideRole).HasMaxLength(60);
            e.Property(x => x.DailyThresholdHours).HasColumnType("decimal(6,2)");
            e.Property(x => x.WeeklyThresholdHours).HasColumnType("decimal(6,2)");
            e.Property(x => x.MaxPerDayHours).HasColumnType("decimal(6,2)");
            e.Property(x => x.MaxPerWeekHours).HasColumnType("decimal(6,2)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgID).IsUnique();
        });

        modelBuilder.Entity<OrgAttendanceOvertimeBucket>(e =>
        {
            e.ToTable("OrgAttendanceOvertimeBuckets");
            e.HasKey(x => x.OrgAttendanceOvertimeBucketID);
            e.Property(x => x.Key).HasMaxLength(60).IsRequired();
            e.Property(x => x.ThresholdHours).HasColumnType("decimal(6,2)");
            e.Property(x => x.MaxHours).HasColumnType("decimal(6,2)");
            e.HasOne<OrgAttendanceOvertimePolicy>()
                .WithMany(p => p.Buckets)
                .HasForeignKey(x => x.OrgAttendanceOvertimePolicyID)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.OrgAttendanceOvertimePolicyID, x.Key }).IsUnique();
        });

        modelBuilder.Entity<OrgAttendanceOvertimeApprovalStep>(e =>
        {
            e.ToTable("OrgAttendanceOvertimeApprovalSteps");
            e.HasKey(x => x.OrgAttendanceOvertimeApprovalStepID);
            e.Property(x => x.Role).HasMaxLength(60).IsRequired();
            e.HasOne<OrgAttendanceOvertimePolicy>()
                .WithMany(p => p.ApprovalSteps)
                .HasForeignKey(x => x.OrgAttendanceOvertimePolicyID)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.OrgAttendanceOvertimePolicyID, x.Order });
        });

        modelBuilder.Entity<OrgAttendanceOvertimeScopeFilter>(e =>
        {
            e.ToTable("OrgAttendanceOvertimeScopeFilters");
            e.HasKey(x => x.OrgAttendanceOvertimeScopeFilterID);
            e.Property(x => x.FilterType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Value).HasMaxLength(120).IsRequired();
            e.HasOne<OrgAttendanceOvertimePolicy>()
                .WithMany(p => p.ScopeFilters)
                .HasForeignKey(x => x.OrgAttendanceOvertimePolicyID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrgAttendanceNightDiffExclusion>(e =>
        {
            e.ToTable("OrgAttendanceNightDiffExclusions");
            e.HasKey(x => x.OrgAttendanceNightDiffExclusionID);
            e.Property(x => x.Department).HasMaxLength(120);
            e.Property(x => x.Site).HasMaxLength(120);
            e.Property(x => x.Role).HasMaxLength(120);
            e.HasOne<OrgAttendanceOvertimePolicy>()
                .WithMany(p => p.NightDiffExclusions)
                .HasForeignKey(x => x.OrgAttendanceOvertimePolicyID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ----- OrgShiftTemplate -----
        modelBuilder.Entity<OrgShiftTemplate>(e =>
        {
            e.ToTable("OrgShiftTemplates");
            e.HasKey(x => x.OrgShiftTemplateID);
            e.Property(x => x.Code).HasMaxLength(60).IsRequired();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Type).HasMaxLength(20).IsRequired();
            e.Property(x => x.DisabledReason).HasMaxLength(60);
            e.Property(x => x.FixedStartTime).HasMaxLength(5);
            e.Property(x => x.FixedEndTime).HasMaxLength(5);
            e.Property(x => x.FlexibleEarliestStart).HasMaxLength(5);
            e.Property(x => x.FlexibleLatestEnd).HasMaxLength(5);
            e.Property(x => x.OpenRequiredWeeklyHours).HasColumnType("decimal(6,2)");
            e.Property(x => x.FlexibleRequiredDailyHours).HasColumnType("decimal(5,2)");
            e.Property(x => x.FlexibleMaxDailyHours).HasColumnType("decimal(5,2)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => new { x.OrgID, x.Code }).IsUnique();
        });

        modelBuilder.Entity<OrgShiftTemplateWorkDay>(e =>
        {
            e.ToTable("OrgShiftTemplateWorkDays");
            e.HasKey(x => x.OrgShiftTemplateWorkDayID);
            e.Property(x => x.DayCode).HasMaxLength(3).IsRequired();
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasOne<OrgShiftTemplate>()
                .WithMany(t => t.WorkDays)
                .HasForeignKey(x => x.OrgShiftTemplateID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrgShiftTemplateBreak>(e =>
        {
            e.ToTable("OrgShiftTemplateBreaks");
            e.HasKey(x => x.OrgShiftTemplateBreakID);
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.StartTime).HasMaxLength(5).IsRequired();
            e.Property(x => x.EndTime).HasMaxLength(5).IsRequired();
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasOne<OrgShiftTemplate>()
                .WithMany(t => t.Breaks)
                .HasForeignKey(x => x.OrgShiftTemplateID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrgShiftTemplateDayOverride>(e =>
        {
            e.ToTable("OrgShiftTemplateDayOverrides");
            e.HasKey(x => x.OrgShiftTemplateDayOverrideID);
            e.Property(x => x.Day).HasMaxLength(3).IsRequired();
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasOne<OrgShiftTemplate>()
                .WithMany(t => t.DayOverrides)
                .HasForeignKey(x => x.OrgShiftTemplateID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrgShiftTemplateOverrideWindow>(e =>
        {
            e.ToTable("OrgShiftTemplateOverrideWindows");
            e.HasKey(x => x.OrgShiftTemplateOverrideWindowID);
            e.Property(x => x.StartTime).HasMaxLength(5).IsRequired();
            e.Property(x => x.EndTime).HasMaxLength(5).IsRequired();
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasOne<OrgShiftTemplateDayOverride>()
                .WithMany(o => o.WorkWindows)
                .HasForeignKey(x => x.OrgShiftTemplateDayOverrideID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrgPayConfig>(e =>
        {
            e.ToTable("OrgPayConfigs");
            e.HasKey(x => x.OrgID);
            e.Property(x => x.OrgID).ValueGeneratedNever();
            e.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            e.Property(x => x.PayCycle).HasMaxLength(30).IsRequired();
            e.Property(x => x.CompensationBasis).HasMaxLength(20).IsRequired();
            e.Property(x => x.CustomUnitLabel).HasMaxLength(40);
            e.Property(x => x.CustomWorkHours).HasColumnType("decimal(7,2)");
            e.Property(x => x.HoursPerDay).HasColumnType("decimal(5,2)");
            e.Property(x => x.CutoffWindowsJson).HasColumnType("NVARCHAR(MAX)").HasDefaultValue("[]");
            e.Property(x => x.ThirteenthMonthBasis).HasMaxLength(40).IsRequired();
            e.Property(x => x.ThirteenthMonthNotes).HasMaxLength(250);
            e.Property(x => x.BIRPeriod).HasMaxLength(30);
            e.Property(x => x.SSSEmployerPercent).HasColumnType("decimal(5,2)");
            e.Property(x => x.PhilHealthRate).HasColumnType("decimal(5,2)");
            e.Property(x => x.PagIbigRate).HasColumnType("decimal(5,2)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<EarningRule>(e =>
        {
            e.ToTable("EarningRules");
            e.HasKey(x => x.EarningRuleID);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.AppliesTo).HasMaxLength(30).IsRequired();
            e.Property(x => x.DayType).HasMaxLength(30).HasDefaultValue("Any");
            e.Property(x => x.HolidayCombo).HasMaxLength(40).HasDefaultValue("Standard");
            e.Property(x => x.RestDayHandling).HasMaxLength(40).HasDefaultValue("FollowAttendance");
            e.Property(x => x.Scope).HasMaxLength(40).HasDefaultValue("AllEmployees");
            e.Property(x => x.ScopeTagsJson).HasColumnType("NVARCHAR(MAX)").HasDefaultValue("[]");
            e.Property(x => x.RateType).HasMaxLength(20).IsRequired();
            e.Property(x => x.RateValue).HasColumnType("decimal(10,4)");
            e.Property(x => x.IsTaxable).HasDefaultValue(true);
            e.Property(x => x.IncludeInBenefitBase).HasDefaultValue(false);
            e.Property(x => x.RequiresApproval).HasDefaultValue(false);
            e.Property(x => x.Notes).HasColumnType("NVARCHAR(MAX)").HasDefaultValue(string.Empty);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Ignore(x => x.Organization);
            e.HasIndex(x => x.OrgID);
        });

        modelBuilder.Entity<DeductionRule>(e =>
        {
            e.ToTable("DeductionRules");
            e.HasKey(x => x.DeductionRuleID);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Category).HasMaxLength(30).HasDefaultValue("Statutory");
            e.Property(x => x.DeductionType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(10,4)");
            e.Property(x => x.FormulaExpression).HasMaxLength(500);
            e.Property(x => x.EmployerSharePercent).HasColumnType("decimal(6,4)");
            e.Property(x => x.EmployeeSharePercent).HasColumnType("decimal(6,4)");
            e.Property(x => x.HasEmployerShare).HasDefaultValue(false);
            e.Property(x => x.HasEmployeeShare).HasDefaultValue(true);
            e.Property(x => x.AutoCompute).HasDefaultValue(true);
            e.Property(x => x.ComputeBasedOn).HasMaxLength(30).HasDefaultValue("BasicPay");
            e.Property(x => x.MaxDeductionAmount).HasColumnType("decimal(12,2)");
            e.Property(x => x.ScopeTagsJson).HasColumnType("NVARCHAR(MAX)").HasDefaultValue("[]");
            e.Property(x => x.Notes).HasColumnType("NVARCHAR(MAX)").HasDefaultValue(string.Empty);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Ignore(x => x.Organization);
            e.HasIndex(x => x.OrgID);
        });

        modelBuilder.Entity<OrgAllowanceRule>(e =>
        {
            e.ToTable("OrgAllowanceRules");
            e.HasKey(x => x.OrgAllowanceRuleID);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.AllowanceType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Category).HasMaxLength(40).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(12,2)");
            e.Property(x => x.ScopeTagsJson).HasColumnType("NVARCHAR(MAX)").HasDefaultValue("[]");
            e.Property(x => x.ComplianceNotes).HasMaxLength(400);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Ignore(x => x.Organization);
            e.HasIndex(x => x.OrgID);
        });
    }
}
