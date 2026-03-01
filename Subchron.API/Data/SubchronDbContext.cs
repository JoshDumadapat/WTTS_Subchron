using Microsoft.EntityFrameworkCore;
using Subchron.API.Models.Entities;

namespace Subchron.API.Data;

/// <summary>Platform/SuperAdmin DB: Organizations, Users, Plans, Subscriptions, Billing, Auth, SuperAdminAuditLog, DemoRequest. No tenant tables.</summary>
public class SubchronDbContext : DbContext
{
    public SubchronDbContext(DbContextOptions<SubchronDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<OrganizationProfile> OrganizationProfiles => Set<OrganizationProfile>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuthLoginSession> AuthLoginSessions => Set<AuthLoginSession>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<BillingRecord> BillingRecords => Set<BillingRecord>();
    public DbSet<SuperAdminAuditLog> SuperAdminAuditLogs => Set<SuperAdminAuditLog>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();
    public DbSet<OrganizationPaymentMethod> OrganizationPaymentMethods => Set<OrganizationPaymentMethod>();
    public DbSet<DemoRequest> DemoRequests => Set<DemoRequest>();

    public DbSet<EarningRule> EarningRules => Set<EarningRule>();
    public DbSet<DeductionRule> DeductionRules => Set<DeductionRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organizations
        modelBuilder.Entity<Organization>(e =>
        {
            e.ToTable("Organizations");
            e.HasKey(x => x.OrgID);
            e.Property(x => x.OrgName).HasMaxLength(100).IsRequired();
            e.Property(x => x.OrgCode).HasMaxLength(20).IsRequired();
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
            e.Property(x => x.AutoClockOutMaxHours).HasColumnType("decimal(5,2)");
            e.Property(x => x.DefaultShiftTemplateCode).HasMaxLength(60);
            e.Property(x => x.OTThresholdHours).HasColumnType("decimal(6,2)");
            e.Property(x => x.OTMaxHoursPerDay).HasColumnType("decimal(6,2)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Organization)
                .WithOne(o => o.Settings)
                .HasForeignKey<OrganizationSettings>(x => x.OrgID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrganizationProfiles (1:1, PK=FK)
        modelBuilder.Entity<OrganizationProfile>(e =>
        {
            e.ToTable("OrganizationProfiles");
            e.HasKey(x => x.OrgID);
            e.Property(x => x.LogoUrl).HasMaxLength(500);
            e.Property(x => x.AddressLine1).HasMaxLength(150);
            e.Property(x => x.AddressLine2).HasMaxLength(150);
            e.Property(x => x.City).HasMaxLength(80);
            e.Property(x => x.StateProvince).HasMaxLength(80);
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.Country).HasMaxLength(80);
            e.Property(x => x.ContactEmail).HasMaxLength(256);
            e.Property(x => x.ContactPhone).HasMaxLength(40);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Organization)
                .WithOne(o => o.Profile)
                .HasForeignKey<OrganizationProfile>(x => x.OrgID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Plans
        modelBuilder.Entity<Plan>(e =>
        {
            e.ToTable("Plans");
            e.HasKey(x => x.PlanID);
            e.Property(x => x.PlanName).HasMaxLength(20).IsRequired();
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
            e.Property(x => x.Role).HasConversion<int>().HasColumnType("int").IsRequired();
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

        // PaymentTransaction
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

        // BillingRecord
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

        // SuperAdminAuditLogs (platform-only)
        modelBuilder.Entity<SuperAdminAuditLog>(e =>
        {
            e.ToTable("SuperAdminAuditLogs");
            e.HasKey(x => x.SuperAdminAuditLogID);
            e.Property(x => x.Action).HasMaxLength(80).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(60);
            e.Property(x => x.Details).HasMaxLength(500);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgID);
            e.HasIndex(x => x.UserID);
            e.HasIndex(x => x.CreatedAt);
        });

        // EmailVerificationCodes (platform)
        modelBuilder.Entity<EmailVerificationCode>(e =>
        {
            e.ToTable("EmailVerificationCodes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.CodeHash).HasMaxLength(255).IsRequired();
        });

        // OrganizationPaymentMethods (platform)
        modelBuilder.Entity<OrganizationPaymentMethod>(e =>
        {
            e.ToTable("OrganizationPaymentMethods");
            e.HasKey(x => x.Id);
            e.Property(x => x.PayMongoPaymentMethodId).HasMaxLength(80);
            e.Property(x => x.PayMongoCustomerId).HasMaxLength(80);
            e.Property(x => x.Type).HasMaxLength(20).IsRequired();
            e.Property(x => x.Last4).HasMaxLength(4);
            e.Property(x => x.Brand).HasMaxLength(20);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrgID).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.OrgID);
        });

        // DemoRequests (platform - pre-signup demo requests)
        modelBuilder.Entity<DemoRequest>(e =>
        {
            e.ToTable("DemoRequests");
            e.HasKey(x => x.DemoRequestID);
            e.Property(x => x.OrgName).HasMaxLength(100).IsRequired();
            e.Property(x => x.ContactName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.OrgSize).HasMaxLength(20);
            e.Property(x => x.DesiredMode).HasMaxLength(20);
            e.Property(x => x.Message).HasMaxLength(255);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OrgID);
        });

        // EarningRules
        modelBuilder.Entity<EarningRule>(e =>
        {
            e.ToTable("EarningRules");
            e.HasKey(x => x.EarningRuleID);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.AppliesTo).HasMaxLength(30).IsRequired();
            e.Property(x => x.RateType).HasMaxLength(20).IsRequired();
            e.Property(x => x.RateValue).HasColumnType("decimal(10,4)");
            e.Property(x => x.IsTaxable).HasDefaultValue(true);
            e.Property(x => x.IncludeInBenefitBase).HasDefaultValue(false);
            e.Property(x => x.RequiresApproval).HasDefaultValue(false);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrgID)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.OrgID);
        });

        // DeductionRules
        modelBuilder.Entity<DeductionRule>(e =>
        {
            e.ToTable("DeductionRules");
            e.HasKey(x => x.DeductionRuleID);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.DeductionType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(10,4)");
            e.Property(x => x.FormulaExpression).HasMaxLength(500);
            e.Property(x => x.EmployerSharePercent).HasColumnType("decimal(6,4)");
            e.Property(x => x.EmployeeSharePercent).HasColumnType("decimal(6,4)");
            e.Property(x => x.HasEmployerShare).HasDefaultValue(false);
            e.Property(x => x.HasEmployeeShare).HasDefaultValue(true);
            e.Property(x => x.AutoCompute).HasDefaultValue(true);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrgID)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.OrgID);
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
