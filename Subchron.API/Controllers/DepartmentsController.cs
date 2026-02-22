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
public class DepartmentsController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly IAuditService _audit;

    public DepartmentsController(SubchronDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
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

    // List departments for the current org.
    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> List([FromQuery] bool? activeOnly)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();

        var query = _db.Departments.AsNoTracking();
        if (orgId.HasValue)
            query = query.Where(d => d.OrgID == orgId.Value);
        if (activeOnly == true)
            query = query.Where(d => d.IsActive);

        var list = await query
            .OrderBy(d => d.DepartmentName)
            .Select(d => new DepartmentDto
            {
                DepID = d.DepID,
                OrgID = d.OrgID,
                DepartmentName = d.DepartmentName,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        var depIds = list.Select(x => x.DepID).ToList();
        if (depIds.Count > 0)
        {
            var counts = await _db.Employees
                .Where(e => e.DepartmentID != null && depIds.Contains(e.DepartmentID.Value))
                .GroupBy(e => e.DepartmentID!.Value)
                .Select(g => new { DepID = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var d in list)
                d.EmployeeCount = counts.FirstOrDefault(c => c.DepID == d.DepID)?.Count ?? 0;
        }

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> Get(int id)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();

        var dep = await _db.Departments.AsNoTracking()
            .Where(d => d.DepID == id && (orgId == null || d.OrgID == orgId.Value))
            .Select(d => new DepartmentDto
            {
                DepID = d.DepID,
                OrgID = d.OrgID,
                DepartmentName = d.DepartmentName,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt
            })
            .FirstOrDefaultAsync();
        if (dep is null)
            return NotFound(new { ok = false, message = "Department not found." });
        dep.EmployeeCount = await _db.Employees.CountAsync(e => e.DepartmentID == id);
        return Ok(dep);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] DepartmentCreateRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        if (string.IsNullOrWhiteSpace(req?.DepartmentName))
            return BadRequest(new { ok = false, message = "Department name is required." });

        var name = req.DepartmentName.Trim();
        var nameExists = await _db.Departments.AnyAsync(d => d.OrgID == orgId.Value && d.DepartmentName.ToLower() == name.ToLower());
        if (nameExists)
            return Conflict(new { ok = false, message = "A department with this name already exists in your organization." });

        var dep = new Department
        {
            OrgID = orgId.Value,
            DepartmentName = name,
            Description = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Departments.Add(dep);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(orgId, userId, "DepartmentCreated", "Department", dep.DepID, dep.DepartmentName);

        return Ok(new DepartmentDto
        {
            DepID = dep.DepID,
            OrgID = dep.OrgID,
            DepartmentName = dep.DepartmentName,
            Description = dep.Description,
            IsActive = dep.IsActive,
            CreatedAt = dep.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> Update(int id, [FromBody] DepartmentUpdateRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var dep = await _db.Departments.FirstOrDefaultAsync(d => d.DepID == id && d.OrgID == orgId.Value);
        if (dep is null)
            return NotFound(new { ok = false, message = "Department not found." });

        if (string.IsNullOrWhiteSpace(req?.DepartmentName))
            return BadRequest(new { ok = false, message = "Department name is required." });

        var name = req.DepartmentName.Trim();
        var nameExists = await _db.Departments.AnyAsync(d => d.OrgID == orgId.Value && d.DepID != id && d.DepartmentName.ToLower() == name.ToLower());
        if (nameExists)
            return Conflict(new { ok = false, message = "A department with this name already exists in your organization." });

        dep.DepartmentName = name;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(orgId, userId, "DepartmentUpdated", "Department", dep.DepID, dep.DepartmentName);

        return Ok(new DepartmentDto
        {
            DepID = dep.DepID,
            OrgID = dep.OrgID,
            DepartmentName = dep.DepartmentName,
            Description = dep.Description,
            IsActive = dep.IsActive,
            CreatedAt = dep.CreatedAt
        });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<DepartmentDto>> SetStatus(int id, [FromBody] DepartmentStatusRequest req)
    {
        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var dep = await _db.Departments.FirstOrDefaultAsync(d => d.DepID == id && d.OrgID == orgId.Value);
        if (dep is null)
            return NotFound(new { ok = false, message = "Department not found." });

        var reason = (req?.Reason ?? "").Trim();
        if (reason.Length > 200)
            reason = reason[..200];

        var targetActive = req?.IsActive ?? !dep.IsActive;
        if (!targetActive && string.IsNullOrEmpty(reason))
            return BadRequest(new { ok = false, message = "A reason is required when deactivating a department." });

        dep.IsActive = targetActive;
        dep.Description = reason;
        await _db.SaveChangesAsync();

        var action = dep.IsActive ? "DepartmentActivated" : "DepartmentDeactivated";
        await _audit.LogAsync(orgId, userId, action, "Department", dep.DepID, reason);

        return Ok(new DepartmentDto
        {
            DepID = dep.DepID,
            OrgID = dep.OrgID,
            DepartmentName = dep.DepartmentName,
            Description = dep.Description,
            IsActive = dep.IsActive,
            CreatedAt = dep.CreatedAt
        });
    }
}

public class DepartmentDto
{
    public int DepID { get; set; }
    public int OrgID { get; set; }
    public string DepartmentName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EmployeeCount { get; set; }
}

public class DepartmentCreateRequest
{
    public string DepartmentName { get; set; } = "";
}

public class DepartmentUpdateRequest
{
    public string DepartmentName { get; set; } = "";
}

public class DepartmentStatusRequest
{
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
}
