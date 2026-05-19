using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;

namespace Subchron.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin-dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly TenantDbContext _db;

    public AdminDashboardController(TenantDbContext db)
    {
        _db = db;
    }

    private int? GetOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId,
        [FromQuery] int recentActivityLimit = 10,
        CancellationToken ct = default)
    {
        var orgId = GetOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var fromDate = DateOnly.FromDateTime((from?.Date ?? DateTime.UtcNow.Date.AddDays(-6)));
        var toDate = DateOnly.FromDateTime((to?.Date ?? DateTime.UtcNow.Date));
        if (toDate < fromDate)
            (fromDate, toDate) = (toDate, fromDate);

        var employeesQuery = _db.Employees.AsNoTracking()
            .Where(e => e.OrgID == orgId.Value && !e.IsArchived);
        if (departmentId.HasValue)
            employeesQuery = employeesQuery.Where(e => e.DepartmentID == departmentId.Value);

        var totalEmployees = await employeesQuery.CountAsync(ct);

        var attendanceQuery = _db.AttendanceLogs.AsNoTracking()
            .Where(a => a.OrgID == orgId.Value && a.LogDate >= fromDate && a.LogDate <= toDate)
            .Join(_db.Employees.AsNoTracking(), a => a.EmpID, e => e.EmpID, (a, e) => new { a, e });
        if (departmentId.HasValue)
            attendanceQuery = attendanceQuery.Where(x => x.e.DepartmentID == departmentId.Value);

        var employeeIds = await employeesQuery.Select(e => e.EmpID).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var todayLogs = await _db.AttendanceLogs.AsNoTracking()
            .Where(a => a.OrgID == orgId.Value && a.LogDate == today && employeeIds.Contains(a.EmpID))
            .GroupBy(a => a.EmpID)
            .Select(g => new
            {
                EmpId = g.Key,
                TimeIn = g.Min(x => x.TimeIn),
                TimeOut = g.Max(x => x.TimeOut)
            })
            .ToListAsync(ct);

        var config = await _db.OrgAttendanceConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId.Value, ct)
            ?? new Models.Entities.OrgAttendanceConfig
            {
                OrgID = orgId.Value,
                EarliestClockInMinutes = 0,
                LatestClockInMinutes = 0,
                UseGracePeriodForLate = false,
                MarkUndertimeBasedOnSchedule = true
            };

        var lateArrivals = 0;
        foreach (var row in todayLogs)
        {
            if (!row.TimeIn.HasValue)
                continue;

            var shift = await ResolveEffectiveShiftAsync(orgId.Value, row.EmpId, today.ToDateTime(TimeOnly.MinValue), config);
            if (!shift.ok)
                continue;

            var lateBase = shift.start;
            if (config.UseGracePeriodForLate)
                lateBase = lateBase.AddMinutes(shift.graceMinutes);
            if (row.TimeIn.Value > lateBase)
                lateArrivals++;
        }

        var presentToday = todayLogs.Count(x => x.TimeIn.HasValue);
        var missingTimeOut = todayLogs.Count(x => x.TimeIn.HasValue && !x.TimeOut.HasValue);

        var onLeave = await _db.LeaveRequests.AsNoTracking()
            .Where(l => l.OrgID == orgId.Value && l.Status == "Approved" && employeeIds.Contains(l.EmpID))
            .CountAsync(l => l.StartDate.Date <= DateTime.UtcNow.Date && l.EndDate.Date >= DateTime.UtcNow.Date, ct);

        var pendingOvertime = await _db.OvertimeRequests.AsNoTracking()
            .CountAsync(o => o.OrgID == orgId.Value && o.Status == "Pending" && employeeIds.Contains(o.EmpID), ct);

        var trendPoints = new List<TrendPoint>();
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            var dayLogs = await _db.AttendanceLogs.AsNoTracking()
                .Where(a => a.OrgID == orgId.Value && a.LogDate == date && employeeIds.Contains(a.EmpID))
                .GroupBy(a => a.EmpID)
                .Select(g => new { EmpId = g.Key, TimeIn = g.Min(x => x.TimeIn), TimeOut = g.Max(x => x.TimeOut) })
                .ToListAsync(ct);

            var onTime = 0;
            var late = 0;
            foreach (var row in dayLogs)
            {
                if (!row.TimeIn.HasValue)
                    continue;

                var shift = await ResolveEffectiveShiftAsync(orgId.Value, row.EmpId, date.ToDateTime(TimeOnly.MinValue), config);
                if (!shift.ok)
                    continue;

                var lateBase = shift.start;
                if (config.UseGracePeriodForLate)
                    lateBase = lateBase.AddMinutes(shift.graceMinutes);

                if (row.TimeIn.Value > lateBase)
                    late++;
                else
                    onTime++;
            }

            trendPoints.Add(new TrendPoint
            {
                Date = date.ToDateTime(TimeOnly.MinValue),
                OnTimeCount = onTime,
                LateCount = late
            });
        }

        var departmentBreakdownQuery = _db.Departments.AsNoTracking()
            .Where(d => d.OrgID == orgId.Value);
        if (departmentId.HasValue)
            departmentBreakdownQuery = departmentBreakdownQuery.Where(d => d.DepID == departmentId.Value);

        var departmentBreakdown = await departmentBreakdownQuery
            .Select(d => new DepartmentBreakdown
            {
                DepartmentId = d.DepID,
                DepartmentName = d.DepartmentName,
                PresentCount = 0
            })
            .ToListAsync(ct);

        var presentQuery = _db.AttendanceLogs.AsNoTracking()
            .Where(a => a.OrgID == orgId.Value && a.LogDate == today)
            .Join(_db.Employees.AsNoTracking(), a => a.EmpID, e => e.EmpID, (a, e) => new { a, e });
        if (departmentId.HasValue)
            presentQuery = presentQuery.Where(x => x.e.DepartmentID == departmentId.Value);

        var presentByDepartment = await presentQuery
            .Where(x => x.a.TimeIn != null)
            .GroupBy(x => x.e.DepartmentID)
            .Select(g => new { DepartmentId = g.Key, Count = g.Select(x => x.e.EmpID).Distinct().Count() })
            .ToListAsync(ct);

        foreach (var dep in departmentBreakdown)
        {
            var count = presentByDepartment.FirstOrDefault(x => x.DepartmentId == dep.DepartmentId)?.Count ?? 0;
            dep.PresentCount = count;
        }

        var recentQuery = _db.AttendanceLogs.AsNoTracking()
            .Where(a => a.OrgID == orgId.Value)
            .Join(_db.Employees.AsNoTracking(), a => a.EmpID, e => e.EmpID, (a, e) => new { a, e });
        if (departmentId.HasValue)
            recentQuery = recentQuery.Where(x => x.e.DepartmentID == departmentId.Value);

        var recentActivity = await recentQuery
            .OrderByDescending(x => x.a.TimeIn ?? x.a.TimeOut)
            .Take(recentActivityLimit)
            .Join(_db.Departments.AsNoTracking(), x => x.e.DepartmentID, d => d.DepID, (x, d) => new { x.a, x.e, d })
            .Select(x => new ActivityItem
            {
                EmployeeName = (x.e.FirstName + " " + x.e.LastName).Trim(),
                DepartmentName = x.d.DepartmentName,
                Action = x.a.TimeOut == null ? "Time In" : "Time Out",
                Status = x.a.TimeOut == null ? "Present" : "Out",
                Time = x.a.TimeOut ?? x.a.TimeIn ?? DateTime.UtcNow
            })
            .ToListAsync(ct);

        return Ok(new AdminDashboardSummary
        {
            TotalEmployees = totalEmployees,
            PresentToday = presentToday,
            LateArrivals = lateArrivals,
            OnLeave = onLeave,
            PendingOvertime = pendingOvertime,
            MissingTimeOut = missingTimeOut,
            AttendanceTrends = trendPoints,
            DepartmentBreakdown = departmentBreakdown,
            RecentActivity = recentActivity
        });
    }

    private async Task<(bool ok, DateTime start, DateTime end, int graceMinutes)> ResolveEffectiveShiftAsync(
        int orgId,
        int empId,
        DateTime date,
        Models.Entities.OrgAttendanceConfig config)
    {
        var assignment = await _db.ShiftAssignments.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrgID == orgId && s.EmpID == empId && s.AssignmentDate == date.Date);
        if (assignment is not null)
        {
            var start = date.Date.Add(assignment.StartTime);
            var end = date.Date.Add(assignment.EndTime);
            if (end <= start)
                end = end.AddDays(1);
            return (true, start, end, 0);
        }

        var templateCode = await _db.Employees.AsNoTracking()
            .Where(e => e.EmpID == empId)
            .Select(e => e.AssignedShiftTemplateCode)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(templateCode))
        {
            var departmentId = await _db.Employees.AsNoTracking()
                .Where(e => e.EmpID == empId)
                .Select(e => e.DepartmentID)
                .FirstOrDefaultAsync();

            templateCode = await _db.Departments.AsNoTracking()
                .Where(d => departmentId.HasValue && d.DepID == departmentId.Value)
                .Select(d => d.DefaultShiftTemplateCode)
                .FirstOrDefaultAsync();
        }

        if (string.IsNullOrWhiteSpace(templateCode))
            templateCode = config.DefaultShiftTemplateCode;
        if (string.IsNullOrWhiteSpace(templateCode))
            return (false, default, default, 0);

        var template = await _db.OrgShiftTemplates.AsNoTracking()
            .Include(t => t.WorkDays)
            .FirstOrDefaultAsync(t => t.OrgID == orgId && t.Code == templateCode && t.IsActive);
        if (template is null || string.IsNullOrWhiteSpace(template.FixedStartTime) || string.IsNullOrWhiteSpace(template.FixedEndTime))
            return (false, default, default, 0);

        var dayCode = date.DayOfWeek.ToString()[..3].ToUpperInvariant();
        if (template.WorkDays.Count > 0 && !template.WorkDays.Any(x => x.DayCode.Trim().ToUpperInvariant().StartsWith(dayCode, StringComparison.Ordinal)))
            return (false, default, default, 0);

        if (!TimeSpan.TryParse(template.FixedStartTime, out var startTs) || !TimeSpan.TryParse(template.FixedEndTime, out var endTs))
            return (false, default, default, 0);

        var startAt = date.Date.Add(startTs);
        var endAt = date.Date.Add(endTs);
        if (endAt <= startAt)
            endAt = endAt.AddDays(1);
        return (true, startAt, endAt, template.FixedGraceMinutes ?? 0);
    }

    public sealed class AdminDashboardSummary
    {
        public int TotalEmployees { get; set; }
        public int PresentToday { get; set; }
        public int LateArrivals { get; set; }
        public int OnLeave { get; set; }
        public int PendingOvertime { get; set; }
        public int MissingTimeOut { get; set; }
        public List<TrendPoint> AttendanceTrends { get; set; } = new();
        public List<DepartmentBreakdown> DepartmentBreakdown { get; set; } = new();
        public List<ActivityItem> RecentActivity { get; set; } = new();
    }

    public sealed class TrendPoint
    {
        public DateTime Date { get; set; }
        public int OnTimeCount { get; set; }
        public int LateCount { get; set; }
    }

    public sealed class DepartmentBreakdown
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int PresentCount { get; set; }
    }

    public sealed class ActivityItem
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }
}
