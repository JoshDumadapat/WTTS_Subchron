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
    public byte[]? TotpSecret { get; set; }         
    public string? RecoveryCodesHash { get; set; }

    // Idle lock
    public bool IdleLockEnabled { get; set; } = false;
    public int? IdleLockTimeoutMinutes { get; set; }
    public DateTime? IdleLockLastSeenAt { get; set; }
    public bool IdleLockIsLocked { get; set; } = false;
    public DateTime? IdleLockLockedAt { get; set; }
    public string? IdleLockPinHash { get; set; }
    public DateTime? IdleLockPinSetAt { get; set; }
public int IdleLockPinFailedCount { get; set; } = 0;
    public DateTime? IdleLockPinLockoutUntil { get; set; }
    public DateTime? IdleLockAutoLogoutAt { get; set; }

    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    public bool EmailVerified { get; set; } = false;

    // Login lockout
    public int FailedLoginCount { get; set; } = 0;
    public int FailedLoginBatch { get; set; } = 0;
    public DateTime? FailedLoginLastAt { get; set; }
    public DateTime? LoginLockoutUntil { get; set; }

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<AuthLoginSession> AuthLoginSessions { get; set; } = new List<AuthLoginSession>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
}
