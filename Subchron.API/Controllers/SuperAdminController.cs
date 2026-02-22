using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/superadmin")]
public class SuperAdminController : ControllerBase
{
    private readonly SubchronDbContext _db;

    public SuperAdminController(SubchronDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard/counts/organizations")]
    public async Task<IActionResult> GetTotalOrganizations(CancellationToken ct = default)
    {
        var count = await _db.Organizations.AsNoTracking().CountAsync(ct);
        return Ok(new { totalOrganizations = count });
    }

    [HttpGet("dashboard/counts/employees")]
    public async Task<IActionResult> GetTotalEmployees(CancellationToken ct = default)
    {
        var count = await _db.Employees.AsNoTracking().CountAsync(e => !e.IsArchived, ct);
        return Ok(new { totalEmployees = count });
    }

    [HttpGet("dashboard/counts/users")]
    public async Task<IActionResult> GetTotalUsers(CancellationToken ct = default)
    {
        var count = await _db.Users.AsNoTracking().CountAsync(u => u.IsActive, ct);
        return Ok(new { totalUsers = count });
    }

    [HttpGet("dashboard/counts/departments")]
    public async Task<IActionResult> GetTotalDepartments(CancellationToken ct = default)
    {
        var count = await _db.Departments.AsNoTracking().CountAsync(ct);
        return Ok(new { totalDepartments = count });
    }

    [HttpGet("dashboard/counts/organizations-by-status")]
    public async Task<IActionResult> GetOrganizationsByStatus(CancellationToken ct = default)
    {
        var trial = await _db.Organizations.AsNoTracking().CountAsync(o => o.Status == "Trial", ct);
        var active = await _db.Organizations.AsNoTracking().CountAsync(o => o.Status == "Active", ct);
        var suspended = await _db.Organizations.AsNoTracking().CountAsync(o => o.Status == "Suspended", ct);
        return Ok(new
        {
            trialOrganizations = trial,
            activeOrganizations = active,
            suspendedOrganizations = suspended
        });
    }

    [HttpGet("dashboard/counts/new-organizations-this-month")]
    public async Task<IActionResult> GetNewOrganizationsThisMonth(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await _db.Organizations.AsNoTracking()
            .CountAsync(o => o.CreatedAt >= startOfMonth, ct);
        return Ok(new { newOrganizationsThisMonth = count });
    }

    [HttpGet("dashboard/counts/leave-requests")]
    public async Task<IActionResult> GetLeaveRequestsCount([FromQuery] string? status = null, CancellationToken ct = default)
    {
        var query = _db.LeaveRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(lr => lr.Status == status.Trim());
        var count = await query.CountAsync(ct);
        return Ok(new { totalLeaveRequests = count });
    }

    [HttpGet("dashboard/counts/subscriptions")]
    public async Task<IActionResult> GetSubscriptionsCount([FromQuery] string? status = null, CancellationToken ct = default)
    {
        var query = _db.Subscriptions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status.Trim());
        var count = await query.CountAsync(ct);
        return Ok(new { totalSubscriptions = count });
    }

    [HttpGet("dashboard/revenue")]
    public async Task<IActionResult> GetTotalRevenue(CancellationToken ct = default)
    {
        var total = await _db.PaymentTransactions.AsNoTracking()
            .Where(t => t.Status == "paid")
            .SumAsync(t => t.Amount, ct);
        return Ok(new { totalRevenue = total, currency = "PHP" });
    }

    [HttpGet("dashboard/trials-expiring")]
    public async Task<IActionResult> GetTrialsExpiringSoon([FromQuery] int withinDays = 14, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(withinDays);
        var list = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Status == "Trial" && s.EndDate != null && s.EndDate <= cutoff && s.EndDate >= DateTime.UtcNow.Date)
            .OrderBy(s => s.EndDate)
            .Take(limit)
            .Select(s => new SuperAdminTrialExpiringDto
            {
                OrgId = s.OrgID,
                OrgName = s.Organization!.OrgName,
                OrgCode = s.Organization.OrgCode,
                EndDate = s.EndDate!.Value,
                DaysRemaining = EF.Functions.DateDiffDay(DateTime.UtcNow.Date, s.EndDate.Value)
            })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("dashboard/recent-activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var list = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new SuperAdminRecentActivityDto
            {
                Action = a.Action,
                EntityName = a.EntityName,
                Details = a.Details,
                CreatedAt = a.CreatedAt,
                OrgId = a.OrgID,
                OrgName = a.Organization != null ? a.Organization.OrgName : null
            })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("dashboard/growth")]
    public async Task<IActionResult> GetOrganizationGrowth([FromQuery] int months = 6, CancellationToken ct = default)
    {
        var end = DateTime.UtcNow;
        var start = end.AddMonths(-Math.Max(1, months));
        var buckets = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.CreatedAt >= start)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var result = new List<SuperAdminGrowthMonthDto>();
        var totalOrgsBeforePeriod = await _db.Organizations.AsNoTracking()
            .CountAsync(o => o.CreatedAt < start, ct);
        var runningTotal = totalOrgsBeforePeriod;
        for (var d = start; d <= end; d = d.AddMonths(1))
        {
            var bucket = buckets.FirstOrDefault(b => b.Year == d.Year && b.Month == d.Month);
            var newCount = bucket?.Count ?? 0;
            runningTotal += newCount;
            result.Add(new SuperAdminGrowthMonthDto
            {
                Year = d.Year,
                Month = d.Month,
                NewOrganizations = newCount,
                TotalOrganizationsCumulative = runningTotal
            });
        }
        return Ok(result);
    }

    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummary(
        [FromQuery] int trialsExpiringWithinDays = 14,
        [FromQuery] int recentActivityLimit = 20,
        [FromQuery] int growthMonths = 6,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var trialsEndCutoff = now.Date.AddDays(trialsExpiringWithinDays);

        var totalOrgs = await _db.Organizations.AsNoTracking().CountAsync(ct);
        var trialOrgs = await _db.Organizations.AsNoTracking().CountAsync(o => o.Status == "Trial", ct);
        var activeOrgs = await _db.Organizations.AsNoTracking().CountAsync(o => o.Status == "Active", ct);
        var suspendedOrgs = await _db.Organizations.AsNoTracking().CountAsync(o => o.Status == "Suspended", ct);
        var newOrgsThisMonth = await _db.Organizations.AsNoTracking().CountAsync(o => o.CreatedAt >= startOfMonth, ct);
        var totalEmployees = await _db.Employees.AsNoTracking().CountAsync(e => !e.IsArchived, ct);
        var totalUsers = await _db.Users.AsNoTracking().CountAsync(u => u.IsActive, ct);
        var totalRevenue = await _db.PaymentTransactions.AsNoTracking()
            .Where(t => t.Status == "paid")
            .SumAsync(t => t.Amount, ct);

        var trialsExpiring = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Status == "Trial" && s.EndDate != null && s.EndDate <= trialsEndCutoff && s.EndDate >= now.Date)
            .OrderBy(s => s.EndDate)
            .Take(20)
            .Select(s => new SuperAdminTrialExpiringDto
            {
                OrgId = s.OrgID,
                OrgName = s.Organization!.OrgName,
                OrgCode = s.Organization.OrgCode,
                EndDate = s.EndDate!.Value,
                DaysRemaining = EF.Functions.DateDiffDay(now.Date, s.EndDate.Value)
            })
            .ToListAsync(ct);

        var recentActivity = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(recentActivityLimit)
            .Select(a => new SuperAdminRecentActivityDto
            {
                Action = a.Action,
                EntityName = a.EntityName,
                Details = a.Details,
                CreatedAt = a.CreatedAt,
                OrgId = a.OrgID,
                OrgName = a.Organization != null ? a.Organization.OrgName : null
            })
            .ToListAsync(ct);

        var growthStart = now.AddMonths(-Math.Max(1, growthMonths));
        var growthBuckets = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.CreatedAt >= growthStart)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var totalOrgsBeforeGrowth = await _db.Organizations.AsNoTracking()
            .CountAsync(o => o.CreatedAt < growthStart, ct);
        var runningTotal = totalOrgsBeforeGrowth;
        var growth = new List<SuperAdminGrowthMonthDto>();
        for (var d = growthStart; d <= now; d = d.AddMonths(1))
        {
            var b = growthBuckets.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
            var newCount = b?.Count ?? 0;
            runningTotal += newCount;
            growth.Add(new SuperAdminGrowthMonthDto
            {
                Year = d.Year,
                Month = d.Month,
                NewOrganizations = newCount,
                TotalOrganizationsCumulative = runningTotal
            });
        }

        return Ok(new SuperAdminDashboardSummaryDto
        {
            TotalOrganizations = totalOrgs,
            TrialOrganizations = trialOrgs,
            ActiveOrganizations = activeOrgs,
            SuspendedOrganizations = suspendedOrgs,
            NewOrganizationsThisMonth = newOrgsThisMonth,
            TotalEmployees = totalEmployees,
            TotalUsers = totalUsers,
            TotalRevenue = totalRevenue,
            Currency = "PHP",
            TrialsExpiringSoon = trialsExpiring,
            RecentActivity = recentActivity,
            OrganizationGrowth = growth
        });
    }

    public sealed class SuperAdminTrialExpiringDto
    {
        public int OrgId { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    public sealed class SuperAdminRecentActivityDto
    {
        public string Action { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? OrgId { get; set; }
        public string? OrgName { get; set; }
    }

    public sealed class SuperAdminGrowthMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int NewOrganizations { get; set; }
        public int TotalOrganizationsCumulative { get; set; }
    }

    public sealed class SuperAdminDashboardSummaryDto
    {
        public int TotalOrganizations { get; set; }
        public int TrialOrganizations { get; set; }
        public int ActiveOrganizations { get; set; }
        public int SuspendedOrganizations { get; set; }
        public int NewOrganizationsThisMonth { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public string Currency { get; set; } = "PHP";
        public List<SuperAdminTrialExpiringDto> TrialsExpiringSoon { get; set; } = new();
        public List<SuperAdminRecentActivityDto> RecentActivity { get; set; } = new();
        public List<SuperAdminGrowthMonthDto> OrganizationGrowth { get; set; } = new();
    }
}
