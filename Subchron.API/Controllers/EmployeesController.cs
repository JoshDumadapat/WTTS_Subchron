using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Services;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;

    public EmployeesController(SubchronDbContext db, IAuditService audit, IConfiguration config)
    {
        _db = db;
        _audit = audit;
        _config = config;
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
                DateHired = e.DateHired,
                Age = e.Age,
                Gender = e.Gender,
                Phone = e.Phone,
                AddressLine1 = e.AddressLine1,
                City = e.City,
                Country = e.Country,
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
        if (userIds.Count > 0)
        {
            var userEmails = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.UserID))
                .Select(u => new { u.UserID, u.Email })
                .ToListAsync();
            var emailLookup = userEmails.ToDictionary(u => u.UserID, u => u.Email ?? "");
            foreach (var dto in list)
                if (dto.UserID.HasValue && emailLookup.TryGetValue(dto.UserID.Value, out var email))
                    dto.Email = email;
        }

        var depIds = list.Where(x => x.DepartmentID.HasValue).Select(x => x.DepartmentID!.Value).Distinct().ToList();
        if (depIds.Count > 0)
        {
            var depNames = await _db.Departments.AsNoTracking()
                .Where(d => depIds.Contains(d.DepID))
                .Select(d => new { d.DepID, d.DepartmentName })
                .ToListAsync();
            var lookup = depNames.ToDictionary(d => d.DepID, d => d.DepartmentName);
            foreach (var dto in list)
                if (dto.DepartmentID.HasValue && lookup.TryGetValue(dto.DepartmentID.Value, out var name))
                    dto.DepartmentName = name;
        }

        return Ok(list);
    }

    // Get next suggested employee number for a role (e.g. EMP-0002).
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

        var depName = emp.DepartmentID.HasValue
            ? await _db.Departments.AsNoTracking().Where(d => d.DepID == emp.DepartmentID).Select(d => d.DepartmentName).FirstOrDefaultAsync()
            : null;
        var dto = ToDto(emp);
        dto.DepartmentName = depName;
        if (emp.UserID.HasValue)
        {
            var user = await _db.Users.AsNoTracking().Where(u => u.UserID == emp.UserID.Value).Select(u => u.Email).FirstOrDefaultAsync();
            dto.Email = user;
        }

        return Ok(dto);
    }

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
        if (r.Age.HasValue && (r.Age.Value < 18 || r.Age.Value > 70))
            return BadRequest(new { ok = false, message = "Age must be between 18 and 70." });

        var role = string.IsNullOrWhiteSpace(r.Role) ? "Employee" : r.Role.Trim();
        var empNumber = string.IsNullOrWhiteSpace(r.EmpNumber) ? null : r.EmpNumber.Trim();

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

        var phoneTrimmed = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim();
        var now = DateTime.UtcNow;
        var qrToken = AttendanceQrHelper.GenerateAttendanceQrToken(32);

        var emp = new Employee
        {
            OrgID = orgId.Value,
            UserID = r.UserID,
            DepartmentID = r.DepartmentID,
            EmpNumber = empNumber,
            FirstName = r.FirstName.Trim(),
            LastName = r.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(r.MiddleName) ? null : r.MiddleName.Trim(),
            Age = r.Age,
            Gender = string.IsNullOrWhiteSpace(r.Gender) ? null : r.Gender.Trim(),
            Role = role,
            WorkState = string.IsNullOrWhiteSpace(r.WorkState) ? "Active" : r.WorkState.Trim(),
            EmploymentType = string.IsNullOrWhiteSpace(r.EmploymentType) ? "Regular" : r.EmploymentType.Trim(),
            DateHired = r.DateHired ?? DateTime.UtcNow.Date,
            Phone = phoneTrimmed?.Length > 30 ? phoneTrimmed[..30] : phoneTrimmed,
            PhoneNormalized = phoneNorm,
            AddressLine1 = string.IsNullOrWhiteSpace(r.AddressLine1) ? null : r.AddressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(r.AddressLine2) ? null : r.AddressLine2.Trim(),
            City = string.IsNullOrWhiteSpace(r.City) ? null : r.City.Trim(),
            StateProvince = string.IsNullOrWhiteSpace(r.StateProvince) ? null : r.StateProvince.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(r.PostalCode) ? null : r.PostalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(r.Country) ? "Philippines" : r.Country.Trim(),
            EmergencyContactName = string.IsNullOrWhiteSpace(r.EmergencyContactName) ? null : r.EmergencyContactName.Trim(),
            EmergencyContactPhone = string.IsNullOrWhiteSpace(r.EmergencyContactPhone) ? null : r.EmergencyContactPhone.Trim(),
            EmergencyContactRelation = string.IsNullOrWhiteSpace(r.EmergencyContactRelation) ? null : r.EmergencyContactRelation.Trim(),
            IsArchived = false,
            AttendanceQrToken = qrToken,
            AttendanceQrIssuedAt = now,
            CreatedByUserId = userId.Value,
            UpdatedByUserId = userId.Value
        };

        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(orgId, userId, "EmployeeCreated", "Employee", emp.EmpID, $"Added {emp.FirstName} {emp.LastName} ({emp.EmpNumber})");

        return Ok(ToDto(emp));
    }

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
            return StatusCode(500, new { ok = false, message = "WebBaseUrl is not configured." });
        var scanUrl = $"{webBase}/attendance/scan/{emp.AttendanceQrToken}";
        var pngBytes = AttendanceQrHelper.GenerateQrPng(scanUrl);
        return File(pngBytes, "image/png");
    }

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

        if (req?.UserID.HasValue == true)
        {
            var linkUserId = req.UserID.Value;
            var userExists = await _db.Users.AnyAsync(u => u.UserID == linkUserId && u.OrgID == orgId.Value);
            if (!userExists)
                return BadRequest(new { ok = false, message = "User not found in this organization." });
            emp.UserID = linkUserId;
        }

        if (req?.FirstName != null) { var s = req.FirstName.Trim(); if (s.Length > 0) emp.FirstName = s.Length > 80 ? s[..80] : s; }
        if (req?.LastName != null) { var s = req.LastName.Trim(); if (s.Length > 0) emp.LastName = s.Length > 80 ? s[..80] : s; }
        if (req?.Age != null) emp.Age = req.Age;
        if (req?.Gender != null) emp.Gender = string.IsNullOrWhiteSpace(req.Gender) ? null : req.Gender.Trim().Length > 20 ? req.Gender.Trim()[..20] : req.Gender.Trim();
        if (req?.MiddleName != null) emp.MiddleName = string.IsNullOrWhiteSpace(req.MiddleName) ? null : (req.MiddleName.Trim().Length > 80 ? req.MiddleName.Trim()[..80] : req.MiddleName.Trim());
        if (req?.EmpNumber != null) { var s = req.EmpNumber.Trim(); if (s.Length > 0) { var exists = await _db.Employees.AnyAsync(e => e.OrgID == orgId.Value && e.EmpNumber == s && e.EmpID != id); if (exists) return BadRequest(new { ok = false, message = "Employee number already in use." }); emp.EmpNumber = s.Length > 40 ? s[..40] : s; } }
        if (req?.Role != null) { var s = req.Role.Trim(); if (s.Length > 0) emp.Role = s.Length > 40 ? s[..40] : s; }
        if (req?.WorkState != null) { var s = req.WorkState.Trim(); if (s.Length > 0) emp.WorkState = s.Length > 40 ? s[..40] : s; }
        if (req?.EmploymentType != null) { var s = req.EmploymentType.Trim(); if (s.Length > 0) emp.EmploymentType = s.Length > 40 ? s[..40] : s; }
        if (req?.DepartmentID.HasValue == true) emp.DepartmentID = req.DepartmentID;
        if (req?.DateHired.HasValue == true) emp.DateHired = req.DateHired;
        if (req?.Phone != null)
        {
            var phoneTrimmed = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
            var newPhoneNorm = NormalizePhone(phoneTrimmed);
            if (!string.IsNullOrEmpty(newPhoneNorm))
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

        emp.UpdatedAt = DateTime.UtcNow;
        emp.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(orgId, userId, "EmployeeUpdated", "Employee", emp.EmpID, $"{emp.FirstName} {emp.LastName}");

        return Ok(ToDto(emp));
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
        if (reason.Length > 200) reason = reason[..200];

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

        await _audit.LogAsync(orgId, userId, "EmployeeArchived", "Employee", emp.EmpID, reason);

        return Ok(ToDto(emp));
    }

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
        if (reason.Length > 200) reason = reason[..200];

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

        await _audit.LogAsync(orgId, userId, "EmployeeRestored", "Employee", emp.EmpID, reason ?? "Restored");

        return Ok(ToDto(emp));
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;
        if (digits.StartsWith("63") && digits.Length >= 10)
            digits = digits.Length > 11 ? digits.Substring(digits.Length - 10, 10) : digits.Substring(2);
        return digits.Length > 11 ? digits.Substring(digits.Length - 11, 11) : (digits.Length >= 10 ? digits : null);
    }

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

    private async Task<string> GetNextEmpNumberAsync(int orgId, string role)
    {
        var prefix = GetRolePrefix(role);
        var pattern = prefix + "-%";
        var existing = await _db.Employees
            .Where(e => e.OrgID == orgId && EF.Functions.Like(e.EmpNumber, pattern))
            .Select(e => e.EmpNumber)
            .ToListAsync();
        var maxNum = 0;
        foreach (var s in existing)
        {
            var parts = s.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var n) && n > maxNum)
                maxNum = n;
        }
        return prefix + "-" + (maxNum + 1).ToString("D4");
    }

    private static EmployeeDto ToDto(Employee e)
    {
        return new EmployeeDto
        {
            EmpID = e.EmpID,
            OrgID = e.OrgID,
            UserID = e.UserID,
            DepartmentID = e.DepartmentID,
            EmpNumber = e.EmpNumber,
            FirstName = e.FirstName,
            LastName = e.LastName,
            MiddleName = e.MiddleName,
            Age = e.Age,
            Gender = e.Gender,
            Role = e.Role,
            WorkState = e.WorkState,
            EmploymentType = e.EmploymentType,
            DateHired = e.DateHired,
            Phone = e.Phone,
            AddressLine1 = e.AddressLine1,
            AddressLine2 = e.AddressLine2,
            City = e.City,
            StateProvince = e.StateProvince,
            PostalCode = e.PostalCode,
            Country = e.Country,
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
        };
    }
}

public class EmployeeDto
{
    public int EmpID { get; set; }
    public int OrgID { get; set; }
    public int? UserID { get; set; }
    public int? DepartmentID { get; set; }
    public string? DepartmentName { get; set; }
    public string EmpNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? MiddleName { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string Role { get; set; } = "";
    public string WorkState { get; set; } = "";
    public string EmploymentType { get; set; } = "";
    public DateTime? DateHired { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? Email { get; set; }
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
    public string? EmpNumber { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? MiddleName { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Role { get; set; }
    public string? WorkState { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime? DateHired { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
}

public class EmployeeUpdateRequest
{
    public int? UserID { get; set; }
    public string? EmpNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Role { get; set; }
    public string? WorkState { get; set; }
    public string? EmploymentType { get; set; }
    public int? DepartmentID { get; set; }
    public DateTime? DateHired { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
}

public class EmployeeArchiveRequest
{
    public string? Reason { get; set; }
}

public class EmployeeRestoreRequest
{
    public string? Reason { get; set; }
}
