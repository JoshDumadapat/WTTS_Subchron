using Microsoft.EntityFrameworkCore;
using Subchron.API.Models.Entities;

namespace Subchron.API.Data;

public class SubchronDbContext : DbContext
{
    public SubchronDbContext(DbContextOptions<SubchronDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuthLoginSession> AuthLoginSessions => Set<AuthLoginSession>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<BillingRecord> BillingRecords => Set<BillingRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organizations
        modelBuilder.Entity<Organization>(e =>
        {
            e.ToTable("Organizations");
            e.HasKey(x => x.OrgID);

            e.Property(x => x.OrgName).HasMaxLength(100).IsRequired();
            e.Property(x => x.OrgCode).HasMaxLength(20).IsRequired(); // ✅ 20
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();

            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgCode).IsUnique();
        });

        // OrganizationSettings (1:1, PK=FK)
        modelBuilder.Entity<OrganizationSettings>(e =>
        {
            e.ToTable("OrganizationSettings");
            e.HasKey(x => x.OrgID);

            e.Property(x => x.Timezone).HasMaxLength(50).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            e.Property(x => x.AttendanceMode).HasMaxLength(20).IsRequired();
            e.Property(x => x.RoundRule).HasMaxLength(20).IsRequired();

            e.Property(x => x.OTThresholdHours).HasColumnType("decimal(6,2)");
            e.Property(x => x.OTMaxHoursPerDay).HasColumnType("decimal(6,2)");

            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Organization)
                .WithOne(o => o.Settings)
                .HasForeignKey<OrganizationSettings>(x => x.OrgID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Plans
        modelBuilder.Entity<Plan>(e =>
        {
            e.ToTable("Plans");
            e.HasKey(x => x.PlanID);

            e.Property(x => x.PlanName).HasMaxLength(20).IsRequired(); // ✅ 20
            e.Property(x => x.BasePrice).HasColumnType("decimal(10,2)");
            e.Property(x => x.IsActive).HasDefaultValue(true);

            e.HasIndex(x => x.PlanName).IsUnique();
        });

        // Subscriptions
        modelBuilder.Entity<Subscription>(e =>
        {
            e.ToTable("Subscriptions");
            e.HasKey(x => x.SubscriptionID);

            e.Property(x => x.AttendanceMode).HasMaxLength(20).IsRequired();
            e.Property(x => x.BasePrice).HasColumnType("decimal(10,2)");
            e.Property(x => x.ModePrice).HasColumnType("decimal(10,2)");
            e.Property(x => x.FinalPrice).HasColumnType("decimal(10,2)");
            e.Property(x => x.BillingCycle).HasMaxLength(10).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();

            e.Property(x => x.StartDate).HasDefaultValueSql("CONVERT(date, SYSUTCDATETIME())");

            e.HasOne(x => x.Organization)
                .WithMany(o => o.Subscriptions)
                .HasForeignKey(x => x.OrgID)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(x => x.PlanID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Users (Role stored as int column)
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users", t =>
                t.HasCheckConstraint("CK_Users_Role_Valid", "[Role] IN (1,2,3,4,5,6,7)"));

            e.HasKey(x => x.UserID);

            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Password).HasMaxLength(255).IsRequired();
            e.Property(x => x.AvatarUrl).HasMaxLength(500);

            e.Property(x => x.Role)
                .HasConversion<int>()
                .HasColumnType("int")
                .IsRequired();

            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.TotpEnabled).HasDefaultValue(false);

            e.Property(x => x.TotpSecret).HasColumnType("varbinary(64)");
            e.Property(x => x.RecoveryCodesHash).HasColumnType("NVARCHAR(MAX)");

            e.Property(x => x.ExternalProvider).HasMaxLength(20);
            e.Property(x => x.ExternalId).HasMaxLength(128);
            e.Property(x => x.EmailVerified).HasDefaultValue(false);

            e.HasIndex(x => new { x.ExternalProvider, x.ExternalId })
                .IsUnique()
                .HasFilter("[ExternalProvider] IS NOT NULL AND [ExternalId] IS NOT NULL");

            e.HasIndex(x => x.Email).IsUnique();

            e.HasOne(x => x.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(x => x.OrgID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Employees (WorkState = HR condition; IsArchived = left org)
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

            e.HasIndex(x => x.AttendanceQrToken).IsUnique().HasFilter("[AttendanceQrToken] IS NOT NULL");

            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrgID)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserID)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.OrgID, x.EmpNumber }).IsUnique();
            e.HasIndex(x => new { x.OrgID, x.PhoneNormalized }).IsUnique().HasFilter("[PhoneNormalized] IS NOT NULL AND [PhoneNormalized] != ''");
        });

        // Departments: unique name per organization (same name allowed in different orgs)
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("Departments");
            e.HasKey(x => x.DepID);
            e.Property(x => x.DepartmentName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Organization)
                .WithMany(o => o.Departments)
                .HasForeignKey(x => x.OrgID)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => new { x.OrgID, x.DepartmentName }).IsUnique();
        });

        // AuditLogs (no sensitive data; IpAddress/UserAgent for traceability)
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.AuditID);
            e.Property(x => x.Action).HasMaxLength(80).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(60);
            e.Property(x => x.Details).HasMaxLength(500);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrgID)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserID)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.UserID);
            e.HasIndex(x => x.CreatedAt);
        });

        // LeaveRequests
        modelBuilder.Entity<LeaveRequest>(e =>
        {
            e.ToTable("LeaveRequests");
            e.HasKey(x => x.LeaveRequestID);
            e.Property(x => x.LeaveType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.ReviewNotes).HasMaxLength(300);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrgID).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmpID).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserID).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.EmpID);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.StartDate);
        });

        // ShiftAssignments (OrgID NoAction to avoid multiple cascade paths: Org->Employees->ShiftAssignments)
        modelBuilder.Entity<ShiftAssignment>(e =>
        {
            e.ToTable("ShiftAssignments");
            e.HasKey(x => x.ShiftAssignmentID);
            e.Property(x => x.Notes).HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrgID).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmpID).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.OrgID, x.AssignmentDate });
            e.HasIndex(x => x.EmpID);
        });

        // PasswordResetTokens
        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.ToTable("PasswordResetTokens");
            e.HasKey(x => x.TokenID);

            e.Property(x => x.TokenHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(x => x.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.UserID);
            e.HasIndex(x => x.TokenHash);
        });

        // AuthLoginSessions
        modelBuilder.Entity<AuthLoginSession>(e =>
        {
            e.ToTable("AuthLoginSessions");
            e.HasKey(x => x.SessionID);

            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.User)
                .WithMany(u => u.AuthLoginSessions)
                .HasForeignKey(x => x.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.UserID);
        });

        // PaymentTransaction: subscription/payment history (super admin sales, audit). Max lengths for DB space.
        modelBuilder.Entity<PaymentTransaction>(e =>
        {
            e.ToTable("PaymentTransactions");
            e.HasKey(x => x.Id);

            e.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.PayMongoPaymentIntentId).HasMaxLength(100);
            e.Property(x => x.PayMongoPaymentId).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.FailureCode).HasMaxLength(50);
            e.Property(x => x.FailureMessage).HasMaxLength(200);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Organization)
                .WithMany(o => o.PaymentTransactions)
                .HasForeignKey(x => x.OrgID)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.User)
                .WithMany(u => u.PaymentTransactions)
                .HasForeignKey(x => x.UserID)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Subscription)
                .WithMany()
                .HasForeignKey(x => x.SubscriptionID)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.UserID);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.PayMongoPaymentIntentId).IsUnique().HasFilter("[PayMongoPaymentIntentId] IS NOT NULL");
        });

        // BillingRecord: billing info snapshot per payment (no CVC, no full card). Tight lengths.
        modelBuilder.Entity<BillingRecord>(e =>
        {
            e.ToTable("BillingRecords");
            e.HasKey(x => x.Id);

            e.Property(x => x.Last4).HasMaxLength(4);
            e.Property(x => x.Expiry).HasMaxLength(5);
            e.Property(x => x.Brand).HasMaxLength(20);
            e.Property(x => x.BillingEmail).HasMaxLength(256);
            e.Property(x => x.BillingPhone).HasMaxLength(11);
            e.Property(x => x.PayMongoPaymentMethodId).HasMaxLength(80);
            e.Property(x => x.PayMongoCustomerId).HasMaxLength(80);
            e.Property(x => x.NameOnCard).HasMaxLength(100);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Organization)
                .WithMany(o => o.BillingRecords)
                .HasForeignKey(x => x.OrgID)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
                .WithMany(u => u.BillingRecords)
                .HasForeignKey(x => x.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.PaymentTransaction)
                .WithMany(t => t.BillingRecords)
                .HasForeignKey(x => x.PaymentTransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.UserID);
        });

        // Seed Plans
        modelBuilder.Entity<Plan>().HasData(
            new Plan { PlanID = 1, PlanName = "Basic", BasePrice = 0m, MaxEmployees = 200, RetentionMonths = 3, IsActive = true },
            new Plan { PlanID = 2, PlanName = "Standard", BasePrice = 0m, MaxEmployees = 400, RetentionMonths = 12, IsActive = true },
            new Plan { PlanID = 3, PlanName = "Enterprise", BasePrice = 0m, MaxEmployees = 800, RetentionMonths = 24, IsActive = true }
        );

        // Seed SuperAdmin user
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserID = 1,
                OrgID = null,
                Name = "Josh Dumadapat",
                Email = "ivanjoshdumadapat30@gmail.com",
                Password = "$2a$11$Aq6k23IxUxMsGwOMuwWQme7xSDkzu3N47OHmvsB44dQxdVLJLyMBe",
                Role = UserRoleType.SuperAdmin,
                IsActive = true,
                TotpEnabled = false,
                EmailVerified = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}