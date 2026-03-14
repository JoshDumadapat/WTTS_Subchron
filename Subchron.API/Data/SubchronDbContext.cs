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
    public DbSet<SuperAdminExpense> SuperAdminExpenses => Set<SuperAdminExpense>();
    public DbSet<SuperAdminAuditLog> SuperAdminAuditLogs => Set<SuperAdminAuditLog>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();
    public DbSet<OrganizationPaymentMethod> OrganizationPaymentMethods => Set<OrganizationPaymentMethod>();
    public DbSet<DemoRequest> DemoRequests => Set<DemoRequest>();


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
            e.Property(x => x.DefaultShiftTemplateCode).HasMaxLength(60);
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

        // SuperAdminExpense
        modelBuilder.Entity<SuperAdminExpense>(e =>
        {
            e.ToTable("SuperAdminExpenses");
            e.HasKey(x => x.Id);
            e.Property(x => x.OccurredAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Tin).HasMaxLength(30);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.OccurredAt);
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

        // Seed Plans
        modelBuilder.Entity<Plan>().HasData(
            new Plan { PlanID = 1, PlanName = "Basic", BasePrice = 0m, MaxEmployees = 200, RetentionMonths = 3, IsActive = true },
            new Plan { PlanID = 2, PlanName = "Standard", BasePrice = 0m, MaxEmployees = 400, RetentionMonths = 12, IsActive = true },
            new Plan { PlanID = 3, PlanName = "Enterprise", BasePrice = 0m, MaxEmployees = 800, RetentionMonths = 24, IsActive = true }
        );

        // Seed demo organizations for SuperAdmin analytics
        modelBuilder.Entity<Organization>().HasData(
            new Organization { OrgID = 101, OrgName = "Northwind Systems", OrgCode = "NWS101", Status = "Active", CreatedAt = new DateTime(2025, 12, 2, 0, 0, 0, DateTimeKind.Utc) },
            new Organization { OrgID = 102, OrgName = "Skyline Retail Group", OrgCode = "SRG102", Status = "Active", CreatedAt = new DateTime(2025, 12, 5, 0, 0, 0, DateTimeKind.Utc) },
            new Organization { OrgID = 103, OrgName = "Pacific Data Labs", OrgCode = "PDL103", Status = "Active", CreatedAt = new DateTime(2025, 12, 8, 0, 0, 0, DateTimeKind.Utc) },
            new Organization { OrgID = 104, OrgName = "Metro Health Ops", OrgCode = "MHO104", Status = "Active", CreatedAt = new DateTime(2025, 12, 11, 0, 0, 0, DateTimeKind.Utc) },
            new Organization { OrgID = 105, OrgName = "Vertex Logistics", OrgCode = "VXL105", Status = "Active", CreatedAt = new DateTime(2025, 12, 14, 0, 0, 0, DateTimeKind.Utc) },
            new Organization { OrgID = 106, OrgName = "Bluepeak Services", OrgCode = "BPS106", Status = "Trial", CreatedAt = new DateTime(2025, 12, 17, 0, 0, 0, DateTimeKind.Utc) },
            new Organization { OrgID = 107, OrgName = "Crescent Foods", OrgCode = "CRF107", Status = "Active", CreatedAt = new DateTime(2025, 12, 20, 0, 0, 0, DateTimeKind.Utc) },
            new Organization { OrgID = 108, OrgName = "Omni Build Works", OrgCode = "OBW108", Status = "Suspended", CreatedAt = new DateTime(2025, 12, 23, 0, 0, 0, DateTimeKind.Utc) },
            new Organization { OrgID = 109, OrgName = "Harbor Finance Co", OrgCode = "HFC109", Status = "Active", CreatedAt = new DateTime(2025, 12, 26, 0, 0, 0, DateTimeKind.Utc) },
            new Organization { OrgID = 110, OrgName = "Summit Education", OrgCode = "SME110", Status = "Trial", CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<OrganizationSettings>().HasData(
            new OrganizationSettings { OrgID = 101, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "QR", DefaultShiftTemplateCode = "DAY", CreatedAt = new DateTime(2025, 12, 2, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new OrganizationSettings { OrgID = 102, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "Hybrid", DefaultShiftTemplateCode = "DAY", CreatedAt = new DateTime(2025, 12, 5, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new OrganizationSettings { OrgID = 103, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "Biometric", DefaultShiftTemplateCode = "NIGHT", CreatedAt = new DateTime(2025, 12, 8, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new OrganizationSettings { OrgID = 104, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "QR", DefaultShiftTemplateCode = "DAY", CreatedAt = new DateTime(2025, 12, 11, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new OrganizationSettings { OrgID = 105, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "Hybrid", DefaultShiftTemplateCode = "DAY", CreatedAt = new DateTime(2025, 12, 14, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new OrganizationSettings { OrgID = 106, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "QR", DefaultShiftTemplateCode = "DAY", CreatedAt = new DateTime(2025, 12, 17, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new OrganizationSettings { OrgID = 107, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "Biometric", DefaultShiftTemplateCode = "NIGHT", CreatedAt = new DateTime(2025, 12, 20, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new OrganizationSettings { OrgID = 108, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "QR", DefaultShiftTemplateCode = "DAY", CreatedAt = new DateTime(2025, 12, 23, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new OrganizationSettings { OrgID = 109, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "Hybrid", DefaultShiftTemplateCode = "DAY", CreatedAt = new DateTime(2025, 12, 26, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new OrganizationSettings { OrgID = 110, Timezone = "Asia/Manila", Currency = "PHP", AttendanceMode = "QR", DefaultShiftTemplateCode = "DAY", CreatedAt = new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<Subscription>().HasData(
            new Subscription { SubscriptionID = 1001, OrgID = 101, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1002, OrgID = 101, PlanID = 2, AttendanceMode = "QR", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1003, OrgID = 101, PlanID = 2, AttendanceMode = "QR", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1004, OrgID = 101, PlanID = 3, AttendanceMode = "Hybrid", BasePrice = 8999m, ModePrice = 0m, FinalPrice = 8999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Active" },

            new Subscription { SubscriptionID = 1005, OrgID = 102, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1006, OrgID = 102, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1007, OrgID = 102, PlanID = 2, AttendanceMode = "Hybrid", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1008, OrgID = 102, PlanID = 2, AttendanceMode = "Hybrid", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Active" },

            new Subscription { SubscriptionID = 1009, OrgID = 103, PlanID = 2, AttendanceMode = "Biometric", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1010, OrgID = 103, PlanID = 2, AttendanceMode = "Biometric", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1011, OrgID = 103, PlanID = 3, AttendanceMode = "Biometric", BasePrice = 8999m, ModePrice = 0m, FinalPrice = 8999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1012, OrgID = 103, PlanID = 3, AttendanceMode = "Biometric", BasePrice = 8999m, ModePrice = 0m, FinalPrice = 8999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Active" },

            new Subscription { SubscriptionID = 1013, OrgID = 104, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1014, OrgID = 104, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1015, OrgID = 104, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1016, OrgID = 104, PlanID = 2, AttendanceMode = "QR", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Active" },

            new Subscription { SubscriptionID = 1017, OrgID = 105, PlanID = 2, AttendanceMode = "Hybrid", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1018, OrgID = 105, PlanID = 2, AttendanceMode = "Hybrid", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1019, OrgID = 105, PlanID = 3, AttendanceMode = "Hybrid", BasePrice = 8999m, ModePrice = 0m, FinalPrice = 8999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1020, OrgID = 105, PlanID = 3, AttendanceMode = "Hybrid", BasePrice = 8999m, ModePrice = 0m, FinalPrice = 8999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Active" },

            new Subscription { SubscriptionID = 1021, OrgID = 106, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1022, OrgID = 106, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1023, OrgID = 106, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1024, OrgID = 106, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Trial" },

            new Subscription { SubscriptionID = 1025, OrgID = 107, PlanID = 1, AttendanceMode = "Biometric", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1026, OrgID = 107, PlanID = 2, AttendanceMode = "Biometric", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1027, OrgID = 107, PlanID = 2, AttendanceMode = "Biometric", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1028, OrgID = 107, PlanID = 3, AttendanceMode = "Biometric", BasePrice = 8999m, ModePrice = 0m, FinalPrice = 8999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Active" },

            new Subscription { SubscriptionID = 1029, OrgID = 108, PlanID = 2, AttendanceMode = "QR", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1030, OrgID = 108, PlanID = 2, AttendanceMode = "QR", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1031, OrgID = 108, PlanID = 2, AttendanceMode = "QR", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1032, OrgID = 108, PlanID = 2, AttendanceMode = "QR", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Suspended" },

            new Subscription { SubscriptionID = 1033, OrgID = 109, PlanID = 1, AttendanceMode = "Hybrid", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1034, OrgID = 109, PlanID = 1, AttendanceMode = "Hybrid", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1035, OrgID = 109, PlanID = 2, AttendanceMode = "Hybrid", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1036, OrgID = 109, PlanID = 2, AttendanceMode = "Hybrid", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Active" },

            new Subscription { SubscriptionID = 1037, OrgID = 110, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1038, OrgID = 110, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1039, OrgID = 110, PlanID = 1, AttendanceMode = "QR", BasePrice = 2499m, ModePrice = 0m, FinalPrice = 2499m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), Status = "Expired" },
            new Subscription { SubscriptionID = 1040, OrgID = 110, PlanID = 2, AttendanceMode = "QR", BasePrice = 5999m, ModePrice = 0m, FinalPrice = 5999m, BillingCycle = "Monthly", StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), Status = "Trial" }
        );

        modelBuilder.Entity<PaymentTransaction>().HasData(
            new PaymentTransaction { Id = 2001, OrgID = 101, SubscriptionID = 1001, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2001", PayMongoPaymentId = "seed-pay-2001", Description = "Subchron - Basic", CreatedAt = new DateTime(2025, 12, 1, 8, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2002, OrgID = 101, SubscriptionID = 1002, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2002", PayMongoPaymentId = "seed-pay-2002", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2003, OrgID = 101, SubscriptionID = 1003, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2003", PayMongoPaymentId = "seed-pay-2003", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2004, OrgID = 101, SubscriptionID = 1004, Amount = 8999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2004", PayMongoPaymentId = "seed-pay-2004", Description = "Subchron - Enterprise", CreatedAt = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2005, OrgID = 102, SubscriptionID = 1005, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2005", PayMongoPaymentId = "seed-pay-2005", Description = "Subchron - Basic", CreatedAt = new DateTime(2025, 12, 1, 8, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2006, OrgID = 102, SubscriptionID = 1006, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2006", PayMongoPaymentId = "seed-pay-2006", Description = "Subchron - Basic", CreatedAt = new DateTime(2026, 1, 1, 8, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2007, OrgID = 102, SubscriptionID = 1007, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2007", PayMongoPaymentId = "seed-pay-2007", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 2, 1, 8, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2008, OrgID = 102, SubscriptionID = 1008, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2008", PayMongoPaymentId = "seed-pay-2008", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 3, 1, 8, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2009, OrgID = 103, SubscriptionID = 1009, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2009", PayMongoPaymentId = "seed-pay-2009", Description = "Subchron - Standard", CreatedAt = new DateTime(2025, 12, 1, 8, 30, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2010, OrgID = 103, SubscriptionID = 1010, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2010", PayMongoPaymentId = "seed-pay-2010", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 1, 1, 8, 30, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2011, OrgID = 103, SubscriptionID = 1011, Amount = 8999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2011", PayMongoPaymentId = "seed-pay-2011", Description = "Subchron - Enterprise", CreatedAt = new DateTime(2026, 2, 1, 8, 30, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2012, OrgID = 103, SubscriptionID = 1012, Amount = 8999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2012", PayMongoPaymentId = "seed-pay-2012", Description = "Subchron - Enterprise", CreatedAt = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2013, OrgID = 104, SubscriptionID = 1013, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2013", PayMongoPaymentId = "seed-pay-2013", Description = "Subchron - Basic", CreatedAt = new DateTime(2025, 12, 1, 8, 45, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2014, OrgID = 104, SubscriptionID = 1014, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2014", PayMongoPaymentId = "seed-pay-2014", Description = "Subchron - Basic", CreatedAt = new DateTime(2026, 1, 1, 8, 45, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2015, OrgID = 104, SubscriptionID = 1015, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2015", PayMongoPaymentId = "seed-pay-2015", Description = "Subchron - Basic", CreatedAt = new DateTime(2026, 2, 1, 8, 45, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2016, OrgID = 104, SubscriptionID = 1016, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2016", PayMongoPaymentId = "seed-pay-2016", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 3, 1, 8, 45, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2017, OrgID = 105, SubscriptionID = 1017, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2017", PayMongoPaymentId = "seed-pay-2017", Description = "Subchron - Standard", CreatedAt = new DateTime(2025, 12, 1, 9, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2018, OrgID = 105, SubscriptionID = 1018, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2018", PayMongoPaymentId = "seed-pay-2018", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2019, OrgID = 105, SubscriptionID = 1019, Amount = 8999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2019", PayMongoPaymentId = "seed-pay-2019", Description = "Subchron - Enterprise", CreatedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2020, OrgID = 105, SubscriptionID = 1020, Amount = 8999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2020", PayMongoPaymentId = "seed-pay-2020", Description = "Subchron - Enterprise", CreatedAt = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2021, OrgID = 106, SubscriptionID = 1021, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2021", PayMongoPaymentId = "seed-pay-2021", Description = "Subchron - Basic", CreatedAt = new DateTime(2025, 12, 1, 9, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2022, OrgID = 106, SubscriptionID = 1022, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2022", PayMongoPaymentId = "seed-pay-2022", Description = "Subchron - Basic", CreatedAt = new DateTime(2026, 1, 1, 9, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2023, OrgID = 106, SubscriptionID = 1023, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2023", PayMongoPaymentId = "seed-pay-2023", Description = "Subchron - Basic", CreatedAt = new DateTime(2026, 2, 1, 9, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2024, OrgID = 106, SubscriptionID = 1024, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2024", PayMongoPaymentId = "seed-pay-2024", Description = "Subchron - Basic", CreatedAt = new DateTime(2026, 3, 1, 9, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2025, OrgID = 107, SubscriptionID = 1025, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2025", PayMongoPaymentId = "seed-pay-2025", Description = "Subchron - Basic", CreatedAt = new DateTime(2025, 12, 1, 9, 30, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2026, OrgID = 107, SubscriptionID = 1026, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2026", PayMongoPaymentId = "seed-pay-2026", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 1, 1, 9, 30, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2027, OrgID = 107, SubscriptionID = 1027, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2027", PayMongoPaymentId = "seed-pay-2027", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 2, 1, 9, 30, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2028, OrgID = 107, SubscriptionID = 1028, Amount = 8999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2028", PayMongoPaymentId = "seed-pay-2028", Description = "Subchron - Enterprise", CreatedAt = new DateTime(2026, 3, 1, 9, 30, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2029, OrgID = 108, SubscriptionID = 1029, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2029", PayMongoPaymentId = "seed-pay-2029", Description = "Subchron - Standard", CreatedAt = new DateTime(2025, 12, 1, 9, 45, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2030, OrgID = 108, SubscriptionID = 1030, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2030", PayMongoPaymentId = "seed-pay-2030", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 1, 1, 9, 45, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2031, OrgID = 108, SubscriptionID = 1031, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2031", PayMongoPaymentId = "seed-pay-2031", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 2, 1, 9, 45, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2032, OrgID = 108, SubscriptionID = 1032, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2032", PayMongoPaymentId = "seed-pay-2032", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 3, 1, 9, 45, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2033, OrgID = 109, SubscriptionID = 1033, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2033", PayMongoPaymentId = "seed-pay-2033", Description = "Subchron - Basic", CreatedAt = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2034, OrgID = 109, SubscriptionID = 1034, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2034", PayMongoPaymentId = "seed-pay-2034", Description = "Subchron - Basic", CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2035, OrgID = 109, SubscriptionID = 1035, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2035", PayMongoPaymentId = "seed-pay-2035", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2036, OrgID = 109, SubscriptionID = 1036, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2036", PayMongoPaymentId = "seed-pay-2036", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2037, OrgID = 110, SubscriptionID = 1037, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2037", PayMongoPaymentId = "seed-pay-2037", Description = "Subchron - Basic", CreatedAt = new DateTime(2025, 12, 1, 10, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2038, OrgID = 110, SubscriptionID = 1038, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2038", PayMongoPaymentId = "seed-pay-2038", Description = "Subchron - Basic", CreatedAt = new DateTime(2026, 1, 1, 10, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2039, OrgID = 110, SubscriptionID = 1039, Amount = 2499m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2039", PayMongoPaymentId = "seed-pay-2039", Description = "Subchron - Basic", CreatedAt = new DateTime(2026, 2, 1, 10, 15, 0, DateTimeKind.Utc) },
            new PaymentTransaction { Id = 2040, OrgID = 110, SubscriptionID = 1040, Amount = 5999m, Currency = "PHP", Status = "paid", PayMongoPaymentIntentId = "seed-pi-2040", PayMongoPaymentId = "seed-pay-2040", Description = "Subchron - Standard", CreatedAt = new DateTime(2026, 3, 1, 10, 15, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<SuperAdminExpense>().HasData(
            new SuperAdminExpense { Id = 3001, OccurredAt = new DateTime(2025, 12, 5, 0, 0, 0, DateTimeKind.Utc), Description = "Cloud hosting and compute", Amount = 42000m, ReferenceNumber = "EXP-2025-12-001", Tin = "123-456-789-000", TaxAmount = 5040m, Category = "Infrastructure", Notes = "December cloud invoice", CreatedByUserId = 1, CreatedAt = new DateTime(2025, 12, 5, 1, 0, 0, DateTimeKind.Utc) },
            new SuperAdminExpense { Id = 3002, OccurredAt = new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), Description = "Marketing and ads", Amount = 18000m, ReferenceNumber = "EXP-2025-12-002", Tin = "123-456-789-000", TaxAmount = 2160m, Category = "Marketing", Notes = "December campaigns", CreatedByUserId = 1, CreatedAt = new DateTime(2025, 12, 15, 1, 0, 0, DateTimeKind.Utc) },
            new SuperAdminExpense { Id = 3003, OccurredAt = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc), Description = "Platform monitoring tools", Amount = 24000m, ReferenceNumber = "EXP-2026-01-001", Tin = "123-456-789-000", TaxAmount = 2880m, Category = "Infrastructure", Notes = "January tools renewal", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 1, 6, 1, 0, 0, DateTimeKind.Utc) },
            new SuperAdminExpense { Id = 3004, OccurredAt = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), Description = "Operations and support", Amount = 15000m, ReferenceNumber = "EXP-2026-01-002", Tin = "123-456-789-000", TaxAmount = 1800m, Category = "Personnel", Notes = "Support contractor payout", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 1, 20, 1, 0, 0, DateTimeKind.Utc) },
            new SuperAdminExpense { Id = 3005, OccurredAt = new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc), Description = "Data backup storage", Amount = 12000m, ReferenceNumber = "EXP-2026-02-001", Tin = "123-456-789-000", TaxAmount = 1440m, Category = "Infrastructure", Notes = "Backup and archival", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 2, 4, 1, 0, 0, DateTimeKind.Utc) },
            new SuperAdminExpense { Id = 3006, OccurredAt = new DateTime(2026, 2, 18, 0, 0, 0, DateTimeKind.Utc), Description = "Office and admin", Amount = 9000m, ReferenceNumber = "EXP-2026-02-002", Tin = "123-456-789-000", TaxAmount = 1080m, Category = "Office", Notes = "Office and legal supplies", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 2, 18, 1, 0, 0, DateTimeKind.Utc) },
            new SuperAdminExpense { Id = 3007, OccurredAt = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc), Description = "Security and compliance tools", Amount = 26000m, ReferenceNumber = "EXP-2026-03-001", Tin = "123-456-789-000", TaxAmount = 3120m, Category = "Infrastructure", Notes = "March security stack", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 3, 3, 1, 0, 0, DateTimeKind.Utc) },
            new SuperAdminExpense { Id = 3008, OccurredAt = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc), Description = "Sales outreach and events", Amount = 17000m, ReferenceNumber = "EXP-2026-03-002", Tin = "123-456-789-000", TaxAmount = 2040m, Category = "Marketing", Notes = "March partner events", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 3, 12, 1, 0, 0, DateTimeKind.Utc) }
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
