using System;
using System.Collections.Generic;

namespace Subchron.API.Models.Entities;

public enum UserRoleType
{
    SuperAdmin = 1,
    OrgAdmin = 2,
    HR = 3,
    Manager = 4,
    Employee = 5,
    Payroll = 6,
    Supervisor = 7
}

public class User
{
    public int UserID { get; set; }

    public int? OrgID { get; set; }                 // NULL for SuperAdmin
    public Organization? Organization { get; set; }

    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;  
    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserRoleType Role { get; set; } = UserRoleType.Employee;

    // 2FA
    public bool TotpEnabled { get; set; } = false;
    public byte[]? TotpSecret { get; set; }         // varbinary
    public string? RecoveryCodesHash { get; set; }

    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    public bool EmailVerified { get; set; } = false;

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<AuthLoginSession> AuthLoginSessions { get; set; } = new List<AuthLoginSession>();
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
