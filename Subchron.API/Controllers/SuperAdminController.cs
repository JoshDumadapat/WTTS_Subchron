using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Subchron.API.Data;
using Subchron.API.Models.Entities;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/superadmin")]
public class SuperAdminController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly TenantDbContext _tenantDb;

    public SuperAdminController(SubchronDbContext db, TenantDbContext tenantDb)
    {
        _db = db;
        _tenantDb = tenantDb;
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
        var count = await _tenantDb.Employees.AsNoTracking().CountAsync(e => !e.IsArchived, ct);
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
        var count = await _tenantDb.Departments.AsNoTracking().CountAsync(ct);
        return Ok(new { totalDepartments = count });
    }

    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var organizations = await _db.Organizations.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var latestSubs = await _db.Subscriptions.AsNoTracking()
            .GroupBy(s => s.OrgID)
            .Select(g => g.OrderByDescending(x => x.StartDate).ThenByDescending(x => x.SubscriptionID).First())
            .ToListAsync(ct);

        var latestByOrg = latestSubs.ToDictionary(x => x.OrgID, x => x);
        var planNames = await _db.Plans.AsNoTracking().ToDictionaryAsync(p => p.PlanID, p => p.PlanName, ct);

        var rows = organizations.Select(o =>
        {
            latestByOrg.TryGetValue(o.OrgID, out var sub);
            var planName = sub != null && planNames.TryGetValue(sub.PlanID, out var pName) ? pName : null;
            return new SuperAdminOrganizationListItemDto
            {
                OrgID = o.OrgID,
                OrgName = o.OrgName,
                OrgCode = o.OrgCode,
                Status = ComputeEffectiveStatus(o.Status, sub?.EndDate, now),
                PlanName = planName,
                SubscriptionStatus = sub?.Status,
                CreatedAt = o.CreatedAt
            };
        }).ToList();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.PlanName))
                row.SubscriptionStatus = "No subscription";
            else
                row.SubscriptionStatus = row.PlanName + " Plan";
        }

        return Ok(rows);
    }

    [HttpGet("organizations/{orgId:int}")]
    public async Task<IActionResult> GetOrganizationDetails([FromRoute] int orgId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.OrgID == orgId, ct);
        if (org == null)
            return NotFound(new { ok = false, message = "Organization not found." });

        var settings = await _db.OrganizationSettings.AsNoTracking().FirstOrDefaultAsync(s => s.OrgID == orgId, ct);
        var sub = await _db.Subscriptions.AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => s.OrgID == orgId)
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.SubscriptionID)
            .FirstOrDefaultAsync(ct);

        var employeeCount = await _tenantDb.Employees.AsNoTracking().CountAsync(e => e.OrgID == orgId && !e.IsArchived, ct);
        var activeUsers = await _db.Users.AsNoTracking().CountAsync(u => u.OrgID == orgId && u.IsActive, ct);
        var attendanceRecords = await _tenantDb.AttendanceLogs.AsNoTracking().CountAsync(a => a.OrgID == orgId, ct);
        var lastActivity = await _tenantDb.AttendanceLogs.AsNoTracking()
            .Where(a => a.OrgID == orgId)
            .Select(a => a.TimeOut ?? a.TimeIn)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct);

        var now = DateTime.UtcNow;
        var status = ComputeEffectiveStatus(org.Status, sub?.EndDate, now);

        return Ok(new SuperAdminOrganizationDetailsResponseDto
        {
            Organization = new SuperAdminOrganizationDetailDto
            {
                OrgID = org.OrgID,
                OrgName = org.OrgName,
                OrgCode = org.OrgCode,
                Status = status,
                CreatedAt = org.CreatedAt
            },
            Settings = new SuperAdminOrganizationSettingsDto
            {
                OrgID = orgId,
                Timezone = settings?.Timezone ?? "Asia/Manila",
                Currency = settings?.Currency ?? "PHP",
                AttendanceMode = settings?.AttendanceMode ?? "QR",
                DefaultShiftTemplateCode = settings?.DefaultShiftTemplateCode
            },
            CurrentSubscription = sub == null
                ? null
                : new SuperAdminOrganizationSubscriptionDto
                {
                    SubscriptionID = sub.SubscriptionID,
                    PlanName = sub.Plan?.PlanName ?? "Unknown",
                    AttendanceMode = sub.AttendanceMode,
                    FinalPrice = sub.FinalPrice,
                    BillingCycle = sub.BillingCycle,
                    StartDate = sub.StartDate,
                    EndDate = sub.EndDate ?? sub.StartDate,
                    Status = ComputeEffectiveStatus(sub.Status, sub.EndDate, now),
                    DaysRemaining = sub.EndDate.HasValue ? Math.Max(0, (sub.EndDate.Value.Date - now.Date).Days) : 0
                },
            EmployeeCount = employeeCount,
            ActiveUsers = activeUsers,
            LastActivity = lastActivity,
            StorageUsed = 0m,
            ApiCallsThisMonth = 0,
            AttendanceRecords = attendanceRecords
        });
    }

    [HttpPost("organizations")]
    public async Task<IActionResult> CreateOrganization([FromBody] SuperAdminCreateOrganizationRequestDto input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.OrgName))
            return BadRequest(new { ok = false, message = "Organization name is required." });
        if (string.IsNullOrWhiteSpace(input.OrgCode))
            return BadRequest(new { ok = false, message = "Organization code is required." });

        var orgCode = input.OrgCode.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(orgCode, "^[A-Z0-9]+$"))
            return BadRequest(new { ok = false, message = "Organization code must contain only uppercase letters and numbers." });
        if (await _db.Organizations.AnyAsync(o => o.OrgCode == orgCode, ct))
            return Conflict(new { ok = false, message = $"Organization code '{orgCode}' is already in use." });

        var status = string.IsNullOrWhiteSpace(input.Status) ? "Trial" : input.Status.Trim();
        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.PlanName == "Standard" && p.IsActive, ct)
            ?? await _db.Plans.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.PlanID).FirstOrDefaultAsync(ct);
        if (plan == null)
            return BadRequest(new { ok = false, message = "No active plan configured." });

        var org = new Organization
        {
            OrgName = input.OrgName.Trim(),
            OrgCode = orgCode,
            Status = status
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(ct);

        _db.OrganizationSettings.Add(new OrganizationSettings
        {
            OrgID = org.OrgID,
            Timezone = "Asia/Manila",
            Currency = "PHP",
            AttendanceMode = "QR",
            UpdatedAt = DateTime.UtcNow
        });

        _db.Subscriptions.Add(new Subscription
        {
            OrgID = org.OrgID,
            PlanID = plan.PlanID,
            AttendanceMode = "QR",
            BasePrice = plan.BasePrice,
            ModePrice = 0m,
            FinalPrice = plan.BasePrice,
            BillingCycle = "Monthly",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(14),
            Status = "Trial"
        });

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, orgId = org.OrgID });
    }

    [HttpPost("organizations/{orgId:int}/suspend")]
    public async Task<IActionResult> SuspendOrganization([FromRoute] int orgId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.OrgID == orgId, ct);
        if (org == null)
            return NotFound(new { ok = false, message = "Organization not found." });

        org.Status = "Suspended";
        var sub = await _db.Subscriptions.Where(s => s.OrgID == orgId)
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.SubscriptionID)
            .FirstOrDefaultAsync(ct);
        if (sub != null)
            sub.Status = "Suspended";

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
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
        var query = _tenantDb.LeaveRequests.AsNoTracking();
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
        var list = await _db.SuperAdminAuditLogs
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
                OrgName = null
            })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("dashboard/growth")]
    public async Task<IActionResult> GetOrganizationGrowth([FromQuery] int months = 6, CancellationToken ct = default)
    {
        var end = DateTime.UtcNow;
        var monthCount = Math.Max(1, months);
        var start = new DateTime(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(monthCount - 1));
        var endMonth = new DateTime(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var buckets = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.CreatedAt >= start && o.CreatedAt < endMonth.AddMonths(1))
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var result = new List<SuperAdminGrowthMonthDto>();
        var totalOrgsBeforePeriod = await _db.Organizations.AsNoTracking()
            .CountAsync(o => o.CreatedAt < start, ct);
        var runningTotal = totalOrgsBeforePeriod;
        for (var d = start; d <= endMonth; d = d.AddMonths(1))
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
        var totalEmployees = await _tenantDb.Employees.AsNoTracking().CountAsync(e => !e.IsArchived, ct);
        var totalUsers = await _db.Users.AsNoTracking().CountAsync(u => u.IsActive, ct);
        var totalRevenue = await _db.PaymentTransactions.AsNoTracking()
            .Where(t => t.Status == "paid")
            .SumAsync(t => t.Amount, ct);
        var pendingDemoRequests = await _db.DemoRequests.AsNoTracking()
            .CountAsync(d => d.Status == "Pending", ct);
        var newActiveOrganizationsThisMonth = await _db.Organizations.AsNoTracking()
            .CountAsync(o => o.Status == "Active" && o.CreatedAt >= startOfMonth, ct);

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

        var recentActivity = await _db.SuperAdminAuditLogs
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
                OrgName = null
            })
            .ToListAsync(ct);

        var growthStart = now.AddMonths(-Math.Max(1, growthMonths));
        growthStart = new DateTime(growthStart.Year, growthStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var growthEnd = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var growthBuckets = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.CreatedAt >= growthStart && o.CreatedAt < growthEnd.AddMonths(1))
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var totalOrgsBeforeGrowth = await _db.Organizations.AsNoTracking()
            .CountAsync(o => o.CreatedAt < growthStart, ct);
        var runningTotal = totalOrgsBeforeGrowth;
        var growth = new List<SuperAdminGrowthMonthDto>();
        for (var d = growthStart; d <= growthEnd; d = d.AddMonths(1))
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
            PendingDemoRequests = pendingDemoRequests,
            NewActiveOrganizationsThisMonth = newActiveOrganizationsThisMonth,
            Currency = "PHP",
            TrialsExpiringSoon = trialsExpiring,
            RecentActivity = recentActivity,
            OrganizationGrowth = growth
        });
    }

    [HttpGet("sales/transactions")]
    public async Task<IActionResult> GetSalesTransactions(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? dateStart = null,
        [FromQuery] DateTime? dateEnd = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var paymentQuery = _db.PaymentTransactions
            .AsNoTracking()
            .Include(t => t.Organization)
            .AsQueryable();

        var expenseQuery = _db.SuperAdminExpenses
            .AsNoTracking()
            .AsQueryable();

        if (dateStart.HasValue)
        {
            var startDate = dateStart.Value.Date;
            paymentQuery = paymentQuery.Where(t => t.CreatedAt >= startDate);
            expenseQuery = expenseQuery.Where(e => e.OccurredAt >= startDate);
        }

        if (dateEnd.HasValue)
        {
            var endDateExclusive = dateEnd.Value.Date.AddDays(1);
            paymentQuery = paymentQuery.Where(t => t.CreatedAt < endDateExclusive);
            expenseQuery = expenseQuery.Where(e => e.OccurredAt < endDateExclusive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            paymentQuery = paymentQuery.Where(t =>
                (!string.IsNullOrEmpty(t.Description) && t.Description.ToLower().Contains(term)) ||
                (!string.IsNullOrEmpty(t.PayMongoPaymentId) && t.PayMongoPaymentId.ToLower().Contains(term)) ||
                (!string.IsNullOrEmpty(t.PayMongoPaymentIntentId) && t.PayMongoPaymentIntentId.ToLower().Contains(term)) ||
                (t.Organization != null && (
                    (!string.IsNullOrEmpty(t.Organization.OrgName) && t.Organization.OrgName.ToLower().Contains(term)) ||
                    (!string.IsNullOrEmpty(t.Organization.OrgCode) && t.Organization.OrgCode.ToLower().Contains(term))
                )));

            expenseQuery = expenseQuery.Where(e =>
                (!string.IsNullOrEmpty(e.Description) && e.Description.ToLower().Contains(term)) ||
                (!string.IsNullOrEmpty(e.ReferenceNumber) && e.ReferenceNumber.ToLower().Contains(term)) ||
                (!string.IsNullOrEmpty(e.Category) && e.Category.ToLower().Contains(term)) ||
                (!string.IsNullOrEmpty(e.Notes) && e.Notes.ToLower().Contains(term)) ||
                (!string.IsNullOrEmpty(e.Tin) && e.Tin.ToLower().Contains(term)));
        }

        paymentQuery = paymentQuery.Where(t => t.Status == "paid" || t.Status == "refunded");

        var normalizedType = (type ?? string.Empty).Trim().ToLower();
        var includeIncome = normalizedType != "expense";
        var includeExpenses = normalizedType != "income";

        if (normalizedType == "income")
            paymentQuery = paymentQuery.Where(t => t.Status == "paid");
        else if (normalizedType == "expense")
            paymentQuery = paymentQuery.Where(t => t.Status == "refunded");

        var paidIncome = includeIncome
            ? await paymentQuery.Where(t => t.Status == "paid")
                .Select(t => (decimal?)t.Amount)
                .SumAsync(ct) ?? 0m
            : 0m;
        var refundedExpenses = includeExpenses
            ? await paymentQuery.Where(t => t.Status == "refunded")
                .Select(t => (decimal?)t.Amount)
                .SumAsync(ct) ?? 0m
            : 0m;
        var customExpenses = includeExpenses
            ? await expenseQuery
            .Select(t => (decimal?)t.Amount)
            .SumAsync(ct) ?? 0m
            : 0m;

        var totalIncome = paidIncome;
        var totalExpenses = refundedExpenses + customExpenses;

        var paymentRows = await paymentQuery
            .Select(t => new SuperAdminSalesTransactionDto
            {
                Id = t.Id,
                Date = t.CreatedAt,
                Description = string.IsNullOrWhiteSpace(t.Description)
                    ? (t.Organization != null ? ("Subscription payment - " + t.Organization.OrgName) : "Subscription transaction")
                    : t.Description!,
                Amount = t.Amount,
                Type = t.Status == "refunded" ? "Expense" : "Income",
                ReferenceNumber = !string.IsNullOrWhiteSpace(t.PayMongoPaymentId) ? t.PayMongoPaymentId! : (t.PayMongoPaymentIntentId ?? ("TXN-" + t.Id)),
                Tin = string.Empty,
                TaxAmount = 0m,
                Category = t.Status == "refunded" ? "Refund" : "Subscription",
                Notes = t.Organization != null ? (t.Organization.OrgName + " (" + t.Organization.OrgCode + ")") : null
            })
            .ToListAsync(ct);

        var expenseRows = includeExpenses
            ? await expenseQuery
                .Select(e => new SuperAdminSalesTransactionDto
                {
                    Id = e.Id,
                    Date = e.OccurredAt,
                    Description = e.Description,
                    Amount = e.Amount,
                    Type = "Expense",
                    ReferenceNumber = e.ReferenceNumber,
                    Tin = e.Tin ?? string.Empty,
                    TaxAmount = e.TaxAmount,
                    Category = string.IsNullOrWhiteSpace(e.Category) ? "Other" : e.Category!,
                    Notes = e.Notes
                })
                .ToListAsync(ct)
            : new List<SuperAdminSalesTransactionDto>();

        var merged = paymentRows
            .Concat(expenseRows)
            .OrderByDescending(x => x.Date)
            .ToList();

        var totalCount = merged.Count;

        var rows = merged
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var paymentTrendBuckets = await _db.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.CreatedAt >= monthStart && (t.Status == "paid" || t.Status == "refunded"))
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month, t.Status })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Status,
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync(ct);

        var expenseTrendBuckets = await _db.SuperAdminExpenses
            .AsNoTracking()
            .Where(e => e.OccurredAt >= monthStart)
            .GroupBy(e => new { e.OccurredAt.Year, e.OccurredAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync(ct);

        var trend = new List<SuperAdminSalesTrendPointDto>();
        for (var d = monthStart; d <= now; d = d.AddMonths(1))
        {
            var income = paymentTrendBuckets
                .Where(b => b.Year == d.Year && b.Month == d.Month && b.Status == "paid")
                .Sum(b => b.Amount);
            var expenseRefund = paymentTrendBuckets
                .Where(b => b.Year == d.Year && b.Month == d.Month && b.Status == "refunded")
                .Sum(b => b.Amount);
            var expenseManual = expenseTrendBuckets
                .Where(b => b.Year == d.Year && b.Month == d.Month)
                .Sum(b => b.Amount);
            var expense = expenseRefund + expenseManual;

            trend.Add(new SuperAdminSalesTrendPointDto
            {
                Year = d.Year,
                Month = d.Month,
                Income = income,
                Expense = expense
            });
        }

        return Ok(new SuperAdminSalesTransactionsResponseDto
        {
            Items = rows,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            Currency = "PHP",
            Trend = trend
        });
    }

    [HttpPost("sales/expenses")]
    public async Task<IActionResult> CreateExpense([FromBody] SuperAdminCreateExpenseRequestDto input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Description))
            return BadRequest(new { ok = false, message = "Description is required." });
        if (string.IsNullOrWhiteSpace(input.ReferenceNumber))
            return BadRequest(new { ok = false, message = "Reference number is required." });
        if (input.Amount <= 0)
            return BadRequest(new { ok = false, message = "Amount must be greater than zero." });
        if (input.TaxAmount < 0)
            return BadRequest(new { ok = false, message = "VAT/Tax cannot be negative." });

        var normalizedTin = NormalizeTin(input.Tin);
        if (!string.IsNullOrWhiteSpace(input.Tin) && normalizedTin == null)
            return BadRequest(new { ok = false, message = "TIN must be in 000-000-000 or 000-000-000-000 format." });

        var userId = 0;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userIdClaim))
            int.TryParse(userIdClaim, out userId);

        var expense = new SuperAdminExpense
        {
            OccurredAt = input.Date?.Date ?? DateTime.UtcNow.Date,
            Description = input.Description.Trim(),
            Amount = input.Amount,
            ReferenceNumber = input.ReferenceNumber.Trim(),
            Tin = normalizedTin,
            TaxAmount = input.TaxAmount,
            Category = string.IsNullOrWhiteSpace(input.Category) ? null : input.Category.Trim(),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            CreatedByUserId = userId > 0 ? userId : null,
            CreatedAt = DateTime.UtcNow
        };

        _db.SuperAdminExpenses.Add(expense);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = expense.Id });
    }

    private static string? NormalizeTin(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length != 9 && digits.Length != 12)
            return null;

        var groups = new List<string>();
        for (var i = 0; i < digits.Length; i += 3)
            groups.Add(digits.Substring(i, 3));
        return string.Join("-", groups);
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] string? status = null,
        [FromQuery] string? plan = null,
        [FromQuery] bool expiringOnly = false,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var query = _db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Organization)
            .Include(s => s.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(plan))
        {
            var planName = plan.Trim();
            query = query.Where(s => s.Plan.PlanName == planName);
        }

        var rows = await query
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.SubscriptionID)
            .Select(s => new SuperAdminSubscriptionListItemDto
            {
                SubscriptionID = s.SubscriptionID,
                OrgID = s.OrgID,
                OrgName = s.Organization.OrgName,
                OrgCode = s.Organization.OrgCode,
                PlanName = s.Plan.PlanName,
                AttendanceMode = s.AttendanceMode,
                BasePrice = s.BasePrice,
                ModePrice = s.ModePrice,
                FinalPrice = s.FinalPrice,
                BillingCycle = s.BillingCycle,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status
            })
            .ToListAsync(ct);

        foreach (var item in rows)
        {
            item.Status = ComputeEffectiveStatus(item.Status, item.EndDate, now);
            item.DaysRemaining = item.EndDate.HasValue
                ? Math.Max(0, (item.EndDate.Value.Date - now.Date).Days)
                : 0;
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim();
            rows = rows
                .Where(x => string.Equals(x.Status, normalized, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (expiringOnly)
            rows = rows.Where(x => x.EndDate.HasValue && x.DaysRemaining <= 7 && x.DaysRemaining >= 0).ToList();

        return Ok(rows);
    }

    [HttpGet("subscriptions/{orgId:int}/manage")]
    public async Task<IActionResult> GetSubscriptionManageData([FromRoute] int orgId, CancellationToken ct = default)
    {
        var org = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.OrgID == orgId)
            .Select(o => new SuperAdminManageOrganizationDto
            {
                OrgID = o.OrgID,
                OrgName = o.OrgName,
                OrgCode = o.OrgCode,
                Status = o.Status
            })
            .FirstOrDefaultAsync(ct);

        if (org == null)
            return NotFound(new { ok = false, message = "Organization not found." });

        var plans = await _db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PlanID)
            .Select(p => new SuperAdminManagePlanOptionDto
            {
                PlanID = p.PlanID,
                PlanName = p.PlanName,
                BasePrice = p.BasePrice
            })
            .ToListAsync(ct);

        var sub = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.OrgID == orgId)
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.SubscriptionID)
            .Select(s => new SuperAdminManageSubscriptionInputDto
            {
                OrgID = s.OrgID,
                PlanID = s.PlanID,
                AttendanceMode = s.AttendanceMode,
                BillingCycle = s.BillingCycle,
                StartDate = s.StartDate,
                EndDate = s.EndDate ?? s.StartDate,
                Status = s.Status,
                ModePrice = s.ModePrice,
                SubscriptionID = s.SubscriptionID
            })
            .FirstOrDefaultAsync(ct);

        if (sub == null)
        {
            sub = new SuperAdminManageSubscriptionInputDto
            {
                OrgID = orgId,
                AttendanceMode = "QR Code",
                BillingCycle = "Monthly",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14),
                Status = "Trial"
            };
        }
        else
        {
            sub.Status = ComputeEffectiveStatus(sub.Status, sub.EndDate, DateTime.UtcNow);
        }

        return Ok(new SuperAdminSubscriptionManageResponseDto
        {
            Organization = org,
            Plans = plans,
            Input = sub
        });
    }

    [HttpPost("subscriptions/{orgId:int}/manage")]
    public async Task<IActionResult> SaveSubscriptionManageData(
        [FromRoute] int orgId,
        [FromBody] SuperAdminManageSubscriptionInputDto input,
        CancellationToken ct = default)
    {
        if (orgId <= 0 || input.OrgID != orgId)
            return BadRequest(new { ok = false, message = "Invalid organization." });

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.OrgID == orgId, ct);
        if (org == null)
            return NotFound(new { ok = false, message = "Organization not found." });

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.PlanID == input.PlanID && p.IsActive, ct);
        if (plan == null)
            return BadRequest(new { ok = false, message = "Invalid plan selected." });

        var sub = await _db.Subscriptions
            .Where(s => s.OrgID == orgId)
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.SubscriptionID)
            .FirstOrDefaultAsync(ct);

        var modePrice = input.ModePrice ?? GetDefaultModePrice(input.AttendanceMode);
        var finalPrice = plan.BasePrice + modePrice;
        if (string.Equals(input.BillingCycle, "Annual", StringComparison.OrdinalIgnoreCase))
            finalPrice *= 0.9m;

        if (sub == null)
        {
            sub = new Subscription
            {
                OrgID = orgId,
                PlanID = plan.PlanID,
                AttendanceMode = input.AttendanceMode,
                BasePrice = plan.BasePrice,
                ModePrice = modePrice,
                FinalPrice = finalPrice,
                BillingCycle = input.BillingCycle,
                StartDate = input.StartDate.Date,
                EndDate = input.EndDate.Date,
                Status = input.Status
            };
            _db.Subscriptions.Add(sub);
        }
        else
        {
            sub.PlanID = plan.PlanID;
            sub.AttendanceMode = input.AttendanceMode;
            sub.BasePrice = plan.BasePrice;
            sub.ModePrice = modePrice;
            sub.FinalPrice = finalPrice;
            sub.BillingCycle = input.BillingCycle;
            sub.StartDate = input.StartDate.Date;
            sub.EndDate = input.EndDate.Date;
            sub.Status = input.Status;
        }

        org.Status = input.Status;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("subscriptions/{subscriptionId:int}/extend-trial")]
    public async Task<IActionResult> ExtendTrial(
        [FromRoute] int subscriptionId,
        [FromBody] SuperAdminExtendTrialRequest? req,
        CancellationToken ct = default)
    {
        var days = req?.Days is > 0 and <= 30 ? req.Days.Value : 7;
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.SubscriptionID == subscriptionId, ct);
        if (sub == null)
            return NotFound(new { ok = false, message = "Subscription not found." });

        var now = DateTime.UtcNow;
        var currentStatus = ComputeEffectiveStatus(sub.Status, sub.EndDate, now);
        if (!string.Equals(currentStatus, "Trial", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(currentStatus, "Expired", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { ok = false, message = "Only trial/expired subscriptions can be extended." });

        sub.Status = "Trial";
        sub.EndDate = (sub.EndDate.HasValue && sub.EndDate.Value > now ? sub.EndDate.Value : now).AddDays(days);

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.OrgID == sub.OrgID, ct);
        if (org != null)
            org.Status = "Trial";

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, endDate = sub.EndDate });
    }

    [HttpPost("subscriptions/{subscriptionId:int}/activate")]
    public async Task<IActionResult> ActivateSubscription([FromRoute] int subscriptionId, CancellationToken ct = default)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.SubscriptionID == subscriptionId, ct);
        if (sub == null)
            return NotFound(new { ok = false, message = "Subscription not found." });

        sub.Status = "Active";
        if (!sub.EndDate.HasValue || sub.EndDate.Value <= DateTime.UtcNow)
            sub.EndDate = DateTime.UtcNow.AddMonths(1);

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.OrgID == sub.OrgID, ct);
        if (org != null)
            org.Status = "Active";

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private static string ComputeEffectiveStatus(string? status, DateTime? endDate, DateTime now)
    {
        if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return "Cancelled";
        if (endDate.HasValue && endDate.Value < now &&
            (string.Equals(status, "Trial", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)))
            return "Expired";
        if (string.IsNullOrWhiteSpace(status))
            return "Trial";
        return status;
    }

    private static decimal GetDefaultModePrice(string? attendanceMode)
    {
        return (attendanceMode ?? string.Empty).Trim() switch
        {
            "Biometric" => 500m,
            "Geo Location" => 300m,
            _ => 0m
        };
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
        public int NewActiveOrganizationsThisMonth { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalUsers { get; set; }
        public int PendingDemoRequests { get; set; }
        public decimal TotalRevenue { get; set; }
        public string Currency { get; set; } = "PHP";
        public List<SuperAdminTrialExpiringDto> TrialsExpiringSoon { get; set; } = new();
        public List<SuperAdminRecentActivityDto> RecentActivity { get; set; } = new();
        public List<SuperAdminGrowthMonthDto> OrganizationGrowth { get; set; } = new();
    }

    public sealed class SuperAdminSubscriptionListItemDto
    {
        public int SubscriptionID { get; set; }
        public int OrgID { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string AttendanceMode { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal ModePrice { get; set; }
        public decimal FinalPrice { get; set; }
        public string BillingCycle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
    }

    public sealed class SuperAdminManageOrganizationDto
    {
        public int OrgID { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class SuperAdminManagePlanOptionDto
    {
        public int PlanID { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
    }

    public sealed class SuperAdminManageSubscriptionInputDto
    {
        public int? SubscriptionID { get; set; }
        public int OrgID { get; set; }
        public int PlanID { get; set; }
        public string AttendanceMode { get; set; } = "QR Code";
        public string BillingCycle { get; set; } = "Monthly";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Trial";
        public decimal? ModePrice { get; set; }
    }

    public sealed class SuperAdminSubscriptionManageResponseDto
    {
        public SuperAdminManageOrganizationDto Organization { get; set; } = new();
        public List<SuperAdminManagePlanOptionDto> Plans { get; set; } = new();
        public SuperAdminManageSubscriptionInputDto Input { get; set; } = new();
    }

    public sealed class SuperAdminExtendTrialRequest
    {
        public int? Days { get; set; }
    }

    public sealed class SuperAdminSalesTransactionDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Income";
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? Tin { get; set; }
        public decimal TaxAmount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public sealed class SuperAdminCreateExpenseRequestDto
    {
        public DateTime? Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? Tin { get; set; }
        public decimal TaxAmount { get; set; }
        public string? Category { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class SuperAdminSalesTrendPointDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }

    public sealed class SuperAdminSalesTransactionsResponseDto
    {
        public List<SuperAdminSalesTransactionDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public string Currency { get; set; } = "PHP";
        public List<SuperAdminSalesTrendPointDto> Trend { get; set; } = new();
    }

    public sealed class SuperAdminOrganizationListItemDto
    {
        public int OrgID { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PlanName { get; set; }
        public string? SubscriptionStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class SuperAdminOrganizationDetailDto
    {
        public int OrgID { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public sealed class SuperAdminOrganizationSettingsDto
    {
        public int OrgID { get; set; }
        public string Timezone { get; set; } = "Asia/Manila";
        public string Currency { get; set; } = "PHP";
        public string AttendanceMode { get; set; } = "QR";
        public string? DefaultShiftTemplateCode { get; set; }
    }

    public sealed class SuperAdminOrganizationSubscriptionDto
    {
        public int SubscriptionID { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string AttendanceMode { get; set; } = string.Empty;
        public decimal FinalPrice { get; set; }
        public string BillingCycle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
    }

    public sealed class SuperAdminOrganizationDetailsResponseDto
    {
        public SuperAdminOrganizationDetailDto Organization { get; set; } = new();
        public SuperAdminOrganizationSettingsDto Settings { get; set; } = new();
        public SuperAdminOrganizationSubscriptionDto? CurrentSubscription { get; set; }
        public int EmployeeCount { get; set; }
        public int ActiveUsers { get; set; }
        public DateTime? LastActivity { get; set; }
        public decimal StorageUsed { get; set; }
        public int ApiCallsThisMonth { get; set; }
        public int AttendanceRecords { get; set; }
    }

    public sealed class SuperAdminCreateOrganizationRequestDto
    {
        public string OrgName { get; set; } = string.Empty;
        public string OrgCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Trial";
    }
}
