using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;

namespace Subchron.API.Controllers;
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly TenantDbContext _db;

    public AdminController(TenantDbContext db)
    {
        _db = db;
    }

    private int? GetOrgId([FromQuery] int? orgId)
    {
        if (orgId is > 0) return orgId;
        var claim = User.FindFirstValue("orgId");
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    [HttpGet("dashboard/counts/employees")]
    public async Task<IActionResult> GetEmployeesCount([FromQuery] int? orgId, CancellationToken ct = default)
    {
        var org = GetOrgId(orgId);
        if (org is null) return Forbid();
        var count = await _db.Employees.AsNoTracking()
            .CountAsync(e => e.OrgID == org && !e.IsArchived, ct);
        return Ok(new { totalEmployees = count });
    }

    [HttpGet("dashboard/counts/departments")]
    public async Task<IActionResult> GetDepartmentsCount([FromQuery] int? orgId, CancellationToken ct = default)
    {
        var org = GetOrgId(orgId);
        if (org is null) return Forbid();
        var count = await _db.Departments.AsNoTracking()
            .CountAsync(d => d.OrgID == org, ct);
        return Ok(new { totalDepartments = count });
    }

    [HttpGet("dashboard/counts/leave-requests")]
    public async Task<IActionResult> GetLeaveRequestsCount([FromQuery] int? orgId, [FromQuery] string? status = null, CancellationToken ct = default)
    {
        var org = GetOrgId(orgId);
        if (org is null) return Forbid();
        var query = _db.LeaveRequests.AsNoTracking().Where(lr => lr.OrgID == org);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(lr => lr.Status == status.Trim());
        var count = await query.CountAsync(ct);
        return Ok(new { totalLeaveRequests = count });
    }

    [HttpGet("dashboard/departments/breakdown")]
    public async Task<IActionResult> GetDepartmentBreakdown([FromQuery] int? orgId, CancellationToken ct = default)
    {
        var org = GetOrgId(orgId);
        if (org is null) return Forbid();
        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => d.OrgID == org)
            .Select(d => new AdminDepartmentBreakdownDto
            {
                DepartmentId = d.DepID,
                DepartmentName = d.DepartmentName,
                EmployeeCount = 0
            })
            .ToListAsync(ct);
        var empCounts = await _db.Employees.AsNoTracking()
            .Where(e => e.OrgID == org && !e.IsArchived && e.DepartmentID != null)
            .GroupBy(e => e.DepartmentID!.Value)
            .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        foreach (var item in departments)
        {
            var match = empCounts.FirstOrDefault(c => c.DepartmentId == item.DepartmentId);
            item.EmployeeCount = match?.Count ?? 0;
        }
        return Ok(departments);
    }

    [HttpGet("dashboard/recent-activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int? orgId, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var org = GetOrgId(orgId);
        if (org is null) return Forbid();
        var list = await _db.TenantAuditLogs
            .AsNoTracking()
            .Where(a => a.OrgID == org)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AdminRecentActivityDto
            {
                Action = a.Action,
                EntityName = a.EntityName,
                Details = a.Details,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummary(
        [FromQuery] int? orgId,
        [FromQuery] int recentActivityLimit = 20,
        CancellationToken ct = default)
    {
        var org = GetOrgId(orgId);
        if (org is null) return Forbid();

        var totalEmployees = await _db.Employees.AsNoTracking()
            .CountAsync(e => e.OrgID == org && !e.IsArchived, ct);
        var totalDepartments = await _db.Departments.AsNoTracking()
            .CountAsync(d => d.OrgID == org, ct);
        var totalLeaveRequests = await _db.LeaveRequests.AsNoTracking()
            .CountAsync(lr => lr.OrgID == org, ct);
        var pendingLeaveRequests = await _db.LeaveRequests.AsNoTracking()
            .CountAsync(lr => lr.OrgID == org && lr.Status == "Pending", ct);

        var departmentBreakdown = await _db.Departments
            .AsNoTracking()
            .Where(d => d.OrgID == org)
            .Select(d => new AdminDepartmentBreakdownDto
            {
                DepartmentId = d.DepID,
                DepartmentName = d.DepartmentName,
                EmployeeCount = 0
            })
            .ToListAsync(ct);
        var empCounts = await _db.Employees.AsNoTracking()
            .Where(e => e.OrgID == org && !e.IsArchived && e.DepartmentID != null)
            .GroupBy(e => e.DepartmentID!.Value)
            .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        foreach (var item in departmentBreakdown)
        {
            var match = empCounts.FirstOrDefault(c => c.DepartmentId == item.DepartmentId);
            item.EmployeeCount = match?.Count ?? 0;
        }

        var recentActivity = await _db.TenantAuditLogs
            .AsNoTracking()
            .Where(a => a.OrgID == org)
            .OrderByDescending(a => a.CreatedAt)
            .Take(recentActivityLimit)
            .Select(a => new AdminRecentActivityDto
            {
                Action = a.Action,
                EntityName = a.EntityName,
                Details = a.Details,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminDashboardSummaryDto
        {
            TotalEmployees = totalEmployees,
            TotalDepartments = totalDepartments,
            TotalLeaveRequests = totalLeaveRequests,
            PendingLeaveRequests = pendingLeaveRequests,
            DepartmentBreakdown = departmentBreakdown,
            RecentActivity = recentActivity
        });
    }

    public sealed class AdminDepartmentBreakdownDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
    }

    public sealed class AdminRecentActivityDto
    {
        public string Action { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class AdminDashboardSummaryDto
    {
        public int TotalEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalLeaveRequests { get; set; }
        public int PendingLeaveRequests { get; set; }
        public List<AdminDepartmentBreakdownDto> DepartmentBreakdown { get; set; } = new();
        public List<AdminRecentActivityDto> RecentActivity { get; set; } = new();
    }
}
