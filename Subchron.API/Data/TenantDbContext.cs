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
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<SignupPending> SignupPendings => Set<SignupPending>();

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
            e.Property(x => x.Age);
            e.Property(x => x.Gender).HasMaxLength(20);
            e.Property(x => x.Role).HasMaxLength(40).IsRequired();
            e.Property(x => x.WorkState).HasMaxLength(40).IsRequired();
            e.Property(x => x.EmploymentType).HasMaxLength(40).IsRequired();
            e.Property(x => x.AddressLine1).HasMaxLength(120);
            e.Property(x => x.AddressLine2).HasMaxLength(120);
            e.Property(x => x.City).HasMaxLength(80);
            e.Property(x => x.StateProvince).HasMaxLength(80);
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.Country).HasMaxLength(80);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.PhoneNormalized).HasMaxLength(20);
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
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentID).OnDelete(DeleteBehavior.SetNull);
        });

        // ----- Department: OrgID scalar (soft ref) -----
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("Departments");
            e.HasKey(x => x.DepID);
            e.Property(x => x.DepartmentName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200);
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
            e.HasIndex(x => x.OrgID);
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
    }
}
