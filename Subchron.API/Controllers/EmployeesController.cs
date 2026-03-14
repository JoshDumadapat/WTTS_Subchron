using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Authorization;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Services;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly SubchronDbContext _platformDb;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;
    private readonly ICloudinaryService _cloudinary;

    public EmployeesController(TenantDbContext db, SubchronDbContext platformDb, IAuditService audit, IConfiguration config, ICloudinaryService cloudinary)
    {
        _db = db;
        _platformDb = platformDb;
        _audit = audit;
        _config = config;
        _cloudinary = cloudinary;
    }

    private int? GetUserOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id) ? id : null;
    }

    private bool IsSuperAdmin()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    // Employee management - list employees with optional filters.
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> List(
        [FromQuery] bool? archivedOnly,
        [FromQuery] string? workState,
        [FromQuery] int? departmentId)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();

        var query = _db.Employees.AsNoTracking()
            .Where(e => orgId == null || e.OrgID == orgId.Value);

        if (archivedOnly == true)
            query = query.Where(e => e.IsArchived);
        else
            query = query.Where(e => !e.IsArchived);

        if (!string.IsNullOrWhiteSpace(workState))
            query = query.Where(e => e.WorkState == workState.Trim());
        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentID == departmentId.Value);

        var userId = GetUserId();
        if (userId.HasValue && archivedOnly != true)
            query = query.Where(e => e.UserID != userId.Value);

        var list = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new EmployeeDto
            {
                EmpID = e.EmpID,
                OrgID = e.OrgID,
                UserID = e.UserID,
                DepartmentID = e.DepartmentID,
                EmpNumber = e.EmpNumber,
                FirstName = e.FirstName,
                LastName = e.LastName,
                MiddleName = e.MiddleName,
                Role = e.Role,
                WorkState = e.WorkState,
                EmploymentType = e.EmploymentType,
                CompensationBasisOverride = e.CompensationBasisOverride,
                BasePayAmount = e.BasePayAmount,
                BaseSalary = e.BasePayAmount,
                CustomUnitLabel = e.CustomUnitLabel,
                CustomWorkHours = e.CustomWorkHours,
                DateHired = e.DateHired,
                BirthDate = e.BirthDate,
                Gender = e.Gender,
                Phone = e.Phone,
                Email = e.Email,
                AddressLine1 = e.AddressLine1,
                City = e.City,
                Country = e.Country,
                AvatarUrl = e.AvatarUrl,
                IdPictureUrl = e.IdPictureUrl,
                SignatureUrl = e.SignatureUrl,
                EmergencyContactName = e.EmergencyContactName,
                EmergencyContactPhone = e.EmergencyContactPhone,
                EmergencyContactRelation = e.EmergencyContactRelation,
                IsArchived = e.IsArchived,
                ArchivedAt = e.ArchivedAt,
                ArchivedReason = e.ArchivedReason,
                RestoredAt = e.RestoredAt,
                RestoreReason = e.RestoreReason,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();

        var userIds = list.Where(x => x.UserID.HasValue).Select(x => x.UserID!.Value).Distinct().ToList();
        var depIds = list.Where(x => x.DepartmentID.HasValue).Select(x => x.DepartmentID!.Value).Distinct().ToList();

        var emailLookupTask = BuildUserEmailLookupAsync(userIds);
        var depLookupTask = BuildDepartmentLookupAsync(depIds);
        await Task.WhenAll(emailLookupTask, depLookupTask);

        var emailLookup = await emailLookupTask;
        var depLookup = await depLookupTask;

        foreach (var dto in list)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) && dto.UserID.HasValue && emailLookup.TryGetValue(dto.UserID.Value, out var email))
                dto.Email = email;
            if (dto.DepartmentID.HasValue && depLookup.TryGetValue(dto.DepartmentID.Value, out var depName))
                dto.DepartmentName = depName;
        }

        return Ok(list);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<string>>> GetRoles([FromQuery] bool includeArchived = false)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();

        var query = _db.Employees.AsNoTracking();
        if (orgId.HasValue)
            query = query.Where(e => e.OrgID == orgId.Value);

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var roles = await query
            .Select(e => e.Role)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct()
            .OrderBy(role => role)
            .ToListAsync();

        return Ok(roles);
    }

    // Employee management - upload employee ID/signature image.
    // Uploads an employee image file (ID picture or signature) and returns its URL.
    [HttpPost("upload-image")]
    public async Task<ActionResult<object>> UploadImage([FromForm] IFormFile file, [FromQuery] string type, CancellationToken ct = default)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest(new { ok = false, message = "No file uploaded." });

        var kind = (type ?? "").Trim().ToLowerInvariant();
        var folder = kind == "signature" ? "employee/signatures" : "employee/photos";
        var prefix = kind == "signature" ? "sig" : "photo";
        var publicId = $"{prefix}_{orgId}_{Guid.NewGuid():N}";

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType?.ToLowerInvariant()))
            return BadRequest(new { ok = false, message = "Only JPEG, PNG, and WebP images are allowed." });
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { ok = false, message = "File size must be 5 MB or less." });

        try
        {
            await using var stream = file.OpenReadStream();
            var url = await _cloudinary.UploadImageAsync(stream, file.FileName ?? "image", folder, publicId, ct);
            return Ok(new { ok = true, url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, message = "Upload failed.", detail = ex.Message });
        }
    }

    // Employee management - get next generated employee number for a role.
    [HttpGet("next-number")]
    public async Task<ActionResult<object>> GetNextEmpNumber([FromQuery] string? role)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();
        var r = string.IsNullOrWhiteSpace(role) ? "Employee" : role.Trim();
        var next = await GetNextEmpNumberAsync(orgId!.Value, r);
        return Ok(new { empNumber = next });
    }

    // Employee management - get one employee profile by id.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> Get(int id)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();

        var emp = await _db.Employees.AsNoTracking()
            .Where(e => e.EmpID == id && (orgId == null || e.OrgID == orgId.Value))
            .FirstOrDefaultAsync();
        if (emp is null)
            return NotFound(new { ok = false, message = "Employee not found." });

        var depNameTask = emp.DepartmentID.HasValue
            ? _db.Departments.AsNoTracking().Where(d => d.DepID == emp.DepartmentID).Select(d => d.DepartmentName).FirstOrDefaultAsync()
            : Task.FromResult<string?>(null);
        var dto = ToDto(emp);
        var emailTask = string.IsNullOrWhiteSpace(emp.Email) && emp.UserID.HasValue
            ? _platformDb.Users.AsNoTracking().Where(u => u.UserID == emp.UserID.Value).Select(u => u.Email).FirstOrDefaultAsync()
            : Task.FromResult<string?>(null);

        await Task.WhenAll(depNameTask, emailTask);
        dto.DepartmentName = await depNameTask;
        if (string.IsNullOrWhiteSpace(dto.Email))
            dto.Email = await emailTask;

        return Ok(dto);
    }

    // Employee management - create employee record.
    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] EmployeeCreateRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue || !userId.HasValue)
            return Forbid();

        if (req is null)
            return BadRequest(new { ok = false, message = "Request body is required." });

        var r = req;
        if (string.IsNullOrWhiteSpace(r.FirstName))
            return BadRequest(new { ok = false, message = "First name is required." });
        if (string.IsNullOrWhiteSpace(r.LastName))
            return BadRequest(new { ok = false, message = "Last name is required." });

        var createFirstName = (r.FirstName ?? string.Empty).Trim().ToLower();
        var createLastName = (r.LastName ?? string.Empty).Trim().ToLower();
        var createMiddleName = (r.MiddleName ?? string.Empty).Trim().ToLower();
        var duplicateNameOnCreate = await _db.Employees.AnyAsync(e =>
            e.OrgID == orgId.Value
            && ((e.FirstName ?? string.Empty).Trim().ToLower() == createFirstName)
            && ((e.LastName ?? string.Empty).Trim().ToLower() == createLastName)
            && ((e.MiddleName ?? string.Empty).Trim().ToLower() == createMiddleName));
        if (duplicateNameOnCreate)
            return Conflict(new { ok = false, message = "This employee name already exists." });
        DateTime? normalizedBirthDate = null;
        if (r.BirthDate.HasValue)
        {
            var birthDate = r.BirthDate.Value.Date;
            var (ok, error) = ValidateBirthDate(birthDate);
            if (!ok)
                return BadRequest(new { ok = false, message = error ?? "Birthdate is invalid." });
            normalizedBirthDate = birthDate;
        }

        var role = string.IsNullOrWhiteSpace(r.Role) ? "Employee" : r.Role.Trim();
        var compensationBasisOverride = NormalizeCompensationBasisOverride(r.CompensationBasisOverride);
        var basePayAmount = r.BasePayAmount;
        if (!ValidateCompensationInput(compensationBasisOverride, basePayAmount, out var compensationError))
            return BadRequest(new { ok = false, message = compensationError });
        var customUnitLabel = string.IsNullOrWhiteSpace(r.CustomUnitLabel) ? null : r.CustomUnitLabel.Trim();
        var customWorkHours = r.CustomWorkHours;
        if (compensationBasisOverride != "Custom")
        {
            customUnitLabel = null;
            customWorkHours = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(customUnitLabel))
                return BadRequest(new { ok = false, message = "Custom unit label is required for custom compensation basis." });
            if (!customWorkHours.HasValue || customWorkHours <= 0)
                return BadRequest(new { ok = false, message = "Custom work hours must be greater than zero for custom compensation basis." });
        }
        var empNumber = string.IsNullOrWhiteSpace(r.EmpNumber) ? null : r.EmpNumber.Trim();
        var normalizedEmail = NormalizeEmail(r.Email);

        if (empNumber != null)
        {
            var empNumberExists = await _db.Employees.AnyAsync(e => e.OrgID == orgId.Value && e.EmpNumber == empNumber);
            if (empNumberExists)
                return Conflict(new { ok = false, message = "Employee number already exists in this organization." });
        }
        else
        {
            empNumber = await GetNextEmpNumberAsync(orgId.Value, role);
        }

        var phoneNorm = NormalizePhone(r.Phone);
        if (!string.IsNullOrEmpty(phoneNorm))
        {
            var phoneExists = await _db.Employees.AnyAsync(e => e.OrgID == orgId.Value && e.PhoneNormalized == phoneNorm);
            if (phoneExists)
                return Conflict(new { ok = false, message = "This contact number is already used by another employee." });
        }

        if (!string.IsNullOrEmpty(normalizedEmail))
        {
            var emailExists = await _db.Employees.AnyAsync(e => e.OrgID == orgId.Value && e.Email == normalizedEmail);
            if (emailExists)
                return Conflict(new { ok = false, message = "This email is already used by another employee." });
        }

        User? linkedUser = null;
        Department? department = null;
        if (r.DepartmentID.HasValue)
        {
            department = await _db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.OrgID == orgId.Value && d.DepID == r.DepartmentID.Value);
            if (department is null)
                return BadRequest(new { ok = false, message = "Department not found in this organization." });
        }

        if (r.UserID.HasValue)
        {
            linkedUser = await _platformDb.Users.FirstOrDefaultAsync(u => u.UserID == r.UserID.Value && u.OrgID == orgId.Value);
            if (linkedUser is null)
                return BadRequest(new { ok = false, message = "User not found in this organization." });

            if (string.IsNullOrEmpty(normalizedEmail))
            {
                normalizedEmail = NormalizeEmail(linkedUser.Email);
            }
            else
            {
                var linkedUserEmail = NormalizeEmail(linkedUser.Email);
                if (!string.Equals(linkedUserEmail, normalizedEmail, StringComparison.Ordinal))
                {
                    var inUseByAnotherUser = await _platformDb.Users.AnyAsync(u => u.UserID != linkedUser.UserID && u.Email.ToLower() == normalizedEmail);
                    if (inUseByAnotherUser)
                        return Conflict(new { ok = false, message = "This email is already used by another user account." });

                    linkedUser.Email = normalizedEmail;
                }
            }
        }

        var phoneTrimmed = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim();
        var now = DateTime.UtcNow;
        var qrToken = AttendanceQrHelper.GenerateAttendanceQrToken(32);

        var idPictureUrl = string.IsNullOrWhiteSpace(r.IdPictureUrl) ? null : r.IdPictureUrl.Trim();
        var signatureUrl = string.IsNullOrWhiteSpace(r.SignatureUrl) ? null : r.SignatureUrl.Trim();
        var avatarUrl = idPictureUrl; // Use ID picture as profile/avatar
        var assignedShiftTemplateCode = string.IsNullOrWhiteSpace(r.AssignedShiftTemplateCode)
            ? department?.DefaultShiftTemplateCode
            : r.AssignedShiftTemplateCode.Trim();
        var assignedLocationId = r.AssignedLocationId ?? department?.DefaultLocationId;

        var assignmentError = await ValidateShiftAndLocationAssignmentAsync(orgId.Value, assignedShiftTemplateCode, assignedLocationId);
        if (!string.IsNullOrEmpty(assignmentError))
            return BadRequest(new { ok = false, message = assignmentError });

        var emp = new Employee
        {
            OrgID = orgId.Value,
            UserID = r.UserID,
            DepartmentID = r.DepartmentID,
            AssignedShiftTemplateCode = assignedShiftTemplateCode,
            AssignedLocationId = assignedLocationId,
            EmpNumber = empNumber,
            FirstName = r.FirstName.Trim(),
            LastName = r.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(r.MiddleName) ? null : r.MiddleName.Trim(),
            BirthDate = normalizedBirthDate,
            Gender = string.IsNullOrWhiteSpace(r.Gender) ? null : r.Gender.Trim(),
            Role = role,
            WorkState = string.IsNullOrWhiteSpace(r.WorkState) ? "Active" : r.WorkState.Trim(),
            EmploymentType = string.IsNullOrWhiteSpace(r.EmploymentType) ? "Regular" : r.EmploymentType.Trim(),
            CompensationBasisOverride = compensationBasisOverride,
            BasePayAmount = basePayAmount,
            CustomUnitLabel = customUnitLabel,
            CustomWorkHours = customWorkHours,
            DateHired = r.DateHired ?? DateTime.UtcNow.Date,
            Phone = phoneTrimmed?.Length > 30 ? phoneTrimmed[..30] : phoneTrimmed,
            PhoneNormalized = phoneNorm,
            Email = normalizedEmail,
            AddressLine1 = string.IsNullOrWhiteSpace(r.AddressLine1) ? null : r.AddressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(r.AddressLine2) ? null : r.AddressLine2.Trim(),
            City = string.IsNullOrWhiteSpace(r.City) ? null : r.City.Trim(),
            StateProvince = string.IsNullOrWhiteSpace(r.StateProvince) ? null : r.StateProvince.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(r.PostalCode) ? null : r.PostalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(r.Country) ? "Philippines" : r.Country.Trim(),
            EmergencyContactName = string.IsNullOrWhiteSpace(r.EmergencyContactName) ? null : r.EmergencyContactName.Trim(),
            EmergencyContactPhone = string.IsNullOrWhiteSpace(r.EmergencyContactPhone) ? null : r.EmergencyContactPhone.Trim(),
            EmergencyContactRelation = string.IsNullOrWhiteSpace(r.EmergencyContactRelation) ? null : r.EmergencyContactRelation.Trim(),
            AvatarUrl = avatarUrl,
            IdPictureUrl = idPictureUrl,
            SignatureUrl = signatureUrl,
            IsArchived = false,
            AttendanceQrToken = qrToken,
            AttendanceQrIssuedAt = now,
            CreatedByUserId = userId.Value,
            UpdatedByUserId = userId.Value
        };

        _db.Employees.Add(emp);
        if (linkedUser is not null)
            await _platformDb.SaveChangesAsync();
        await _db.SaveChangesAsync();

        await _audit.LogTenantAsync(orgId!.Value, userId, "EmployeeCreated", "Employee", emp.EmpID, $"Added {emp.FirstName} {emp.LastName} ({emp.EmpNumber})");

        return Ok(ToDto(emp));
    }

    // Employee management - generate or return attendance QR image.
    // Get attendance QR PNG for an employee. Token is generated and saved if missing (legacy).
    [HttpGet("{employeeId:int}/attendance-qr")]
    public async Task<IActionResult> GetAttendanceQr(int employeeId)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();

        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpID == employeeId && (orgId == null || e.OrgID == orgId.Value));
        if (emp is null)
            return NotFound(new { ok = false, message = "Employee not found." });

        if (string.IsNullOrEmpty(emp.AttendanceQrToken))
        {
            emp.AttendanceQrToken = AttendanceQrHelper.GenerateAttendanceQrToken(32);
            emp.AttendanceQrIssuedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var webBase = (_config["WebBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(webBase))
        {
            var headerBase = (Request.Headers["X-Web-Base"].ToString() ?? "").Trim();
            if (!string.IsNullOrEmpty(headerBase))
                webBase = headerBase.TrimEnd('/');
        }
        if (string.IsNullOrEmpty(webBase))
        {
            var origin = (Request.Headers["Origin"].ToString() ?? "").Trim();
            if (!string.IsNullOrEmpty(origin))
                webBase = origin.TrimEnd('/');
        }
        if (string.IsNullOrEmpty(webBase))
            webBase = (Request.Scheme + "://" + Request.Host).TrimEnd('/');
        if (string.IsNullOrEmpty(webBase))
            return StatusCode(500, new { ok = false, message = "WebBaseUrl is not configured." });
        var scanUrl = $"{webBase}/attendance/scan/{emp.AttendanceQrToken}";
        var pngBytes = AttendanceQrHelper.GenerateQrPng(scanUrl);
        return File(pngBytes, "image/png");
    }

    // Employee management - update existing employee.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> Update(int id, [FromBody] EmployeeUpdateRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue || !userId.HasValue)
            return Forbid();

        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpID == id && e.OrgID == orgId.Value);
        if (emp is null)
            return NotFound(new { ok = false, message = "Employee not found." });

        if (emp.IsArchived)
            return BadRequest(new { ok = false, message = "Cannot update an archived employee. Restore first." });

        User? linkedUserForSync = null;
        if (req?.UserID.HasValue == true)
        {
            var linkUserId = req.UserID.Value;
            linkedUserForSync = await _platformDb.Users.FirstOrDefaultAsync(u => u.UserID == linkUserId && u.OrgID == orgId.Value);
            if (linkedUserForSync is null)
                return BadRequest(new { ok = false, message = "User not found in this organization." });
            emp.UserID = linkUserId;
        }

        if (req?.Email != null)
        {
            var normalizedEmail = NormalizeEmail(req.Email);
            if (!string.IsNullOrEmpty(normalizedEmail))
            {
                var emailTaken = await _db.Employees.AnyAsync(e => e.OrgID == orgId.Value && e.Email == normalizedEmail && e.EmpID != id);
                if (emailTaken)
                    return Conflict(new { ok = false, message = "This email is already used by another employee." });
            }

            if (string.IsNullOrEmpty(normalizedEmail) && emp.UserID.HasValue)
                return BadRequest(new { ok = false, message = "Email is required when this employee is linked to a user account." });

            emp.Email = normalizedEmail;
        }
        else if (req?.UserID.HasValue == true && linkedUserForSync is not null)
        {
            emp.Email = NormalizeEmail(linkedUserForSync.Email);
        }

        if (emp.UserID.HasValue)
        {
            linkedUserForSync ??= await _platformDb.Users.FirstOrDefaultAsync(u => u.UserID == emp.UserID.Value && u.OrgID == orgId.Value);
            if (linkedUserForSync is null)
                return BadRequest(new { ok = false, message = "Linked user not found in this organization." });

            if (!string.IsNullOrEmpty(emp.Email))
            {
                var linkedUserEmail = NormalizeEmail(linkedUserForSync.Email);
                if (!string.Equals(linkedUserEmail, emp.Email, StringComparison.Ordinal))
                {
                    var inUseByAnotherUser = await _platformDb.Users.AnyAsync(u => u.UserID != linkedUserForSync.UserID && u.Email.ToLower() == emp.Email);
                    if (inUseByAnotherUser)
                        return Conflict(new { ok = false, message = "This email is already used by another user account." });

                    linkedUserForSync.Email = emp.Email;
                }
            }
        }

        if (req?.FirstName != null) { var s = req.FirstName.Trim(); if (s.Length > 0) emp.FirstName = s.Length > 80 ? s[..80] : s; }
        if (req?.LastName != null) { var s = req.LastName.Trim(); if (s.Length > 0) emp.LastName = s.Length > 80 ? s[..80] : s; }
        if (req?.BirthDate.HasValue == true)
        {
            var birthDate = req.BirthDate.Value.Date;
            var (ok, error) = ValidateBirthDate(birthDate);
            if (!ok)
                return BadRequest(new { ok = false, message = error ?? "Birthdate is invalid." });
            emp.BirthDate = birthDate;
        }
        if (req?.Gender != null) emp.Gender = string.IsNullOrWhiteSpace(req.Gender) ? null : req.Gender.Trim().Length > 20 ? req.Gender.Trim()[..20] : req.Gender.Trim();
        if (req?.MiddleName != null) emp.MiddleName = string.IsNullOrWhiteSpace(req.MiddleName) ? null : (req.MiddleName.Trim().Length > 80 ? req.MiddleName.Trim()[..80] : req.MiddleName.Trim());

        var updateFirstName = (emp.FirstName ?? string.Empty).Trim().ToLower();
        var updateLastName = (emp.LastName ?? string.Empty).Trim().ToLower();
        var updateMiddleName = (emp.MiddleName ?? string.Empty).Trim().ToLower();
        var duplicateNameOnUpdate = await _db.Employees.AnyAsync(e =>
            e.OrgID == orgId.Value
            && e.EmpID != id
            && ((e.FirstName ?? string.Empty).Trim().ToLower() == updateFirstName)
            && ((e.LastName ?? string.Empty).Trim().ToLower() == updateLastName)
            && ((e.MiddleName ?? string.Empty).Trim().ToLower() == updateMiddleName));
        if (duplicateNameOnUpdate)
            return Conflict(new { ok = false, message = "This employee name already exists." });
        if (req?.EmpNumber != null)
        {
            var s = req.EmpNumber.Trim();
            if (s.Length > 0)
            {
                var newEmpNumber = s.Length > 40 ? s[..40] : s;
                if (!string.Equals(emp.EmpNumber, newEmpNumber, StringComparison.Ordinal))
                {
                    var exists = await _db.Employees.AnyAsync(e => e.OrgID == orgId.Value && e.EmpNumber == newEmpNumber && e.EmpID != id);
                    if (exists) return BadRequest(new { ok = false, message = "Employee number already in use." });
                    emp.EmpNumber = newEmpNumber;
                }
            }
        }
        if (req?.Role != null) { var s = req.Role.Trim(); if (s.Length > 0) emp.Role = s.Length > 40 ? s[..40] : s; }
        if (req?.WorkState != null) { var s = req.WorkState.Trim(); if (s.Length > 0) emp.WorkState = s.Length > 40 ? s[..40] : s; }
        if (req?.EmploymentType != null) { var s = req.EmploymentType.Trim(); if (s.Length > 0) emp.EmploymentType = s.Length > 40 ? s[..40] : s; }
        if (req?.CompensationBasisOverride != null)
        {
            var compensationBasisOverride = NormalizeCompensationBasisOverride(req.CompensationBasisOverride);
            if (!ValidateCompensationInput(compensationBasisOverride, req.BasePayAmount ?? emp.BasePayAmount, out var compensationError))
                return BadRequest(new { ok = false, message = compensationError });
            emp.CompensationBasisOverride = compensationBasisOverride;
            if (compensationBasisOverride != "Custom")
            {
                emp.CustomUnitLabel = null;
                emp.CustomWorkHours = null;
            }
        }
        if (req?.BasePayAmount.HasValue == true)
        {
            if (req.BasePayAmount.Value < 0)
                return BadRequest(new { ok = false, message = "Base pay amount cannot be negative." });
            emp.BasePayAmount = req.BasePayAmount.Value;
        }
        if (req?.CustomUnitLabel != null)
            emp.CustomUnitLabel = string.IsNullOrWhiteSpace(req.CustomUnitLabel) ? null : (req.CustomUnitLabel.Trim().Length > 40 ? req.CustomUnitLabel.Trim()[..40] : req.CustomUnitLabel.Trim());
        if (req?.CustomWorkHours.HasValue == true)
        {
            if (req.CustomWorkHours <= 0)
                return BadRequest(new { ok = false, message = "Custom work hours must be greater than zero." });
            emp.CustomWorkHours = req.CustomWorkHours;
        }
        if (req?.DepartmentID.HasValue == true) emp.DepartmentID = req.DepartmentID;
        if (req?.AssignedShiftTemplateCode != null)
        {
            var shiftCode = string.IsNullOrWhiteSpace(req.AssignedShiftTemplateCode) ? null : req.AssignedShiftTemplateCode.Trim();
            var shiftErr = await ValidateShiftAndLocationAssignmentAsync(orgId.Value, shiftCode, null);
            if (!string.IsNullOrEmpty(shiftErr))
                return BadRequest(new { ok = false, message = shiftErr });
            emp.AssignedShiftTemplateCode = shiftCode;
        }
        if (req?.AssignedLocationId.HasValue == true)
        {
            var locationErr = await ValidateShiftAndLocationAssignmentAsync(orgId.Value, null, req.AssignedLocationId);
            if (!string.IsNullOrEmpty(locationErr))
                return BadRequest(new { ok = false, message = locationErr });
            emp.AssignedLocationId = req.AssignedLocationId;
        }
        if (req?.DateHired.HasValue == true) emp.DateHired = req.DateHired;
        if (req?.Phone != null)
        {
            var phoneTrimmed = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
            var newPhoneNorm = NormalizePhone(phoneTrimmed);
            if (!string.Equals(emp.PhoneNormalized, newPhoneNorm, StringComparison.Ordinal) && !string.IsNullOrEmpty(newPhoneNorm))
            {
                var phoneTaken = await _db.Employees.AnyAsync(e => e.OrgID == orgId.Value && e.PhoneNormalized == newPhoneNorm && e.EmpID != id);
                if (phoneTaken)
                    return Conflict(new { ok = false, message = "This contact number is already used by another employee." });
            }
            emp.Phone = phoneTrimmed?.Length > 30 ? phoneTrimmed[..30] : phoneTrimmed;
            emp.PhoneNormalized = newPhoneNorm;
        }
        if (req?.AddressLine1 != null) emp.AddressLine1 = string.IsNullOrWhiteSpace(req.AddressLine1) ? null : (req.AddressLine1.Trim().Length > 120 ? req.AddressLine1.Trim()[..120] : req.AddressLine1.Trim());
        if (req?.AddressLine2 != null) emp.AddressLine2 = string.IsNullOrWhiteSpace(req.AddressLine2) ? null : (req.AddressLine2.Trim().Length > 120 ? req.AddressLine2.Trim()[..120] : req.AddressLine2.Trim());
        if (req?.City != null) emp.City = string.IsNullOrWhiteSpace(req.City) ? null : (req.City.Trim().Length > 80 ? req.City.Trim()[..80] : req.City.Trim());
        if (req?.StateProvince != null) emp.StateProvince = string.IsNullOrWhiteSpace(req.StateProvince) ? null : (req.StateProvince.Trim().Length > 80 ? req.StateProvince.Trim()[..80] : req.StateProvince.Trim());
        if (req?.PostalCode != null) emp.PostalCode = string.IsNullOrWhiteSpace(req.PostalCode) ? null : (req.PostalCode.Trim().Length > 20 ? req.PostalCode.Trim()[..20] : req.PostalCode.Trim());
        if (req?.Country != null) { var s = req.Country.Trim(); if (s.Length > 0) emp.Country = s.Length > 80 ? s[..80] : s; }
        if (req?.EmergencyContactName != null) emp.EmergencyContactName = string.IsNullOrWhiteSpace(req.EmergencyContactName) ? null : (req.EmergencyContactName.Trim().Length > 120 ? req.EmergencyContactName.Trim()[..120] : req.EmergencyContactName.Trim());
        if (req?.EmergencyContactPhone != null) emp.EmergencyContactPhone = string.IsNullOrWhiteSpace(req.EmergencyContactPhone) ? null : (req.EmergencyContactPhone.Trim().Length > 30 ? req.EmergencyContactPhone.Trim()[..30] : req.EmergencyContactPhone.Trim());
        if (req?.EmergencyContactRelation != null) emp.EmergencyContactRelation = string.IsNullOrWhiteSpace(req.EmergencyContactRelation) ? null : (req.EmergencyContactRelation.Trim().Length > 60 ? req.EmergencyContactRelation.Trim()[..60] : req.EmergencyContactRelation.Trim());
        if (req?.IdPictureUrl != null)
        {
            var idPic = string.IsNullOrWhiteSpace(req.IdPictureUrl) ? null : req.IdPictureUrl.Trim();
            emp.IdPictureUrl = idPic?.Length > 500 ? idPic[..500] : idPic;
            emp.AvatarUrl = emp.IdPictureUrl; // Keep avatar in sync with ID picture
        }
        if (req?.SignatureUrl != null) emp.SignatureUrl = string.IsNullOrWhiteSpace(req.SignatureUrl) ? null : (req.SignatureUrl.Trim().Length > 500 ? req.SignatureUrl.Trim()[..500] : req.SignatureUrl.Trim());
        if (req?.AvatarUrl != null) emp.AvatarUrl = string.IsNullOrWhiteSpace(req.AvatarUrl) ? null : (req.AvatarUrl.Trim().Length > 500 ? req.AvatarUrl.Trim()[..500] : req.AvatarUrl.Trim());

        emp.UpdatedAt = DateTime.UtcNow;
        emp.UpdatedByUserId = userId;
        if (linkedUserForSync is not null)
            await _platformDb.SaveChangesAsync();
        await _db.SaveChangesAsync();

        await _audit.LogTenantAsync(orgId!.Value, userId, "EmployeeUpdated", "Employee", emp.EmpID, $"{emp.FirstName} {emp.LastName}");

        return Ok(ToDto(emp));
    }

    [HttpGet("{id:int}/deductions")]
    public async Task<IActionResult> GetEmployeeDeductions(int id)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();

        var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmpID == id && (orgId == null || e.OrgID == orgId.Value));
        if (emp is null)
            return NotFound(new { ok = false, message = "Employee not found." });

        var rules = await _db.DeductionRules.AsNoTracking()
            .Where(r => r.OrgID == emp.OrgID && r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.DeductionRuleID,
                r.Name,
                r.Category,
                r.DeductionType,
                r.Amount,
                r.ComputeBasedOn
            })
            .ToListAsync();

        var selected = await _db.EmployeeDeductionProfiles.AsNoTracking()
            .Where(x => x.OrgID == emp.OrgID && x.EmpID == emp.EmpID && x.IsActive)
            .Select(x => new
            {
                x.DeductionRuleID,
                x.Mode,
                x.Value,
                x.Notes
            })
            .ToListAsync();

        return Ok(new { rules, selected });
    }

    [HttpPut("{id:int}/deductions")]
    public async Task<IActionResult> SaveEmployeeDeductions(int id, [FromBody] EmployeeDeductionSaveRequest req)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue || !GetUserId().HasValue)
            return Forbid();

        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpID == id && e.OrgID == orgId.Value);
        if (emp is null)
            return NotFound(new { ok = false, message = "Employee not found." });

        var requested = req?.Items ?? new List<EmployeeDeductionItemRequest>();
        var ruleIds = requested.Select(x => x.DeductionRuleID).Distinct().ToList();
        var validRuleIds = await _db.DeductionRules.AsNoTracking()
            .Where(r => r.OrgID == orgId.Value && r.IsActive && ruleIds.Contains(r.DeductionRuleID))
            .Select(r => r.DeductionRuleID)
            .ToListAsync();

        if (validRuleIds.Count != ruleIds.Count)
            return BadRequest(new { ok = false, message = "One or more selected deduction rules are invalid or inactive." });

        var existing = await _db.EmployeeDeductionProfiles
            .Where(x => x.OrgID == orgId.Value && x.EmpID == id)
            .ToListAsync();

        foreach (var old in existing)
            old.IsActive = false;

        foreach (var item in requested)
        {
            var mode = NormalizeDeductionMode(item.Mode);
            var profile = existing.FirstOrDefault(x => x.DeductionRuleID == item.DeductionRuleID);
            if (profile == null)
            {
                profile = new EmployeeDeductionProfile
                {
                    OrgID = orgId.Value,
                    EmpID = id,
                    DeductionRuleID = item.DeductionRuleID,
                    CreatedAt = DateTime.UtcNow
                };
                _db.EmployeeDeductionProfiles.Add(profile);
            }

            profile.Mode = mode;
            profile.Value = item.Value;
            profile.Notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim();
            profile.IsActive = true;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPatch("{id:int}/archive")]
    public async Task<ActionResult<EmployeeDto>> Archive(int id, [FromBody] EmployeeArchiveRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue || !userId.HasValue)
            return Forbid();

        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpID == id && e.OrgID == orgId.Value);
        if (emp is null)
            return NotFound(new { ok = false, message = "Employee not found." });

        if (emp.IsArchived)
            return BadRequest(new { ok = false, message = "Employee is already archived." });

        var reason = (req?.Reason ?? "").Trim();
        if (string.IsNullOrEmpty(reason))
            return BadRequest(new { ok = false, message = "Archived reason is required (e.g. Resigned, Terminated)." });
        if (reason.Length > 60) reason = reason[..60];

        emp.IsArchived = true;
        emp.ArchivedAt = DateTime.UtcNow;
        emp.ArchivedReason = reason;
        emp.ArchivedByUserId = userId;
        emp.RestoredAt = null;
        emp.RestoreReason = null;
        emp.RestoredByUserId = null;
        emp.UpdatedAt = DateTime.UtcNow;
        emp.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();

        await _audit.LogTenantAsync(orgId!.Value, userId, "EmployeeArchived", "Employee", emp.EmpID, reason);

        return Ok(ToDto(emp));
    }

    // Employee management - restore archived employee.
    [HttpPatch("{id:int}/restore")]
    public async Task<ActionResult<EmployeeDto>> Restore(int id, [FromBody] EmployeeRestoreRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue || !userId.HasValue)
            return Forbid();

        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpID == id && e.OrgID == orgId.Value);
        if (emp is null)
            return NotFound(new { ok = false, message = "Employee not found." });

        if (!emp.IsArchived)
            return BadRequest(new { ok = false, message = "Employee is not archived." });

        var reason = (req?.Reason ?? "").Trim();
        if (reason.Length > 60) reason = reason[..60];

        emp.IsArchived = false;
        emp.RestoredAt = DateTime.UtcNow;
        emp.RestoreReason = string.IsNullOrEmpty(reason) ? null : reason;
        emp.RestoredByUserId = userId;
        emp.ArchivedAt = null;
        emp.ArchivedReason = null;
        emp.ArchivedByUserId = null;
        emp.WorkState = "Active";
        emp.UpdatedAt = DateTime.UtcNow;
        emp.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();

        await _audit.LogTenantAsync(orgId!.Value, userId, "EmployeeRestored", "Employee", emp.EmpID, reason ?? "Restored");

        return Ok(ToDto(emp));
    }

    // Employee management - check uniqueness for phone/email.
    [HttpGet("check-unique")]
    public async Task<IActionResult> CheckUnique([FromQuery] string type, [FromQuery] string value, [FromQuery] string? scope = null)
    {
        var orgId = GetUserOrgId();
        var isSuperAdmin = IsSuperAdmin();
        if (!orgId.HasValue && !isSuperAdmin)
            return Forbid();

        var kind = (type ?? string.Empty).Trim().ToLowerInvariant();
        var rawValue = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(kind) || (string.IsNullOrEmpty(rawValue) && kind != "fullname" && kind != "full-name" && kind != "name"))
            return BadRequest(new { ok = false, message = "Type and value are required." });

        bool exists = false;
        if (kind == "phone")
        {
            var normalized = NormalizePhone(rawValue);
            if (!string.IsNullOrEmpty(normalized))
            {
                var query = _db.Employees.AsQueryable();
                if (orgId.HasValue)
                    query = query.Where(e => e.OrgID == orgId.Value);
                exists = await query.AnyAsync(e => e.PhoneNormalized == normalized);
            }
        }
        else if (kind == "email")
        {
            var email = NormalizeEmail(rawValue);
            if (!string.IsNullOrEmpty(email))
            {
                var checkGlobalUser = string.Equals(scope, "global-user", StringComparison.OrdinalIgnoreCase);
                var users = _platformDb.Users.AsQueryable();
                var employees = _db.Employees.AsQueryable();
                if (orgId.HasValue)
                {
                    if (!checkGlobalUser)
                        users = users.Where(u => u.OrgID == orgId.Value);
                    employees = employees.Where(e => e.OrgID == orgId.Value);
                }
                var userExistsTask = users.AnyAsync(u => u.Email != null && u.Email == email);
                var employeeExistsTask = employees.AnyAsync(e => e.Email != null && e.Email == email);
                await Task.WhenAll(userExistsTask, employeeExistsTask);
                exists = userExistsTask.Result || employeeExistsTask.Result;
            }
        }
        else if (kind == "fullname" || kind == "full-name" || kind == "name")
        {
            var parts = rawValue.Split('|');
            var firstName = parts.Length > 0 ? (parts[0] ?? string.Empty).Trim().ToLower() : string.Empty;
            var lastName = parts.Length > 1 ? (parts[1] ?? string.Empty).Trim().ToLower() : string.Empty;
            var middleName = parts.Length > 2 ? (parts[2] ?? string.Empty).Trim().ToLower() : string.Empty;
            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            {
                var employees = _db.Employees.AsQueryable();
                if (orgId.HasValue)
                    employees = employees.Where(e => e.OrgID == orgId.Value);

                exists = await employees.AnyAsync(e =>
                    ((e.FirstName ?? string.Empty).Trim().ToLower() == firstName)
                    && ((e.LastName ?? string.Empty).Trim().ToLower() == lastName)
                    && ((e.MiddleName ?? string.Empty).Trim().ToLower() == middleName));
            }
        }
        else
        {
            return BadRequest(new { ok = false, message = "Unsupported type." });
        }

        return Ok(new { ok = true, exists });
    }

    // Employee management - normalize phone for uniqueness checks.
    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;
        if (digits.StartsWith("63") && digits.Length >= 10)
            digits = digits.Length > 11 ? digits.Substring(digits.Length - 10, 10) : digits.Substring(2);
        return digits.Length > 11 ? digits.Substring(digits.Length - 11, 11) : (digits.Length >= 10 ? digits : null);
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var normalized = email.Trim().ToLowerInvariant();
        return normalized.Length == 0 ? null : normalized;
    }

    private static (bool ok, string? error) ValidateBirthDate(DateTime? birthDate)
    {
        if (!birthDate.HasValue)
            return (true, null);

        var date = birthDate.Value.Date;
        if (date > DateTime.UtcNow.Date)
            return (false, "Birthdate cannot be in the future.");

        var age = CalculateAge(date, DateTime.UtcNow.Date);
        if (age < 19 || age > 70)
            return (false, "Birthdate must make employee between 19 and 70 years old.");

        return (true, null);
    }

    private static int CalculateAge(DateTime birthDate, DateTime asOf)
    {
        var age = asOf.Year - birthDate.Year;
        if (birthDate.Date > asOf.AddYears(-age))
            age--;
        return age;
    }

    // Employee management - map role names to employee-number prefixes.
    private static string GetRolePrefix(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return "EMP";
        var r = role.Trim().ToUpperInvariant();
        if (r.StartsWith("MANAGER", StringComparison.Ordinal)) return "MGR";
        if (r == "HR") return "HR";
        if (r.StartsWith("PAYROLL", StringComparison.Ordinal)) return "PR";
        if (r.StartsWith("ADMIN", StringComparison.Ordinal)) return "ADM";
        return "EMP";
    }

    // Employee management - generate the next employee number with minimal DB reads.
    private async Task<string> GetNextEmpNumberAsync(int orgId, string role)
    {
        var prefix = GetRolePrefix(role);
        var numberPrefix = prefix + "-";
        var existing = await _db.Employees.AsNoTracking()
            .Where(e => e.OrgID == orgId && e.EmpNumber != null && e.EmpNumber.StartsWith(numberPrefix))
            .Select(e => e.EmpNumber!)
            .OrderByDescending(s => s.Length)
            .ThenByDescending(s => s)
            .Take(300)
            .ToListAsync();

        var maxNum = 0;
        foreach (var s in existing)
        {
            if (TryExtractEmpNumberSuffix(s, prefix, out var n) && n > maxNum)
                maxNum = n;
        }
        return prefix + "-" + (maxNum + 1).ToString("D4");
    }

    // Employee management - read the numeric suffix from a formatted employee number.
    private static bool TryExtractEmpNumberSuffix(string value, string prefix, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var expectedStart = prefix + "-";
        if (!value.StartsWith(expectedStart, StringComparison.OrdinalIgnoreCase)) return false;
        var suffix = value[expectedStart.Length..];
        return int.TryParse(suffix, out number);
    }

    // Employee management - build email lookup table for linked user accounts.
    private async Task<Dictionary<int, string>> BuildUserEmailLookupAsync(List<int> userIds)
    {
        if (userIds.Count == 0) return new Dictionary<int, string>();
        var userEmails = await _platformDb.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.UserID))
            .Select(u => new { u.UserID, u.Email })
            .ToListAsync();
        return userEmails.ToDictionary(u => u.UserID, u => u.Email ?? "");
    }

    // Employee management - build department lookup table for display names.
    private async Task<Dictionary<int, string>> BuildDepartmentLookupAsync(List<int> depIds)
    {
        if (depIds.Count == 0) return new Dictionary<int, string>();
        var depNames = await _db.Departments.AsNoTracking()
            .Where(d => depIds.Contains(d.DepID))
            .Select(d => new { d.DepID, d.DepartmentName })
            .ToListAsync();
        return depNames.ToDictionary(d => d.DepID, d => d.DepartmentName);
    }

    private async Task<string?> ValidateShiftAndLocationAssignmentAsync(int orgId, string? shiftTemplateCode, int? locationId)
    {
        if (!string.IsNullOrWhiteSpace(shiftTemplateCode))
        {
            var shiftExists = await _db.OrgShiftTemplates.AsNoTracking()
                .AnyAsync(s => s.OrgID == orgId && s.Code == shiftTemplateCode && s.IsActive);
            if (!shiftExists)
                return "Selected shift template is invalid or inactive.";
        }

        if (locationId.HasValue)
        {
            var locationExists = await _db.Locations.AsNoTracking()
                .AnyAsync(l => l.OrgID == orgId && l.LocationID == locationId.Value && l.IsActive);
            if (!locationExists)
                return "Selected site location is invalid or inactive.";
        }

        return null;
    }

    // Employee management - map entity model to API response shape.
    private static EmployeeDto ToDto(Employee e)
    {
        return new EmployeeDto
        {
            EmpID = e.EmpID,
            OrgID = e.OrgID,
            UserID = e.UserID,
            DepartmentID = e.DepartmentID,
            AssignedShiftTemplateCode = e.AssignedShiftTemplateCode,
            AssignedLocationId = e.AssignedLocationId,
            EmpNumber = e.EmpNumber,
            FirstName = e.FirstName,
            LastName = e.LastName,
            MiddleName = e.MiddleName,
            BirthDate = e.BirthDate,
            Gender = e.Gender,
            Role = e.Role,
            WorkState = e.WorkState,
            EmploymentType = e.EmploymentType,
            CompensationBasisOverride = e.CompensationBasisOverride,
            BasePayAmount = e.BasePayAmount,
            BaseSalary = e.BasePayAmount,
            CustomUnitLabel = e.CustomUnitLabel,
            CustomWorkHours = e.CustomWorkHours,
            DateHired = e.DateHired,
            Phone = e.Phone,
            Email = e.Email,
            AddressLine1 = e.AddressLine1,
            AddressLine2 = e.AddressLine2,
            City = e.City,
            StateProvince = e.StateProvince,
            PostalCode = e.PostalCode,
            Country = e.Country,
            EmergencyContactName = e.EmergencyContactName,
            EmergencyContactPhone = e.EmergencyContactPhone,
            EmergencyContactRelation = e.EmergencyContactRelation,
            AvatarUrl = e.AvatarUrl,
            IdPictureUrl = e.IdPictureUrl,
            SignatureUrl = e.SignatureUrl,
            IsArchived = e.IsArchived,
            ArchivedAt = e.ArchivedAt,
            ArchivedReason = e.ArchivedReason,
            RestoredAt = e.RestoredAt,
            RestoreReason = e.RestoreReason,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    private static string NormalizeCompensationBasisOverride(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized switch
        {
            "Monthly" => "Monthly",
            "Daily" => "Daily",
            "Hourly" => "Hourly",
            "Custom" => "Custom",
            _ => "UseOrgDefault"
        };
    }

    private static bool ValidateCompensationInput(string compensationBasisOverride, decimal basePayAmount, out string? message)
    {
        message = null;
        if (basePayAmount < 0)
        {
            message = "Base pay amount cannot be negative.";
            return false;
        }
        if (compensationBasisOverride != "UseOrgDefault" && basePayAmount <= 0)
        {
            message = "Base pay amount must be greater than zero when compensation basis override is set.";
            return false;
        }
        return true;
    }

    private static string NormalizeDeductionMode(string? mode)
    {
        var m = (mode ?? string.Empty).Trim();
        return m switch
        {
            "Fixed" => "Fixed",
            "Percent" => "Percent",
            _ => "UseRule"
        };
    }
}

public class EmployeeDto
{
    public int EmpID { get; set; }
    public int OrgID { get; set; }
    public int? UserID { get; set; }
    public int? DepartmentID { get; set; }
    public string? AssignedShiftTemplateCode { get; set; }
    public int? AssignedLocationId { get; set; }
    public string? DepartmentName { get; set; }
    public string EmpNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? MiddleName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string Role { get; set; } = "";
    public string WorkState { get; set; } = "";
    public string EmploymentType { get; set; } = "";
    public string CompensationBasisOverride { get; set; } = "UseOrgDefault";
    public decimal BasePayAmount { get; set; }
    public decimal BaseSalary { get; set; }
    public string? CustomUnitLabel { get; set; }
    public decimal? CustomWorkHours { get; set; }
    public DateTime? DateHired { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? AvatarUrl { get; set; }
    public string? IdPictureUrl { get; set; }
    public string? SignatureUrl { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedReason { get; set; }
    public DateTime? RestoredAt { get; set; }
    public string? RestoreReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class EmployeeCreateRequest
{
    public int? UserID { get; set; }
    public int? DepartmentID { get; set; }
    public string? AssignedShiftTemplateCode { get; set; }
    public int? AssignedLocationId { get; set; }
    public string? EmpNumber { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? MiddleName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Role { get; set; }
    public string? WorkState { get; set; }
    public string? EmploymentType { get; set; }
    public string? CompensationBasisOverride { get; set; }
    public decimal BasePayAmount { get; set; }
    public string? CustomUnitLabel { get; set; }
    public decimal? CustomWorkHours { get; set; }
    public DateTime? DateHired { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? IdPictureUrl { get; set; }
    public string? SignatureUrl { get; set; }
}

public class EmployeeUpdateRequest
{
    public int? UserID { get; set; }
    public string? EmpNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Role { get; set; }
    public string? WorkState { get; set; }
    public string? EmploymentType { get; set; }
    public string? CompensationBasisOverride { get; set; }
    public decimal? BasePayAmount { get; set; }
    public string? CustomUnitLabel { get; set; }
    public decimal? CustomWorkHours { get; set; }
    public int? DepartmentID { get; set; }
    public string? AssignedShiftTemplateCode { get; set; }
    public int? AssignedLocationId { get; set; }
    public DateTime? DateHired { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? AvatarUrl { get; set; }
    public string? IdPictureUrl { get; set; }
    public string? SignatureUrl { get; set; }
}

public class EmployeeArchiveRequest
{
    public string? Reason { get; set; }
}

public class EmployeeDeductionSaveRequest
{
    public List<EmployeeDeductionItemRequest> Items { get; set; } = new();
}

public class EmployeeDeductionItemRequest
{
    public int DeductionRuleID { get; set; }
    public string? Mode { get; set; }
    public decimal? Value { get; set; }
    public string? Notes { get; set; }
}

public class EmployeeRestoreRequest
{
    public string? Reason { get; set; }
}
