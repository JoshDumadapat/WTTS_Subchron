using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Services;

namespace Subchron.API.Controllers;

[ApiController]
[Authorize]
[Route("api/idle-lock")]
public class IdleLockController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly EmailService _email;

    private const int DefaultTimeoutMinutes = 1;
    private const int MaxPrivilegedTimeoutMinutes = 30;
    private const int MinTimeoutMinutes = 1;  // 1 minute for testing; normally 10
    private const int WarningGraceSeconds = 30;
    private const int PinFailLimit = 5;
    private const int PinAutoLogoutFailCount = 3;  // Auto-logout after 3 failed PIN attempts
    private const int LockedAutoLogoutMinutes = 1;  // Auto-logout after 1 minute of being locked
    private static readonly TimeSpan PinLockoutDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TempPinLifetime = TimeSpan.FromMinutes(10);

    public IdleLockController(SubchronDbContext db, EmailService email)
    {
        _db = db;
        _email = email;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    private static bool IsPrivileged(UserRoleType role)
        => role is UserRoleType.SuperAdmin or UserRoleType.OrgAdmin or UserRoleType.HR or UserRoleType.Manager
            or UserRoleType.Payroll or UserRoleType.Supervisor;

    private static int MaxAllowedMinutes(UserRoleType role) => MaxPrivilegedTimeoutMinutes;

    private static int ClampTimeout(int requested, UserRoleType role)
    {
        var max = MaxAllowedMinutes(role);
        if (requested < MinTimeoutMinutes) return MinTimeoutMinutes;
        if (requested > max) return max;
        return requested;
    }

    private static string HashPin(string pin)
        => BCrypt.Net.BCrypt.HashPassword(pin);

    private static bool VerifyPin(string pin, string? hash)
        => !string.IsNullOrWhiteSpace(hash) && BCrypt.Net.BCrypt.Verify(pin, hash);

    private static string GeneratePin()
        => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

    private static string SafeRoleLabel(UserRoleType role) => role.ToString();

    /// <returns>false when idle-lock auto-logout cleared the session lock state; caller should return 401 JSON.</returns>
    private async Task<bool> TryContinueAfterIdleAutoLogoutAsync(User user, CancellationToken ct)
    {
        if (user.IdleLockIsLocked && user.IdleLockLockedAt.HasValue && !user.IdleLockAutoLogoutAt.HasValue)
        {
            user.IdleLockAutoLogoutAt = user.IdleLockLockedAt.Value.AddMinutes(LockedAutoLogoutMinutes);
            await _db.SaveChangesAsync(ct);
        }

        if (user.IdleLockIsLocked && user.IdleLockAutoLogoutAt.HasValue && user.IdleLockAutoLogoutAt.Value <= DateTime.UtcNow)
        {
            await ForceLogoutAsync(user, ct);
            return false;
        }

        return true;
    }

    private async Task ForceLogoutAsync(User user, CancellationToken ct)
    {
        user.IdleLockIsLocked = false;
        user.IdleLockLockedAt = null;
        user.IdleLockPinFailedCount = 0;
        user.IdleLockPinLockoutUntil = null;
        user.IdleLockAutoLogoutAt = null;
        await _db.SaveChangesAsync(ct);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new { ok = false, message = "Unauthorized" });

        User? user;
        try
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value, ct);
        }
        catch (TaskCanceledException)
        {
            return Ok(new { ok = false, cancelled = true });
        }
        if (user == null) return Unauthorized(new { ok = false, message = "User not found" });

        try
        {
            if (!await TryContinueAfterIdleAutoLogoutAsync(user, ct))
                return Unauthorized(new { ok = false, message = "Session ended after prolonged lock. Please sign in again.", idleLockAutoLogout = true });
            await EnsureIdlePolicyAsync(user, ct, ensurePin: false);
        }
        catch (TaskCanceledException)
        {
            return Ok(new { ok = false, cancelled = true });
        }

        if (!user.IdleLockIsLocked && user.IdleLockLastSeenAt.HasValue && user.IdleLockTimeoutMinutes.HasValue)
        {
            var idleFor = DateTime.UtcNow - user.IdleLockLastSeenAt.Value;
            var lockAfterSeconds = (user.IdleLockTimeoutMinutes.Value * 60) + WarningGraceSeconds;
            if (idleFor.TotalSeconds >= lockAfterSeconds)
            {
                user.IdleLockIsLocked = true;
                user.IdleLockLockedAt = DateTime.UtcNow;
                user.IdleLockAutoLogoutAt = user.IdleLockLockedAt.Value.AddMinutes(LockedAutoLogoutMinutes);
                try
                {
                    await _db.SaveChangesAsync(ct);
                }
                catch (TaskCanceledException)
                {
                    return Ok(new { ok = false, cancelled = true });
                }
            }
            else if (user.IdleLockAutoLogoutAt.HasValue)
            {
                user.IdleLockAutoLogoutAt = null;
                try
                {
                    await _db.SaveChangesAsync(ct);
                }
                catch (TaskCanceledException)
                {
                    return Ok(new { ok = false, cancelled = true });
                }
            }
        }

        return Ok(new
        {
            ok = true,
            enabled = true,
            locked = user.IdleLockIsLocked,
            role = user.Role.ToString(),
            timeoutMinutes = user.IdleLockTimeoutMinutes,
            maxTimeoutMinutes = MaxAllowedMinutes(user.Role),
            pinSet = !string.IsNullOrWhiteSpace(user.IdleLockPinHash),
            autoLogoutAt = user.IdleLockIsLocked ? user.IdleLockAutoLogoutAt : null
        });
    }

    [HttpPost("activity")]
    public async Task<IActionResult> TrackActivity([FromBody] IdleActivityRequest? req, CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new { ok = false, message = "Unauthorized" });

        User? user;
        try
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value, ct);
        }
        catch (TaskCanceledException)
        {
            return Ok(new { ok = false, cancelled = true });
        }
        if (user == null) return Unauthorized(new { ok = false, message = "User not found" });

        await EnsureIdlePolicyAsync(user, ct, ensurePin: false);

        if (!user.IdleLockIsLocked)
        {
            user.IdleLockLastSeenAt = DateTime.UtcNow;
            user.IdleLockAutoLogoutAt = null;
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (TaskCanceledException)
            {
                return Ok(new { ok = true, enabled = true, locked = user.IdleLockIsLocked });
            }
        }

        return Ok(new { ok = true, enabled = true, locked = user.IdleLockIsLocked });
    }

    [HttpPost("lock")]
    public async Task<IActionResult> ForceLock(CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new { ok = false, message = "Unauthorized" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value, ct);
        if (user == null) return Unauthorized(new { ok = false, message = "User not found" });

        await EnsureIdlePolicyAsync(user, ct, ensurePin: false);

        user.IdleLockIsLocked = true;
        user.IdleLockLockedAt = DateTime.UtcNow;
        user.IdleLockAutoLogoutAt = DateTime.UtcNow.AddMinutes(LockedAutoLogoutMinutes);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, locked = true });
    }

    [HttpPost("unlock")]
    public async Task<IActionResult> Unlock([FromBody] IdleUnlockRequest? req, CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new { ok = false, message = "Unauthorized" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value, ct);
        if (user == null) return Unauthorized(new { ok = false, message = "User not found" });

        // Check if auto-logout already triggered
        if (!await TryContinueAfterIdleAutoLogoutAsync(user, ct))
            return Unauthorized(new { ok = false, message = "Session ended after prolonged lock. Please sign in again.", idleLockAutoLogout = true });

        await EnsureIdlePolicyAsync(user, ct, ensurePin: false);

        if (string.IsNullOrWhiteSpace(user.IdleLockPinHash))
            return BadRequest(new { ok = false, message = "Quick Unlock PIN not set. Use the email PIN to unlock.", pinSet = false });

        if (user.IdleLockPinLockoutUntil.HasValue && user.IdleLockPinLockoutUntil.Value > DateTime.UtcNow)
        {
            var waitSeconds = (int)Math.Ceiling((user.IdleLockPinLockoutUntil.Value - DateTime.UtcNow).TotalSeconds);
            return BadRequest(new { ok = false, message = "Too many attempts. Try again later.", retryAfterSeconds = waitSeconds });
        }

        var pin = NormalizeSixDigit((req?.Pin ?? "").Trim());
        if (pin.Length != 6 || !pin.All(char.IsDigit))
            return BadRequest(new { ok = false, message = "Enter a valid 6-digit PIN." });

        var now = DateTime.UtcNow;
        var hasTemp = _tempUnlockPins.TryGetValue(user.UserID, out var temp);
        if (hasTemp && temp.ExpiresAt <= now)
        {
            _tempUnlockPins.TryRemove(user.UserID, out _);
            hasTemp = false;
        }

        var tempMatches = hasTemp && VerifyPin(pin, temp.Hash);
        var baseMatches = VerifyPin(pin, user.IdleLockPinHash);

        if (hasTemp && !tempMatches)
            return BadRequest(new { ok = false, message = "Email PIN is invalid or expired. Request a new PIN." });

        if (!hasTemp && !baseMatches)
        {
            user.IdleLockPinFailedCount += 1;

            // After 3 failed attempts, trigger auto-logout
            if (user.IdleLockPinFailedCount >= PinFailLimit)
            {
                user.IdleLockPinLockoutUntil = DateTime.UtcNow.Add(PinLockoutDuration);
                user.IdleLockPinFailedCount = 0;
            }
            await _db.SaveChangesAsync(ct);

            return BadRequest(new { ok = false, message = "Invalid PIN." });
        }

        if (tempMatches)
            _tempUnlockPins.TryRemove(user.UserID, out _);

        // Success: clear lock state
        user.IdleLockIsLocked = false;
        user.IdleLockLockedAt = null;
        user.IdleLockLastSeenAt = DateTime.UtcNow;
        user.IdleLockPinFailedCount = 0;
        user.IdleLockPinLockoutUntil = null;
        user.IdleLockAutoLogoutAt = null;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, unlocked = true });
    }

    [HttpPost("request-pin-reset")]
    public async Task<IActionResult> RequestPinReset(CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new { ok = false, message = "Unauthorized" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value, ct);
        if (user == null) return Unauthorized(new { ok = false, message = "User not found" });

        if (_lastIdleVerifyEmailUtc.TryGetValue(userId.Value, out var lastVerify) && DateTime.UtcNow - lastVerify < PinResendCooldown)
        {
            var waitV = (int)Math.Ceiling((PinResendCooldown - (DateTime.UtcNow - lastVerify)).TotalSeconds);
            return BadRequest(new { ok = false, message = $"Please wait {waitV} seconds before requesting another code.", retryAfterSeconds = waitV });
        }

        var email = user.Email;
        var code = GeneratePin();
        var codeHash = Sha256Base64(code);
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        _emailVerificationCodes.AddOrUpdate(email, (codeHash, expiresAt), (_, _) => (codeHash, expiresAt));

        var html = EmailTemplates.GetIdleLockPinChangeCodeHtml(code, email);
        const string subject = "Confirm your Quick Unlock PIN change - Subchron";
        try
        {
            await _email.SendAsync(email, subject, html);
        }
        catch (Exception)
        {
            _emailVerificationCodes.TryRemove(email, out _);
            return StatusCode(503, new { ok = false, message = "Could not send email. Check SMTP settings in the API configuration." });
        }

        _lastIdleVerifyEmailUtc[userId.Value] = DateTime.UtcNow;
        return Ok(new { ok = true, emailed = true });
    }

    /// <summary>Emails a freshly generated Quick Unlock PIN (invalidates the previous PIN).</summary>
    [HttpPost("resend-unlock-pin")]
    public async Task<IActionResult> ResendUnlockPin(CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new { ok = false, message = "Unauthorized" });

        if (_lastPinResendUtc.TryGetValue(userId.Value, out var last) && DateTime.UtcNow - last < PinResendCooldown)
        {
            var wait = (int)Math.Ceiling((PinResendCooldown - (DateTime.UtcNow - last)).TotalSeconds);
            return BadRequest(new { ok = false, message = $"Please wait {wait} seconds before requesting another PIN email.", retryAfterSeconds = wait });
        }

        User? user;
        try
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value, ct);
        }
        catch (TaskCanceledException)
        {
            return Ok(new { ok = false, cancelled = true });
        }
        if (user == null) return Unauthorized(new { ok = false, message = "User not found" });

        try
        {
            await EnsureIdlePolicyAsync(user, ct, ensurePin: false);
        }
        catch (TaskCanceledException)
        {
            return Ok(new { ok = false, cancelled = true });
        }

        var pin = GeneratePin();
        _tempUnlockPins[userId.Value] = (HashPin(pin), DateTime.UtcNow.Add(TempPinLifetime));
        user.IdleLockPinFailedCount = 0;
        user.IdleLockPinLockoutUntil = null;
        user.IdleLockAutoLogoutAt = null;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (TaskCanceledException)
        {
            return Ok(new { ok = false, cancelled = true });
        }

        _lastPinResendUtc[userId.Value] = DateTime.UtcNow;

        try
        {
            var html = EmailTemplates.GetQuickUnlockPinDeliveryHtml(pin, user.Email);
            await _email.SendAsync(user.Email, "Your Subchron Quick Unlock PIN", html);
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new { ok = false, message = "Email request timed out. Try again." });
        }
        catch (Exception)
        {
            return StatusCode(503, new { ok = false, message = "Could not send email. Check SMTP settings in the API configuration." });
        }

        return Ok(new { ok = true, emailed = true });
    }

    private static string NormalizeSixDigit(string raw)
        => new string((raw ?? "").Where(char.IsDigit).ToArray());

    [HttpPost("verify-pin-reset")]
    public async Task<IActionResult> VerifyPinReset([FromBody] VerifyPinResetRequest? req, CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new { ok = false, message = "Unauthorized" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value, ct);
        if (user == null) return Unauthorized(new { ok = false, message = "User not found" });

        var code = NormalizeSixDigit((req?.Code ?? "").Trim());
        var newPin = NormalizeSixDigit((req?.NewPin ?? "").Trim());
        if (code.Length != 6 || !code.All(char.IsDigit))
            return BadRequest(new { ok = false, message = "Enter the 6-digit verification code from your email (numbers only)." });
        if (newPin.Length != 6 || !newPin.All(char.IsDigit))
            return BadRequest(new { ok = false, message = "Enter a valid 6-digit PIN (numbers only)." });

        if (!_emailVerificationCodes.TryGetValue(user.Email, out var stored))
            return BadRequest(new { ok = false, message = "Invalid or expired code." });
        if (stored.ExpiresAt < DateTime.UtcNow)
        {
            _emailVerificationCodes.TryRemove(user.Email, out _);
            return BadRequest(new { ok = false, message = "Invalid or expired code." });
        }
        if (Sha256Base64(code) != stored.CodeHash)
            return BadRequest(new { ok = false, message = "Invalid or expired code." });

        _emailVerificationCodes.TryRemove(user.Email, out _);

        user.IdleLockPinHash = HashPin(newPin);
        user.IdleLockPinSetAt = DateTime.UtcNow;
        user.IdleLockPinFailedCount = 0;
        user.IdleLockPinLockoutUntil = null;
        user.IdleLockAutoLogoutAt = null;
        user.IdleLockIsLocked = false;
        user.IdleLockLockedAt = null;
        user.IdleLockLastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, unlocked = true });
    }

    [HttpPost("update-timeout")]
    public async Task<IActionResult> UpdateTimeout([FromBody] IdleTimeoutUpdateRequest? req, CancellationToken ct)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new { ok = false, message = "Unauthorized" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value, ct);
        if (user == null) return Unauthorized(new { ok = false, message = "User not found" });

        await EnsureIdlePolicyAsync(user, ct, ensurePin: false);

        var requested = req?.TimeoutMinutes ?? DefaultTimeoutMinutes;
        var clamped = ClampTimeout(requested, user.Role);

        user.IdleLockTimeoutMinutes = clamped;
        user.IdleLockEnabled = true;
        user.IdleLockLastSeenAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, timeoutMinutes = clamped, maxTimeoutMinutes = MaxAllowedMinutes(user.Role), role = SafeRoleLabel(user.Role) });
    }

    private async Task EnsureIdlePolicyAsync(User user, CancellationToken ct, bool ensurePin)
    {
        var max = MaxAllowedMinutes(user.Role);
        if (!user.IdleLockTimeoutMinutes.HasValue || user.IdleLockTimeoutMinutes.Value < MinTimeoutMinutes)
            user.IdleLockTimeoutMinutes = Math.Min(DefaultTimeoutMinutes, max);
        if (user.IdleLockTimeoutMinutes.Value > max)
            user.IdleLockTimeoutMinutes = max;

        user.IdleLockEnabled = true;
        user.IdleLockLastSeenAt ??= DateTime.UtcNow;

        if (ensurePin && string.IsNullOrWhiteSpace(user.IdleLockPinHash))
        {
            var pin = GeneratePin();
            user.IdleLockPinHash = HashPin(pin);
            user.IdleLockPinSetAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            try
            {
                var html = EmailTemplates.GetQuickUnlockPinDeliveryHtml(pin, user.Email);
                await _email.SendAsync(user.Email, "Your Subchron Quick Unlock PIN", html);
            }
            catch
            {
                // PIN is set; user can use resend or change-PIN flow if email failed
            }
            return;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static readonly TimeSpan PinResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly ConcurrentDictionary<int, DateTime> _lastPinResendUtc = new();
    private static readonly ConcurrentDictionary<int, DateTime> _lastIdleVerifyEmailUtc = new();
    private static readonly ConcurrentDictionary<int, (string Hash, DateTime ExpiresAt)> _tempUnlockPins = new();

    // In-memory store for email verification
    private static readonly ConcurrentDictionary<string, (string CodeHash, DateTime ExpiresAt)> _emailVerificationCodes = new();

    private static string Sha256Base64(string input)
    {
        using var sha = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public sealed class IdleActivityRequest
    {
        public string? EventType { get; set; }
    }

    public sealed class IdleUnlockRequest
    {
        public string? Pin { get; set; }
    }

    public sealed class IdleTimeoutUpdateRequest
    {
        public int TimeoutMinutes { get; set; }
    }

    public sealed class VerifyPinResetRequest
    {
        public string? Code { get; set; }
        public string? NewPin { get; set; }
    }
}
