using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Subchron.API.Data;
using Subchron.API.Models.Auth;
using Subchron.API.Models.Entities;
using Subchron.API.Models.Settings;
using Subchron.API.Services;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using OtpNet;
using QRCoder;
using System.Text.Json;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly RecaptchaService _recaptcha;
    private readonly EmailService _email;
    private readonly JwtTokenService _jwt;
    private readonly IAuditService _audit;
    private readonly PayMongoService? _payMongo;
    private readonly ILogger<AuthController> _logger;
    private readonly ICloudinaryService _cloudinary;
    private readonly IOptions<CloudinarySettings> _cloudinaryOpts;
    private readonly IConfiguration _config;

    // Track failed login attempts per IP
    private static readonly ConcurrentDictionary<string, (int Count, DateTime LastAttempt)> _loginAttempts = new();

    // Track forgot password attempts per IP
    private static readonly ConcurrentDictionary<string, (int Count, DateTime LastAttempt)> _forgotPasswordAttempts = new();

    private const int MaxAttemptsBeforeCaptcha = 3;
    private static readonly TimeSpan LoginAttemptWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ForgotPasswordAttemptWindow = TimeSpan.FromMinutes(5);

    public AuthController(
        SubchronDbContext db,
        RecaptchaService recaptcha,
        EmailService email,
        JwtTokenService jwt,
        IAuditService audit,
        ILogger<AuthController> logger,
        ICloudinaryService cloudinary,
        IOptions<CloudinarySettings> cloudinaryOpts,
        IConfiguration config,
        PayMongoService? payMongo = null)
    {
        _db = db;
        _recaptcha = recaptcha;
        _email = email;
        _jwt = jwt;
        _audit = audit;
        _logger = logger;
        _cloudinary = cloudinary;
        _cloudinaryOpts = cloudinaryOpts;
        _config = config;
        _payMongo = payMongo;
    }

    // Edit trial length here: 10080 = 7 days; use minutes for testing (e.g. 5).
    private int GetTrialDurationMinutes() => _config.GetValue("Trial:DurationMinutes", 10080);

    // Tells the client whether login should show reCAPTCHA
    [HttpGet("captcha-required")]
    public IActionResult IsCaptchaRequired()
    {
        var ip = GetClientIp();
        var required = IsLoginCaptchaRequired(ip);
        return Ok(new { required });
    }

    // Tells the client whether forgot-password should show reCAPTCHA
    [HttpGet("forgot-password-captcha-required")]
    public IActionResult IsForgotPasswordCaptchaRequired()
    {
        var ip = GetClientIp();
        var required = IsForgotPasswordCaptchaRequiredForIp(ip);
        return Ok(new { required });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest? req)
    {
        if (req is null)
            return BadRequest(new LoginResponse { Ok = false, Message = "Invalid request body.", RequiresCaptcha = false });

        try
        {
            var ip = GetClientIp();
            var captchaRequired = IsLoginCaptchaRequired(ip);

        // If captcha is required, validate it first
        if (captchaRequired)
        {
            if (string.IsNullOrWhiteSpace(req.RecaptchaToken))
            {
                return BadRequest(new LoginResponse
                {
                    Ok = false,
                    Message = "Please complete the CAPTCHA verification.",
                    RequiresCaptcha = true
                });
            }

            bool captchaValid;
            try
            {
                captchaValid = await _recaptcha.VerifyAsync(req.RecaptchaToken);
            }
            catch
            {
                return BadRequest(new LoginResponse
                {
                    Ok = false,
                    Message = "CAPTCHA verification failed. Please try again.",
                    RequiresCaptcha = true
                });
            }

            if (!captchaValid)
            {
                return BadRequest(new LoginResponse
                {
                    Ok = false,
                    Message = "CAPTCHA verification failed. Please try again.",
                    RequiresCaptcha = true
                });
            }
        }

        // Normalize email once
        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new LoginResponse
            {
                Ok = false,
                Message = "Email is required.",
                RequiresCaptcha = captchaRequired
            });
        }

        // Fetch user 
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Email != null)
            .FirstOrDefaultAsync(u => u.Email!.ToLower() == email);

        // Fast-fail: user not found, inactive, or no password (external-only account(OAuth))
        if (user is null || !user.IsActive)
        {
            RecordLoginAttempt(ip);
            await _audit.LogAsync(null, null, "LoginFailed", "User", null, "Invalid email or inactive: " + (email.Length > 80 ? email[..80] : email));
            var nowRequiresCaptcha = IsLoginCaptchaRequired(ip);

            return Unauthorized(new LoginResponse
            {
                Ok = false,
                Message = "Invalid email or password.",
                RequiresCaptcha = nowRequiresCaptcha
            });
        }

        if (string.IsNullOrEmpty(user.Password))
        {
            RecordLoginAttempt(ip);
            await _audit.LogAsync(user.OrgID, user.UserID, "LoginFailed", "User", user.UserID, "No password (external account)");
            return Unauthorized(new LoginResponse
            {
                Ok = false,
                Message = "Invalid email or password.",
                RequiresCaptcha = IsLoginCaptchaRequired(ip)
            });
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(req.Password ?? "", user.Password))
        {
            RecordLoginAttempt(ip);
            await _audit.LogAsync(user.OrgID, user.UserID, "LoginFailed", "User", user.UserID, "Invalid password");
            var nowRequiresCaptcha = IsLoginCaptchaRequired(ip);

            return Unauthorized(new LoginResponse
            {
                Ok = false,
                Message = "Invalid email or password.",
                RequiresCaptcha = nowRequiresCaptcha
            });
        }

        // Success - clear failed attempts for this IP
        ClearLoginAttempts(ip);

        // TOTP required?
        if (user.TotpEnabled)
        {
            await _audit.LogAsync(user.OrgID, user.UserID, "Login", "User", user.UserID, "TOTP required");
            return Ok(new LoginResponse
            {
                Ok = true,
                RequiresTotp = true,
                Message = "TOTP required."
            });
        }

        // Update last login and audit
        _ = UpdateLastLoginAsync(user.UserID);
        await _audit.LogAsync(user.OrgID, user.UserID, "Login", "User", user.UserID, "Success");

        // Use linked Employee's Role for token/redirect
        var roleString = await GetEffectiveRoleForUserAsync(user.UserID) ?? user.Role.ToString();
        string? orgName = null;
        if (user.OrgID.HasValue)
            orgName = await _db.Organizations.AsNoTracking().Where(o => o.OrgID == user.OrgID.Value).Select(o => o.OrgName).FirstOrDefaultAsync();
        string token;
        try
        {
            token = _jwt.CreateToken(user, roleString);
        }
        catch (Exception jwtEx)
        {
            _logger.LogError(jwtEx, "JWT creation failed for user {UserId}", user.UserID);
            return StatusCode(500, new LoginResponse { Ok = false, Message = "An error occurred during sign-in. Please try again.", RequiresCaptcha = false });
        }

        return Ok(new LoginResponse
        {
            Ok = true,
            UserId = user.UserID,
            OrgId = user.OrgID,
            OrgName = orgName,
            Role = roleString,
            Name = user.Name ?? "",
            Token = token
        });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email {Email}", req?.Email ?? "(null)");
            return StatusCode(500, new LoginResponse { Ok = false, Message = "An error occurred during sign-in. Please try again.", RequiresCaptcha = false });
        }
    }

    // Record logout in audit log
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orgIdClaim = User.FindFirstValue("orgId");
        int? userId = null;
        int? orgId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var uid))
            userId = uid;
        if (!string.IsNullOrEmpty(orgIdClaim) && int.TryParse(orgIdClaim, out var oid))
            orgId = oid;
        await _audit.LogAsync(orgId, userId, "Logout", "User", userId, "Success");
        return Ok(new { ok = true });
    }

    // Create a login user for an employee 
    [Authorize]
    [HttpPost("create-employee-user")]
    public async Task<IActionResult> CreateEmployeeUser([FromBody] CreateEmployeeUserRequest? req)
    {
        var orgIdClaim = User.FindFirstValue("orgId");
        if (string.IsNullOrEmpty(orgIdClaim) || !int.TryParse(orgIdClaim, out var orgId))
            return Forbid();

        if (req is null || string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { ok = false, message = "Email is required." });
        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { ok = false, message = "Password is required." });

        var email = req.Email.Trim().ToLowerInvariant();
        var existing = await _db.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (existing)
            return Conflict(new { ok = false, message = "This email is already registered." });

        var name = string.IsNullOrWhiteSpace(req.Name) ? email : req.Name.Trim();
        if (name.Length > 255) name = name[..255];

        var user = new User
        {
            OrgID = orgId,
            Name = name,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(req.Password.Trim()),
            IsActive = true,
            Role = UserRoleType.Employee,
            EmailVerified = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, userId = user.UserID });
    }

    [Authorize]
    [HttpPost("profile/avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false, message = "Not authenticated." });

        if (file == null || file.Length == 0)
            return BadRequest(new { ok = false, message = "No file uploaded." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { ok = false, message = "File too large (max 5MB)." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { ok = false, message = "Only JPG/PNG/WEBP images are allowed." });

        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user is null)
            return Unauthorized(new { ok = false, message = "User not found." });

        var folder = _cloudinaryOpts.Value.Folder ?? "subchron/avatars";

        // IMPORTANT: stable PublicId per user so it overwrites
        var publicId = $"user_{userId}";

        string avatarUrl;
        await using (var stream = file.OpenReadStream())
        {
            avatarUrl = await _cloudinary.UploadAvatarOverwriteAsync(
                stream,
                file.FileName,
                folder,
                publicId,
                ct);
        }

        user.AvatarUrl = avatarUrl;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, avatarUrl });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest? req)
    {
        if (req is null)
            return BadRequest(new { ok = false, message = "Invalid request.", requiresCaptcha = false });

        var ip = GetClientIp();
        var captchaRequired = IsForgotPasswordCaptchaRequiredForIp(ip);

        // If captcha is required, validate it first
        if (captchaRequired)
        {
            if (string.IsNullOrWhiteSpace(req.RecaptchaToken))
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "Please complete the CAPTCHA verification.",
                    requiresCaptcha = true
                });
            }

            if (!await _recaptcha.VerifyAsync(req.RecaptchaToken))
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "CAPTCHA verification failed. Please try again.",
                    requiresCaptcha = true
                });
            }
        }

        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new
            {
                ok = false,
                message = "Email is required.",
                requiresCaptcha = captchaRequired
            });
        }

        // Record attempt 
        RecordForgotPasswordAttempt(ip);
        var nowRequiresCaptcha = IsForgotPasswordCaptchaRequiredForIp(ip);

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email);

        // return success to prevent account enumeration
        if (user is null)
            return Ok(new { ok = true, requiresCaptcha = nowRequiresCaptcha });

        // Generate token
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Sha256Base64(token);
        var expires = DateTime.UtcNow.AddMinutes(30);

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserID = user.UserID,
            TokenHash = tokenHash,
            ExpiresAt = expires
        });

        await _db.SaveChangesAsync();

        // Build reset URL and get web base URL for logo
        var webBaseUrl =
            Request.Headers["Origin"].FirstOrDefault()
            ?? Request.Headers["Referer"].FirstOrDefault()?.TrimEnd('/')
            ?? $"{Request.Scheme}://{Request.Host}";

        if (Uri.TryCreate(webBaseUrl, UriKind.Absolute, out var uri))
        {
            webBaseUrl = $"{uri.Scheme}://{uri.Host}" +
                         (uri.Port != 80 && uri.Port != 443 ? $":{uri.Port}" : "");
        }

        var resetUrl =
            $"{webBaseUrl}/Auth/ResetPassword?email={Uri.EscapeDataString(user.Email)}&code={Uri.EscapeDataString(token)}";

        var html = EmailTemplates.GetPasswordResetHtml(resetUrl, user.Email, logoUrl: null, webBaseUrl: webBaseUrl);
        try
        {
            await _email.SendAsync(user.Email, "Reset your Subchron password", html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        return Ok(new { ok = true, requiresCaptcha = nowRequiresCaptcha });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        // Reset password requires captcha
        if (!await _recaptcha.VerifyAsync(req.RecaptchaToken))
            return BadRequest(new { ok = false, message = "CAPTCHA verification failed." });

        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { ok = false, message = "Invalid request." });

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email);
        if (user is null)
            return BadRequest(new { ok = false, message = "Invalid request." });

        var tokenHash = Sha256Base64(req.Code);

        var prt = await _db.PasswordResetTokens
            .Where(x => x.UserID == user.UserID && x.TokenHash == tokenHash && x.UsedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (prt is null || prt.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { ok = false, message = "Reset code is invalid or expired." });

        user.Password = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        prt.UsedAt = DateTime.UtcNow;

        // Clear forgot password attempts on successful reset
        var ip = GetClientIp();
        ClearForgotPasswordAttempts(ip);

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // Change password for the authenticated user
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false, message = "Not authenticated." });

        var currentPassword = req?.CurrentPassword ?? "";
        var newPassword = req?.NewPassword ?? "";
        if (string.IsNullOrWhiteSpace(currentPassword))
            return BadRequest(new { ok = false, message = "Current password is required." });
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            return BadRequest(new { ok = false, message = "New password must be at least 8 characters." });

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return Unauthorized(new { ok = false, message = "User not found." });

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            return BadRequest(new { ok = false, message = "Current password is incorrect." });

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false, message = "Not authenticated." });

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.UserID == userId)
            .Select(u => new { u.Name, u.Email, u.AvatarUrl, u.ExternalProvider })
            .FirstOrDefaultAsync();
        if (user is null)
            return Unauthorized(new { ok = false, message = "User not found." });

        return Ok(new { name = user.Name, email = user.Email, avatarUrl = user.AvatarUrl, loginProvider = user.ExternalProvider });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false, message = "Not authenticated." });

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return Unauthorized(new { ok = false, message = "User not found." });

        if (req?.Name != null)
        {
            var name = req.Name.Trim();
            if (name.Length > 100) return BadRequest(new { ok = false, message = "Name is too long." });
            if (name.Length > 0) user.Name = name;
        }
        if (req != null)
        {
            if (string.IsNullOrWhiteSpace(req.AvatarUrl))
                user.AvatarUrl = null;
            else
            {
                var url = req.AvatarUrl!.Trim();
                if (url.Length > 500) return BadRequest(new { ok = false, message = "Avatar URL is too long." });
                user.AvatarUrl = url;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, name = user.Name, avatarUrl = user.AvatarUrl });
    }

    [Authorize]
    [HttpGet("employee-info")]
    public async Task<IActionResult> GetEmployeeInfo()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false, message = "Not authenticated." });

        var emp = await _db.Employees.AsNoTracking()
            .Where(e => e.UserID == userId)
            .Select(e => new
            {
                e.FirstName,
                e.MiddleName,
                e.LastName,
                e.EmpNumber,
                e.DepartmentID,
                e.EmploymentType,
                workState = e.WorkState,
                isArchived = e.IsArchived,
                e.DateHired,
                e.Phone,
                e.AddressLine1,
                e.AddressLine2,
                e.City,
                e.StateProvince,
                e.PostalCode,
                e.Country,
                e.EmergencyContactName,
                e.EmergencyContactPhone,
                e.EmergencyContactRelation,
                Role = e.User != null ? e.User.Role : (UserRoleType?)null
            })
            .FirstOrDefaultAsync();
        if (emp is null)
            return Ok(new { hasEmployee = false });

        return Ok(new
        {
            hasEmployee = true,
            firstName = emp.FirstName,
            middleName = emp.MiddleName ?? "",
            lastName = emp.LastName,
            empNumber = emp.EmpNumber ?? "",
            departmentID = emp.DepartmentID,
            employmentType = emp.EmploymentType ?? "Regular",
            workState = emp.workState ?? "Active",
            isArchived = emp.isArchived,
            dateHired = emp.DateHired,
            phone = emp.Phone ?? "",
            addressLine1 = emp.AddressLine1 ?? "",
            addressLine2 = emp.AddressLine2 ?? "",
            city = emp.City ?? "",
            stateProvince = emp.StateProvince ?? "",
            postalCode = emp.PostalCode ?? "",
            country = emp.Country ?? "",
            emergencyContactName = emp.EmergencyContactName ?? "",
            emergencyContactPhone = emp.EmergencyContactPhone ?? "",
            emergencyContactRelation = emp.EmergencyContactRelation ?? "",
            role = emp.Role?.ToString() ?? ""
        });
    }

    [Authorize]
    [HttpPut("employee-info")]
    public async Task<IActionResult> UpdateEmployeeInfo([FromBody] UpdateEmployeeInfoRequest req)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false, message = "Not authenticated." });

        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.UserID == userId);
        if (emp is null)
            return NotFound(new { ok = false, message = "No employee record linked to your account." });

        if (req?.FirstName != null) { 
            var s = req.FirstName.Trim(); if (s.Length > 50) return BadRequest(new 
            { 
                ok = false, message = "First name is too long." 
            }); 
            if (s.Length > 0) emp.FirstName = s; 
        }

        if (req?.MiddleName != null) { 
            var s = req.MiddleName.Trim(); emp.MiddleName = s.Length > 50 ? s[..50] : (s.Length > 0 ? s : null); 
        }

        if (req?.LastName != null) { 
            var s = req.LastName.Trim(); if (s.Length > 50) return BadRequest(new { 
                ok = false, message = "Last name is too long." 
            }); 

            if (s.Length > 0) emp.LastName = s; 
        }
        if (req?.DepartmentID.HasValue == true) emp.DepartmentID = req.DepartmentID;

        if (req?.EmploymentType != null) { 
            var s = req.EmploymentType.Trim(); if (s.Length > 50) s = s[..50]; if (s.Length > 0) emp.EmploymentType = s; 
        }

        if (req?.WorkState != null && !emp.IsArchived) { 
            var s = req.WorkState.Trim(); if (s.Length > 40) s = s[..40]; if (s.Length > 0) emp.WorkState = s; 
        }

        if (req?.DateHired.HasValue == true) emp.DateHired = req.DateHired;

        if (req?.Phone != null) { 
            var s = req.Phone.Trim(); emp.Phone = s.Length > 30 ? s[..30] : (s.Length > 0 ? s : null); 
        }

        if (req?.AddressLine1 != null) { 
            var s = req.AddressLine1.Trim(); emp.AddressLine1 = s.Length > 200 ? s[..200] : (s.Length > 0 ? s : null); 
        }

        if (req?.AddressLine2 != null) { 
            var s = req.AddressLine2.Trim(); emp.AddressLine2 = s.Length > 200 ? s[..200] : (s.Length > 0 ? s : null); 
        }

        if (req?.City != null) { 
            var s = req.City.Trim(); emp.City = s.Length > 80 ? s[..80] : (s.Length > 0 ? s : null); 
        }

        if (req?.StateProvince != null) { 
            var s = req.StateProvince.Trim(); emp.StateProvince = s.Length > 80 ? s[..80] : (s.Length > 0 ? s : null); 
        }

        if (req?.PostalCode != null) { 
            var s = req.PostalCode.Trim(); emp.PostalCode = s.Length > 20 ? s[..20] : (s.Length > 0 ? s : null); 
        }

        if (req?.Country != null) { 
            var s = req.Country.Trim(); if (s.Length > 0) emp.Country = s.Length > 80 ? s[..80] : s; 
        }

        if (req?.EmergencyContactName != null) { 
            var s = req.EmergencyContactName.Trim(); 
            emp.EmergencyContactName = s.Length > 100 ? s[..100] : (s.Length > 0 ? s : null); 
        }

        if (req?.EmergencyContactPhone != null) { 
            var s = req.EmergencyContactPhone.Trim(); 
            emp.EmergencyContactPhone = s.Length > 30 ? s[..30] : (s.Length > 0 ? s : null); 
        }

        if (req?.EmergencyContactRelation != null) { 
            var s = req.EmergencyContactRelation.Trim(); 
            emp.EmergencyContactRelation = s.Length > 50 ? s[..50] : (s.Length > 0 ? s : null); 
        }

        // Keep User.Name in sync with employee full name so profile/header display name updates
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            var parts = new[] { emp.FirstName?.Trim(), emp.MiddleName?.Trim(), emp.LastName?.Trim() }
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (parts.Count > 0)
                user.Name = string.Join(" ", parts);
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // In-memory store for email verification
    private static readonly ConcurrentDictionary<string, (string CodeHash, DateTime ExpiresAt)> _emailVerificationCodes = new();

    // Sends a 6-digit code to the email for signup
    [HttpPost("send-verification-code")]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendVerificationCodeRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { ok = false, message = "Email is required." });

        if (DisposableEmailChecker.IsDisposable(email))
            return BadRequest(new { ok = false, message = "Disposable email addresses are not allowed. Please use a work or personal email." });

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
            return Conflict(new { ok = false, message = "This email is already registered." });

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = Sha256Base64(code);
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        _emailVerificationCodes.AddOrUpdate(email, (codeHash, expiresAt), (_, _) => (codeHash, expiresAt));

        var html = EmailTemplates.GetVerificationCodeHtml(code, email);
        var subject = "Verify your email - Subchron";
        _ = Task.Run(async () =>
        {
            try { await _email.SendAsync(email, subject, html); }
            catch { /* log in production */ }
        });

        return Ok(new { ok = true });
    }

    // Verifies the 6-digit code from email
    [HttpPost("verify-email-code")]
    public IActionResult VerifyEmailCode([FromBody] VerifyEmailCodeRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var code = (req.Code ?? "").Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            return BadRequest(new { ok = false, message = "Email and code are required." });

        if (!_emailVerificationCodes.TryGetValue(email, out var stored))
            return BadRequest(new { ok = false, message = "Invalid or expired code. Please request a new one." });

        if (stored.ExpiresAt < DateTime.UtcNow)
        {
            _emailVerificationCodes.TryRemove(email, out _);
            return BadRequest(new { ok = false, message = "Invalid or expired code. Please request a new one." });
        }

        var codeHash = Sha256Base64(code);
        if (codeHash != stored.CodeHash)
            return BadRequest(new { ok = false, message = "Invalid or expired code. Please request a new one." });

        _emailVerificationCodes.TryRemove(email, out _);
        return Ok(new { ok = true });
    }

    [HttpPost("external-login")]
    public async Task<ActionResult<ExternalLoginResponse>> ExternalLogin([FromBody] ExternalLoginRequest? req)
    {
        if (req is null)
            return BadRequest(new ExternalLoginResponse { Ok = false, Message = "Invalid request body." });

        var provider = (req.Provider ?? "").Trim();
        var externalId = (req.ExternalId ?? "").Trim();
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var name = (req.Name ?? "").Trim();

        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalId))
            return BadRequest(new ExternalLoginResponse { Ok = false, Message = "Provider/external id required." });

        // Normalize provider so lookup and store are consistent 
        if (string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
            provider = "Google";

        // Find by provider + external id
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.ExternalProvider == provider && u.ExternalId == externalId);

        // If not found, link existing account by email
        if (user == null && !string.IsNullOrWhiteSpace(email))
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user != null)
            {
                user.ExternalProvider = provider;
                user.ExternalId = externalId;
                user.EmailVerified = true;
                await _db.SaveChangesAsync();
            }
        }

        if (user == null)
        {
            return Ok(new ExternalLoginResponse
            {
                Ok = false,
                RequiresSignup = true,
                Email = email,
                Name = name
            });
        }

        if (!user.IsActive)
        {
            return Ok(new ExternalLoginResponse
            {
                Ok = false,
                Message = "This account has been deactivated. Contact your administrator."
            });
        }

        await _audit.LogAsync(user.OrgID, user.UserID, "Login", "User", user.UserID, provider);

        // If 2FA is enabled, require TOTP before issuing session token
        if (user.TotpEnabled)
        {
            var intentToken = _jwt.CreateTotpIntentToken(user.UserID);
            return Ok(new ExternalLoginResponse
            {
                Ok = true,
                RequiresTotp = true,
                TotpIntentToken = intentToken,
                Email = user.Email,
                Name = user.Name
            });
        }

        // Use linked employee's role when present so they can redirect to backoffice.
        var roleString = await GetEffectiveRoleForUserAsync(user.UserID) ?? user.Role.ToString();
        string? orgName = null;
        if (user.OrgID.HasValue)
            orgName = await _db.Organizations.AsNoTracking().Where(o => o.OrgID == user.OrgID.Value).Select(o => o.OrgName).FirstOrDefaultAsync();
        var token = _jwt.CreateToken(user, roleString);

        return Ok(new ExternalLoginResponse
        {
            Ok = true,
            UserId = user.UserID,
            OrgId = user.OrgID,
            OrgName = orgName,
            Role = roleString,
            Name = user.Name,
            Token = token
        });
    }

    [HttpPost("verify-external-totp")]
    public async Task<IActionResult> VerifyExternalTotp([FromBody] Subchron.API.Models.Auth.VerifyExternalTotpRequest? req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.TotpIntentToken))
            return BadRequest(new { ok = false, message = "Invalid request." });
        var code = (req.TotpCode ?? "").Trim();
        if (code.Length != 6 || !code.All(char.IsDigit))
            return BadRequest(new { ok = false, message = "Enter a valid 6-digit code." });

        var userId = _jwt.ValidateTotpIntentToken(req.TotpIntentToken);
        if (!userId.HasValue)
            return BadRequest(new { ok = false, message = "Session expired. Please sign in again." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value);
        if (user is null || !user.IsActive)
            return Unauthorized(new { ok = false, message = "Invalid session." });
        if (!user.TotpEnabled || user.TotpSecret is null)
            return BadRequest(new { ok = false, message = "2FA is not enabled." });

        var totp = new Totp(user.TotpSecret, step: 30, totpSize: 6);
        if (!totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1)))
            return BadRequest(new { ok = false, message = "Invalid authentication code." });

        _ = UpdateLastLoginAsync(user.UserID);
        var roleString = await GetEffectiveRoleForUserAsync(user.UserID) ?? user.Role.ToString();
        string? orgName = null;
        if (user.OrgID.HasValue)
            orgName = await _db.Organizations.AsNoTracking().Where(o => o.OrgID == user.OrgID.Value).Select(o => o.OrgName).FirstOrDefaultAsync();
        var token = _jwt.CreateToken(user, roleString);

        return Ok(new
        {
            ok = true,
            userId = user.UserID,
            orgId = user.OrgID,
            orgName = orgName,
            role = roleString,
            name = user.Name ?? "",
            token
        });
    }


    // ========== 2FA Related Endpoints ==========


    [Authorize]
    [HttpGet("totp/status")]
    public async Task<IActionResult> TotpStatus()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false });

        var enabled = await _db.Users.AsNoTracking()
            .Where(u => u.UserID == userId)
            .Select(u => u.TotpEnabled)
            .FirstOrDefaultAsync();

        return Ok(new { ok = true, enabled });
    }

    [Authorize]
    [HttpPost("totp/begin")]
    public async Task<IActionResult> BeginTotp()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false, message = "Not authenticated." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null) return Unauthorized(new { ok = false, message = "User not found." });

        if (user.TotpEnabled)
            return BadRequest(new { ok = false, message = "2FA is already enabled." });

        // create and store secret (setup in progress)
        var secret = NewSecretBytes();
        user.TotpSecret = secret;
        user.RecoveryCodesHash = null;
        await _db.SaveChangesAsync();

        var secretBase32 = ToBase32(secret);
        var issuer = "Subchron";
        var account = user.Email;
        var uri = BuildOtpAuthUri(issuer, account, secretBase32);
        var qr = QrPngDataUrl(uri);

        return Ok(new
        {
            ok = true,
            manualKey = secretBase32,
            otpAuthUri = uri,
            qrDataUrl = qr
        });
    }

    [Authorize]
    [HttpPost("totp/verify-enable")]
    public async Task<IActionResult> VerifyEnableTotp([FromBody] Subchron.API.Models.Auth.VerifyEnableTotpRequest req)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false, message = "Not authenticated." });

        var code = (req?.TotpCode ?? "").Trim();
        if (code.Length != 6 || !code.All(char.IsDigit))
            return BadRequest(new { ok = false, message = "Enter a valid 6-digit code." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null) return Unauthorized(new { ok = false, message = "User not found." });

        if (user.TotpSecret is null || user.TotpSecret.Length == 0)
            return BadRequest(new { ok = false, message = "No setup in progress. Start again." });

        if (user.TotpEnabled)
            return Ok(new { ok = true, message = "2FA already enabled." });

        var totp = new Totp(user.TotpSecret, step: 30, totpSize: 6);
        var ok = totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        if (!ok)
            return BadRequest(new { ok = false, message = "Invalid code. Try again." });

        user.TotpEnabled = true;

        // generate recovery codes (return once)
        var recoveryPlain = GenerateRecoveryCodes(10);
        var items = recoveryPlain
            .Select(rc => new RcItem(HashRecoveryCode(user.UserID, rc), false))
            .ToList();
        user.RecoveryCodesHash = WriteRecoveryList(items);

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, recoveryCodes = recoveryPlain });
    }

    [Authorize]
    [HttpPost("totp/disable")]
    public async Task<IActionResult> DisableTotp()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { ok = false });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null) return Unauthorized(new { ok = false });

        user.TotpEnabled = false;
        user.TotpSecret = null;
        user.RecoveryCodesHash = null;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("verify-totp")]
    public async Task<IActionResult> VerifyTotp([FromBody] Subchron.API.Models.Auth.VerifyTotpLoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var password = req.Password ?? "";
        var code = (req.TotpCode ?? "").Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { ok = false, message = "Invalid request." });

        if (code.Length != 6 || !code.All(char.IsDigit))
            return BadRequest(new { ok = false, message = "Enter a valid 6-digit code." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null || !user.IsActive)
            return Unauthorized(new { ok = false, message = "Invalid email or password." });

        if (string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            return Unauthorized(new { ok = false, message = "Invalid email or password." });

        if (!user.TotpEnabled || user.TotpSecret is null)
            return BadRequest(new { ok = false, message = "2FA is not enabled." });

        var totp = new Totp(user.TotpSecret, step: 30, totpSize: 6);
        var ok = totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        if (!ok)
            return BadRequest(new { ok = false, message = "Invalid authentication code." });

        _ = UpdateLastLoginAsync(user.UserID);

        var roleString = await GetEffectiveRoleForUserAsync(user.UserID) ?? user.Role.ToString();
        string? orgName = null;
        if (user.OrgID.HasValue)
            orgName = await _db.Organizations.AsNoTracking()
                .Where(o => o.OrgID == user.OrgID.Value)
                .Select(o => o.OrgName)
                .FirstOrDefaultAsync();

        var token = _jwt.CreateToken(user, roleString);

        return Ok(new
        {
            ok = true,
            userId = user.UserID,
            orgId = user.OrgID,
            orgName,
            role = roleString,
            name = user.Name ?? "",
            token
        });
    }

    [HttpPost("verify-recovery")]
    public async Task<IActionResult> VerifyRecovery([FromBody] Subchron.API.Models.Auth.VerifyRecoveryLoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var password = req.Password ?? "";
        var recovery = (req.RecoveryCode ?? "").Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { ok = false, message = "Invalid request." });

        if (string.IsNullOrWhiteSpace(recovery))
            return BadRequest(new { ok = false, message = "Recovery code is required." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null || !user.IsActive)
            return Unauthorized(new { ok = false, message = "Invalid email or password." });

        if (string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            return Unauthorized(new { ok = false, message = "Invalid email or password." });

        if (!user.TotpEnabled)
            return BadRequest(new { ok = false, message = "2FA is not enabled." });

        var list = ReadRecoveryList(user.RecoveryCodesHash);
        if (list.Count == 0)
            return BadRequest(new { ok = false, message = "No recovery codes available. Contact support." });

        var targetHash = HashRecoveryCode(user.UserID, recovery);
        var idx = list.FindIndex(x => !x.u && x.h == targetHash);
        if (idx < 0)
            return BadRequest(new { ok = false, message = "Invalid or already used recovery code." });

        list[idx] = new RcItem(list[idx].h, true);
        user.RecoveryCodesHash = WriteRecoveryList(list);
        await _db.SaveChangesAsync();

        _ = UpdateLastLoginAsync(user.UserID);

        var roleString = await GetEffectiveRoleForUserAsync(user.UserID) ?? user.Role.ToString();
        string? orgName = null;
        if (user.OrgID.HasValue)
            orgName = await _db.Organizations.AsNoTracking()
                .Where(o => o.OrgID == user.OrgID.Value)
                .Select(o => o.OrgName)
                .FirstOrDefaultAsync();

        var token = _jwt.CreateToken(user, roleString);

        return Ok(new
        {
            ok = true,
            userId = user.UserID,
            orgId = user.OrgID,
            orgName,
            role = roleString,
            name = user.Name ?? "",
            token
        });
    }

    [HttpPost("verify-external-recovery")]
    public async Task<IActionResult> VerifyExternalRecovery([FromBody] Subchron.API.Models.Auth.VerifyExternalRecoveryRequest? req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.TotpIntentToken))
            return BadRequest(new { ok = false, message = "Invalid request." });
        var recovery = (req.RecoveryCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(recovery))
            return BadRequest(new { ok = false, message = "Recovery code is required." });

        var userId = _jwt.ValidateTotpIntentToken(req.TotpIntentToken);
        if (!userId.HasValue)
            return BadRequest(new { ok = false, message = "Session expired. Please sign in again." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value);
        if (user is null || !user.IsActive)
            return Unauthorized(new { ok = false, message = "Invalid session." });
        if (!user.TotpEnabled)
            return BadRequest(new { ok = false, message = "2FA is not enabled." });

        var list = ReadRecoveryList(user.RecoveryCodesHash);
        if (list.Count == 0)
            return BadRequest(new { ok = false, message = "No recovery codes available. Contact support." });

        var targetHash = HashRecoveryCode(user.UserID, recovery);
        var idx = list.FindIndex(x => !x.u && x.h == targetHash);
        if (idx < 0)
            return BadRequest(new { ok = false, message = "Invalid or already used recovery code." });

        list[idx] = new RcItem(list[idx].h, true);
        user.RecoveryCodesHash = WriteRecoveryList(list);
        await _db.SaveChangesAsync();

        _ = UpdateLastLoginAsync(user.UserID);
        var roleString = await GetEffectiveRoleForUserAsync(user.UserID) ?? user.Role.ToString();
        string? orgName = null;
        if (user.OrgID.HasValue)
            orgName = await _db.Organizations.AsNoTracking().Where(o => o.OrgID == user.OrgID.Value).Select(o => o.OrgName).FirstOrDefaultAsync();
        var token = _jwt.CreateToken(user, roleString);

        return Ok(new
        {
            ok = true,
            userId = user.UserID,
            orgId = user.OrgID,
            orgName = orgName,
            role = roleString,
            name = user.Name ?? "",
            token
        });
    }

    // Saves signup as draft and returns a token for the billing page
    [HttpPost("signup-draft")]
    public async Task<IActionResult> SignupDraft([FromBody] SignupRequest req)
    {
        var isExternal =
            !string.IsNullOrWhiteSpace(req.ExternalProvider) &&
            !string.IsNullOrWhiteSpace(req.ExternalId);

        if (!isExternal)
        {
            if (string.IsNullOrWhiteSpace(req.RecaptchaToken) || !await _recaptcha.VerifyAsync(req.RecaptchaToken))
                return BadRequest(new { ok = false, message = "CAPTCHA verification failed." });
        }

        var orgName = (req.OrgName ?? "").Trim();
        var orgCode = (req.OrgCode ?? "").Trim().ToUpperInvariant();
        var email = (req.AdminEmail ?? "").Trim().ToLowerInvariant();
        var adminName = (req.AdminName ?? "").Trim();
        var mode = (req.AttendanceMode ?? "QR").Trim();
        var billing = (req.BillingCycle ?? "Monthly").Trim();

        if (string.IsNullOrWhiteSpace(orgName) || string.IsNullOrWhiteSpace(orgCode))
            return BadRequest(new { ok = false, message = "Organization name/code required." });
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { ok = false, message = "Admin email is required." });
        if (!isExternal && string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { ok = false, message = "Admin email/password required." });

        if (await _db.Organizations.AnyAsync(o => o.OrgCode == orgCode))
            return Conflict(new { ok = false, message = "Organization code already exists." });
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
            return Conflict(new { ok = false, message = "Email already registered." });

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanID == req.PlanId && p.IsActive);
        if (plan is null)
            return BadRequest(new { ok = false, message = "Invalid plan." });

        var draft = new SignupDraftData
        {
            OrgName = orgName,
            OrgCode = orgCode,
            PlanId = req.PlanId,
            AttendanceMode = mode,
            BillingCycle = billing,
            AdminName = string.IsNullOrWhiteSpace(adminName) ? email : adminName,
            AdminEmail = email,
            Password = req.Password ?? "",
            ExternalProvider = isExternal ? req.ExternalProvider : null,
            ExternalId = isExternal ? req.ExternalId : null
        };
        var draftToken = SignupDraftStore.Save(draft);
        return Ok(new { ok = true, draftToken, requiresBilling = true });
    }

    [HttpPost("signup")]
    public async Task<ActionResult<SignupResponse>> Signup([FromBody] SignupRequest req)
    {
        var isExternal =
            !string.IsNullOrWhiteSpace(req.ExternalProvider) &&
            !string.IsNullOrWhiteSpace(req.ExternalId);

        // Only require captcha for email/password signup
        if (!isExternal)
        {
            if (string.IsNullOrWhiteSpace(req.RecaptchaToken) || !await _recaptcha.VerifyAsync(req.RecaptchaToken))
                return BadRequest(new SignupResponse { Ok = false, Message = "CAPTCHA verification failed." });
        }

        // normalize inputs
        var orgName = (req.OrgName ?? "").Trim();
        var orgCode = (req.OrgCode ?? "").Trim().ToUpperInvariant();
        var email = (req.AdminEmail ?? "").Trim().ToLowerInvariant();
        var adminName = (req.AdminName ?? "").Trim();
        var mode = (req.AttendanceMode ?? "QR").Trim();
        var billing = (req.BillingCycle ?? "Monthly").Trim();

        // required checks
        if (string.IsNullOrWhiteSpace(orgName) || string.IsNullOrWhiteSpace(orgCode))
            return BadRequest(new SignupResponse { Ok = false, Message = "Organization name/code required." });

        // email is required for both flows because Users
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new SignupResponse { Ok = false, Message = "Admin email is required." });

        // password required only for non-external flow
        if (!isExternal && string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new SignupResponse { Ok = false, Message = "Admin email/password required." });

        // validate uniqueness
        if (await _db.Organizations.AnyAsync(o => o.OrgCode == orgCode))
            return Conflict(new SignupResponse { Ok = false, Message = "Organization code already exists." });

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
            return Conflict(new SignupResponse { Ok = false, Message = "Email already registered." });

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanID == req.PlanId && p.IsActive);
        if (plan is null)
            return BadRequest(new SignupResponse { Ok = false, Message = "Invalid plan." });

        // Create org/user immediately 
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // CREATE ORGANIZATION (need OrgID)
                var org = new Organization
                {
                    OrgName = orgName,
                    OrgCode = orgCode,
                    Status = "Trial"
                };

                _db.Organizations.Add(org);
                await _db.SaveChangesAsync(); // org.OrgID

                // SETTINGS + SUBSCRIPTION
                _db.OrganizationSettings.Add(new OrganizationSettings
                {
                    OrgID = org.OrgID,
                    Timezone = "Asia/Manila",
                    Currency = "PHP",
                    AttendanceMode = mode,
                    RoundRule = "None"
                });

                var startDate = DateTime.UtcNow.Date;
                var isTrialPlan = (plan.PlanName ?? "").Equals("Standard", StringComparison.OrdinalIgnoreCase);
                _db.Subscriptions.Add(new Subscription
                {
                    OrgID = org.OrgID,
                    PlanID = plan.PlanID,
                    AttendanceMode = mode,
                    BasePrice = plan.BasePrice,
                    ModePrice = 0m,
                    FinalPrice = plan.BasePrice,
                    BillingCycle = billing,
                    StartDate = startDate,
                    EndDate = isTrialPlan ? DateTime.UtcNow.AddMinutes(GetTrialDurationMinutes()) : (DateTime?)null,
                    Status = "Trial"
                });

                // CREATE ADMIN USER 
                var user = new User
                {
                    OrgID = org.OrgID,
                    Name = string.IsNullOrWhiteSpace(adminName) ? email : adminName,
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword(
                        isExternal ? Guid.NewGuid().ToString("N") : req.Password
                    ),
                    AvatarUrl = null,
                    IsActive = true,
                    TotpEnabled = false,
                    ExternalProvider = isExternal ? req.ExternalProvider : null,
                    ExternalId = isExternal ? req.ExternalId : null,
                    EmailVerified = isExternal,
                    Role = UserRoleType.OrgAdmin
                };

                _db.Users.Add(user);

                await _db.SaveChangesAsync();

                var safeFirstName = "Admin";
                var safeLastName = "Admin";

                if (!string.IsNullOrWhiteSpace(adminName))
                {
                    var parts = adminName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 1)
                    {
                        safeFirstName = parts[0];
                        safeLastName = parts[0];
                    }
                    else
                    {
                        safeFirstName = parts[0];
                        safeLastName = parts[^1];
                    }
                }

                _db.Employees.Add(new Employee
                {
                    OrgID = org.OrgID,
                    UserID = user.UserID,
                    DepartmentID = null,
                    EmpNumber = "ADMIN-001",
                    FirstName = safeFirstName,
                    LastName = safeLastName,
                    MiddleName = null,
                    Role = "Admin",
                    WorkState = "Active",
                    EmploymentType = "Regular",
                    DateHired = null,
                    CreatedByUserId = user.UserID
                });

                await _db.SaveChangesAsync();

                await tx.CommitAsync();

                var planName = plan.PlanName ?? "";
                var isFreeTrial = planName == "Standard";
                var displayPrice = GetDisplayPrice(planName);
                var onboardingToken = _jwt.CreateOnboardingToken(
                    user.UserID, org.OrgID, user.Role.ToString(), plan.PlanID, planName, isFreeTrial);

                return Ok(new SignupResponse
                {
                    Ok = true,
                    RequiresBilling = true,
                    SignupToken = onboardingToken,
                    PlanName = planName,
                    Amount = isFreeTrial ? 0 : displayPrice,
                    OrgId = org.OrgID,
                    UserId = user.UserID,
                    Role = user.Role.ToString(),
                    Token = null
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    [HttpPost("complete-signup-with-billing")]
    public async Task<IActionResult> CompleteSignupWithBilling([FromBody] CompleteSignupWithBillingRequest req)
    {
        var draft = SignupDraftStore.GetAndRemove(req.DraftToken ?? "");
        if (draft == null)
            return BadRequest(new { ok = false, message = "Invalid or expired session. Please complete signup again." });

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanID == draft.PlanId && p.IsActive);
        if (plan is null)
            return BadRequest(new { ok = false, message = "Invalid plan." });

        var planName = plan.PlanName ?? "";
        var isFreeTrial = planName == "Standard";

        if (!isFreeTrial)
        {
            if (string.IsNullOrWhiteSpace(req.PaymentIntentId))
                return BadRequest(new { ok = false, message = "Payment is required for this plan." });
            if (_payMongo == null)
                return StatusCode(500, new { ok = false, message = "Payment service not configured." });
            var intent = await _payMongo.GetPaymentIntentAsync(req.PaymentIntentId);
            if (intent == null)
                return BadRequest(new { ok = false, message = "Payment session not found." });
            if (intent.Status != "succeeded")
                return BadRequest(new { ok = false, message = "Only confirmed paid payments grant access. Current status: " + (intent.Status ?? "unknown") + "." });
        }
        else if (!string.IsNullOrWhiteSpace(req.PaymentIntentId))
            return BadRequest(new { ok = false, message = "Free trial does not require payment." });

        var orgName = draft.OrgName;
        var orgCode = draft.OrgCode;
        var email = draft.AdminEmail.Trim().ToLowerInvariant();
        var adminName = draft.AdminName.Trim();
        var mode = draft.AttendanceMode.Trim();
        var billing = draft.BillingCycle.Trim();
        var isExternal = !string.IsNullOrWhiteSpace(draft.ExternalProvider) && !string.IsNullOrWhiteSpace(draft.ExternalId);

        if (await _db.Organizations.AnyAsync(o => o.OrgCode == orgCode))
            return Conflict(new { ok = false, message = "Organization code already exists." });
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
            return Conflict(new { ok = false, message = "Email already registered." });

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var org = new Organization { OrgName = orgName, OrgCode = orgCode, Status = "Trial" };
                _db.Organizations.Add(org);
                await _db.SaveChangesAsync();

                _db.OrganizationSettings.Add(new OrganizationSettings
                {
                    OrgID = org.OrgID,
                    Timezone = "Asia/Manila",
                    Currency = "PHP",
                    AttendanceMode = mode,
                    RoundRule = "None"
                });
                var startDate = DateTime.UtcNow.Date;
                var isTrialPlan = planName.Equals("Standard", StringComparison.OrdinalIgnoreCase);
                _db.Subscriptions.Add(new Subscription
                {
                    OrgID = org.OrgID,
                    PlanID = plan.PlanID,
                    AttendanceMode = mode,
                    BasePrice = plan.BasePrice,
                    ModePrice = 0m,
                    FinalPrice = plan.BasePrice,
                    BillingCycle = billing,
                    StartDate = startDate,
                    EndDate = isTrialPlan ? DateTime.UtcNow.AddMinutes(GetTrialDurationMinutes()) : (DateTime?)null,
                    Status = "Trial"
                });

                var user = new User
                {
                    OrgID = org.OrgID,
                    Name = string.IsNullOrWhiteSpace(adminName) ? email : adminName,
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword(isExternal ? Guid.NewGuid().ToString("N") : draft.Password),
                    AvatarUrl = null,
                    IsActive = true,
                    TotpEnabled = false,
                    ExternalProvider = isExternal ? draft.ExternalProvider : null,
                    ExternalId = isExternal ? draft.ExternalId : null,
                    EmailVerified = isExternal,
                    Role = UserRoleType.OrgAdmin
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                var safeFirstName = "Admin";
                var safeLastName = "Admin";
                if (!string.IsNullOrWhiteSpace(adminName))
                {
                    var parts = adminName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 1) { safeFirstName = parts[0]; safeLastName = parts[0]; }
                    else { safeFirstName = parts[0]; safeLastName = parts[^1]; }
                }
                _db.Employees.Add(new Employee
                {
                    OrgID = org.OrgID,
                    UserID = user.UserID,
                    DepartmentID = null,
                    EmpNumber = "ADMIN-001",
                    FirstName = safeFirstName,
                    LastName = safeLastName,
                    MiddleName = null,
                    Role = "Admin",
                    WorkState = "Active",
                    EmploymentType = "Regular",
                    DateHired = null,
                    CreatedByUserId = user.UserID
                });
                await _db.SaveChangesAsync();

                int? trialTxnId = null;
                if (isFreeTrial)
                {
                    var createdSub = await _db.Subscriptions.Where(s => s.OrgID == org.OrgID).OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();
                    if (createdSub != null)
                    {
                        var trialTxn = new PaymentTransaction
                        {
                            OrgID = org.OrgID,
                            UserID = user.UserID,
                            SubscriptionID = createdSub.SubscriptionID,
                            Amount = 0m,
                            Currency = "PHP",
                            Status = "paid",
                            PayMongoPaymentIntentId = "trial-" + org.OrgID + "-" + createdSub.SubscriptionID + "-" + Guid.NewGuid().ToString("N")[..8],
                            Description = "Trial start (0 PHP)",
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.PaymentTransactions.Add(trialTxn);
                        await _db.SaveChangesAsync();
                        trialTxnId = trialTxn.Id;
                        await UpsertBillingRecordFromRequestAsync(trialTxn.Id, org.OrgID, user.UserID, req);
                    }
                }

                if (!isFreeTrial && !string.IsNullOrWhiteSpace(req.PaymentIntentId))
                {
                    var paymentTxn = await _db.PaymentTransactions
                        .FirstOrDefaultAsync(pt => pt.PayMongoPaymentIntentId == req.PaymentIntentId);
                    if (paymentTxn != null)
                    {
                        paymentTxn.OrgID = org.OrgID;
                        paymentTxn.UserID = user.UserID;
                        paymentTxn.SubscriptionID = (await _db.Subscriptions
                            .Where(s => s.OrgID == org.OrgID)
                            .OrderByDescending(s => s.StartDate)
                            .Select(s => s.SubscriptionID)
                            .FirstOrDefaultAsync());
                        paymentTxn.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();

                if (!isFreeTrial && !string.IsNullOrWhiteSpace(req.PaymentIntentId))
                {
                    var paymentTxn = await _db.PaymentTransactions.FirstOrDefaultAsync(pt => pt.PayMongoPaymentIntentId == req.PaymentIntentId);
                    if (paymentTxn != null)
                        await UpsertBillingRecordFromRequestAsync(paymentTxn.Id, org.OrgID, user.UserID, req);
                    var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.OrgID == org.OrgID && s.Status == "Trial");
                    if (sub != null) { sub.Status = "Active"; sub.EndDate = DateTime.UtcNow.AddMonths(1); await _db.SaveChangesAsync(); }
                }

                var receiptTo = (req.BillingEmail ?? email)?.Trim();
                if (!string.IsNullOrEmpty(receiptTo))
                {
                    string amountLine;
                    if (isFreeTrial) amountLine = "7-day free trial";
                    else
                    {
                        var pt = string.IsNullOrWhiteSpace(req.PaymentIntentId) ? null : await _db.PaymentTransactions.FirstOrDefaultAsync(x => x.PayMongoPaymentIntentId == req.PaymentIntentId);
                        amountLine = pt != null ? "₱" + pt.Amount.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("en-PH")) : "—";
                    }
                    try { await _email.SendAsync(receiptTo, "Your Subchron receipt", EmailTemplates.GetReceiptHtml(planName, amountLine, isFreeTrial, receiptTo)); }
                    catch { /* best effort */ }
                }

                var accessToken = _jwt.CreateToken(user, null);
                return Ok(new { ok = true, userId = user.UserID, orgId = org.OrgID, role = user.Role.ToString(), name = user.Name, token = accessToken });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    private static string? NormalizeBillingPhone11(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;
        if (digits.Length >= 11) return digits.Length > 11 ? digits.Substring(digits.Length - 11, 11) : digits;
        if (digits.StartsWith("63") && digits.Length >= 10) return "0" + digits.Substring(2, Math.Min(9, digits.Length - 2)).PadRight(9, '0');
        return digits.PadLeft(11, '0').Substring(0, 11);
    }

    private async Task UpsertBillingRecordFromRequestAsync(int paymentTransactionId, int orgId, int userId, CompleteSignupWithBillingRequest req)
    {
        var phone = NormalizeBillingPhone11(req.BillingPhone);
        var existing = await _db.BillingRecords.FirstOrDefaultAsync(b => b.PaymentTransactionId == paymentTransactionId);
        var nameOnCard = req.NameOnCard?.Trim();
        if (nameOnCard != null && nameOnCard.Length > 100) nameOnCard = nameOnCard.Substring(0, 100);
        var email = req.BillingEmail?.Trim();
        if (email != null && email.Length > 256) email = email.Substring(0, 256);
        var last4 = req.Last4?.Trim();
        if (last4 != null && last4.Length > 4) last4 = last4.Substring(0, 4);
        var expiry = req.Expiry?.Trim();
        if (expiry != null && expiry.Length > 5) expiry = expiry.Substring(0, 5);
        var brand = req.Brand?.Trim();
        if (brand != null && brand.Length > 20) brand = brand.Substring(0, 20);

        if (existing != null)
        {
            existing.BillingEmail = email;
            existing.BillingPhone = phone;
            existing.NameOnCard = nameOnCard;
            existing.Last4 = last4;
            existing.Expiry = expiry;
            existing.Brand = brand;
        }
        else
        {
            _db.BillingRecords.Add(new BillingRecord
            {
                OrgID = orgId,
                UserID = userId,
                PaymentTransactionId = paymentTransactionId,
                BillingEmail = email,
                BillingPhone = phone,
                NameOnCard = nameOnCard,
                Last4 = last4,
                Expiry = expiry,
                Brand = brand,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }

    #region Request models
    public class CreateEmployeeUserRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Name { get; set; }
    }

    public class CompleteSignupWithBillingRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("draftToken")]
        public string? DraftToken { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("paymentIntentId")]
        public string? PaymentIntentId { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("billingEmail")]
        public string? BillingEmail { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("billingPhone")]
        public string? BillingPhone { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("nameOnCard")]
        public string? NameOnCard { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("last4")]
        public string? Last4 { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("expiry")]
        public string? Expiry { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("brand")]
        public string? Brand { get; set; }
    }
    #endregion

    private static decimal GetDisplayPrice(string planName)
    {
        return planName switch
        {
            "Basic" => 2499m,
            "Standard" => 5999m,
            "Enterprise" => 8999m,
            _ => 0m
        };
    }

    #region Helper Methods

    private string GetClientIp()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    // ===== LOGIN ATTEMPT TRACKING =====

    private bool IsLoginCaptchaRequired(string ip)
    {
        CleanupOldAttempts(_loginAttempts, LoginAttemptWindow);

        if (_loginAttempts.TryGetValue(ip, out var attempt))
        {
            if (DateTime.UtcNow - attempt.LastAttempt < LoginAttemptWindow)
                return attempt.Count >= MaxAttemptsBeforeCaptcha;
        }

        return false;
    }

    private void RecordLoginAttempt(string ip)
    {
        _loginAttempts.AddOrUpdate(
            ip,
            (1, DateTime.UtcNow),
            (key, existing) =>
            {
                if (DateTime.UtcNow - existing.LastAttempt >= LoginAttemptWindow)
                    return (1, DateTime.UtcNow);

                return (existing.Count + 1, DateTime.UtcNow);
            });
    }

    private void ClearLoginAttempts(string ip)
    {
        _loginAttempts.TryRemove(ip, out _);
    }

    // FORGOT PASSWORD ATTEMPT TRACKING 

    private bool IsForgotPasswordCaptchaRequiredForIp(string ip)
    {
        CleanupOldAttempts(_forgotPasswordAttempts, ForgotPasswordAttemptWindow);

        if (_forgotPasswordAttempts.TryGetValue(ip, out var attempt))
        {
            if (DateTime.UtcNow - attempt.LastAttempt < ForgotPasswordAttemptWindow)
                return attempt.Count >= MaxAttemptsBeforeCaptcha;
        }

        return false;
    }

    private void RecordForgotPasswordAttempt(string ip)
    {
        _forgotPasswordAttempts.AddOrUpdate(
            ip,
            (1, DateTime.UtcNow),
            (key, existing) =>
            {
                if (DateTime.UtcNow - existing.LastAttempt >= ForgotPasswordAttemptWindow)
                    return (1, DateTime.UtcNow);

                return (existing.Count + 1, DateTime.UtcNow);
            });
    }

    private void ClearForgotPasswordAttempts(string ip)
    {
        _forgotPasswordAttempts.TryRemove(ip, out _);
    }

    private static void CleanupOldAttempts(
        ConcurrentDictionary<string, (int Count, DateTime LastAttempt)> dict,
        TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;

        var keysToRemove = dict
            .Where(kvp => kvp.Value.LastAttempt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
            dict.TryRemove(key, out _);
    }

    // When the user has a linked Employee, returns that Employee's role so login redirect and RBAC use backoffice and portal correctly.
    private async Task<string?> GetEffectiveRoleForUserAsync(int userId)
    {
        var emp = await _db.Employees.AsNoTracking()
            .Where(e => e.UserID == userId)
            .Select(e => e.Role)
            .FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(emp)) return null;
        return NormalizeEmployeeRoleForClaim(emp.Trim());
    }

    // Maps Employee.Role string to the role name used in claims and Web redirect.
    private static string NormalizeEmployeeRoleForClaim(string employeeRole)
    {
        if (string.IsNullOrWhiteSpace(employeeRole)) return employeeRole;
        var r = employeeRole.Trim();
        if (string.Equals(r, "PayrollPersonnel", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "Payroll Personnel", StringComparison.OrdinalIgnoreCase))
            return "Payroll";
        if (string.Equals(r, "OnLeave", StringComparison.OrdinalIgnoreCase)) return "Employee";
        return r;
    }

    private async Task UpdateLastLoginAsync(int userId)
    {
        try
        {
            await _db.Users
                .Where(u => u.UserID == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTime.UtcNow));
        }
        catch
        {
            // non-critical; ignore
        }
    }

    private static string Sha256Base64(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw ?? ""));
        return Convert.ToBase64String(bytes);
    }

    // ======= TOTP Related =======

    private string RecoveryPepper =>
    HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Security:RecoveryCodePepper"] ?? "dev-pepper";

    private static byte[] NewSecretBytes(int length = 20) =>
        RandomNumberGenerator.GetBytes(length);

    private static string ToBase32(byte[] secret) =>
        Base32Encoding.ToString(secret);

    private static string BuildOtpAuthUri(string issuer, string account, string secretBase32)
    {
        var label = Uri.EscapeDataString($"{issuer}:{account}");
        var issuerEnc = Uri.EscapeDataString(issuer);
        return $"otpauth://totp/{label}?secret={secretBase32}&issuer={issuerEnc}&digits=6&period=30";
    }

    private static string QrPngDataUrl(string payload)
    {
        using var qrGen = new QRCodeGenerator();
        using var qrData = qrGen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(qrData).GetGraphic(8);
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }

    // Recovery codes are stored as JSON in User.RecoveryCodesHash
    private sealed record RcItem(string h, bool u);

    private string HashRecoveryCode(int userId, string code)
    {
        var raw = $"{RecoveryPepper}|uid:{userId}|{code.Trim()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(bytes);
    }

    private static string[] GenerateRecoveryCodes(int count = 10)
    {
        static string One()
        {
            var b = RandomNumberGenerator.GetBytes(8);
            var hex = Convert.ToHexString(b); // 16 chars
            return $"{hex[..4]}-{hex.Substring(4, 4)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}";
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (set.Count < count) set.Add(One());
        return set.ToArray();
    }

    private List<RcItem> ReadRecoveryList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<RcItem>();
        try { return JsonSerializer.Deserialize<List<RcItem>>(json) ?? new List<RcItem>(); }
        catch { return new List<RcItem>(); }
    }

    private static string WriteRecoveryList(List<RcItem> items) =>
        JsonSerializer.Serialize(items);

    #endregion
}
