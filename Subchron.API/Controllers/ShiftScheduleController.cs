using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;

namespace Subchron.API.Controllers;

[ApiController]
[Authorize]
[Route("api/shift-schedule")]
public class ShiftScheduleController : ControllerBase
{
    private const bool EnableRapidScanTestMode = true;
    private readonly TenantDbContext _db;

    public ShiftScheduleController(TenantDbContext db)
    {
        _db = db;
    }

    [HttpGet("my/schedule")]
    public async Task<ActionResult<List<MyScheduleItemDto>>> GetMySchedule([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var ctx = await ResolveEmployeeContextAsync();
        if (!ctx.ok)
            return Forbid();

        var anchor = from?.Date ?? DateTime.UtcNow.Date;
        var mondayOffset = ((int)anchor.DayOfWeek + 6) % 7;
        var start = anchor.AddDays(-mondayOffset);
        var end = start.AddDays(6);

        var config = await _db.OrgAttendanceConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == ctx.orgId)
            ?? new OrgAttendanceConfig { OrgID = ctx.orgId, EarliestClockInMinutes = 0, LatestClockInMinutes = 0 };

        var departmentName = await _db.Departments.AsNoTracking()
            .Where(d => d.DepID == ctx.employee!.DepartmentID)
            .Select(d => d.DepartmentName)
            .FirstOrDefaultAsync();
        var siteName = await _db.Locations.AsNoTracking()
            .Where(l => l.LocationID == ctx.employee!.AssignedLocationId)
            .Select(l => l.LocationName)
            .FirstOrDefaultAsync();

        var list = new List<MyScheduleItemDto>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            var resolved = await ResolveEffectiveShiftAsync(ctx.orgId, ctx.employee!, d, config);
            DateTime? shiftStart = null;
            DateTime? shiftEnd = null;
            DateTime? inFrom = null;
            DateTime? inUntil = null;
            var shiftName = resolved.ok ? resolved.shiftName : "No shift assigned";
            var source = resolved.ok ? resolved.source : "NONE";

            if (resolved.ok)
            {
                shiftStart = resolved.start;
                shiftEnd = resolved.end;
                inFrom = resolved.start.AddMinutes(-(config.EarliestClockInMinutes ?? 0));
                inUntil = resolved.start.AddMinutes(config.LatestClockInMinutes ?? 0);
            }

            list.Add(new MyScheduleItemDto
            {
                Date = d,
                DayName = d.ToString("dddd"),
                DepartmentName = departmentName,
                SiteName = siteName,
                ShiftName = shiftName,
                ShiftStart = shiftStart,
                ShiftEnd = shiftEnd,
                Source = source,
                ClockInFrom = inFrom,
                ClockInUntil = inUntil
            });
        }

        return Ok(list);
    }

    [HttpGet("my/assigned-shifts")]
    public async Task<ActionResult<List<MyAssignedShiftItemDto>>> GetMyAssignedShifts([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var ctx = await ResolveEmployeeContextAsync();
        if (!ctx.ok)
            return Forbid();

        var start = (from?.Date ?? DateTime.UtcNow.Date);
        var end = (to?.Date ?? DateTime.UtcNow.Date.AddDays(30));
        if (end < start)
            (start, end) = (end, start);

        var departmentName = await _db.Departments.AsNoTracking()
            .Where(d => d.DepID == ctx.employee!.DepartmentID)
            .Select(d => d.DepartmentName)
            .FirstOrDefaultAsync();

        var assignments = await _db.ShiftAssignments.AsNoTracking()
            .Where(s => s.OrgID == ctx.orgId && s.EmpID == ctx.empId && s.AssignmentDate >= start && s.AssignmentDate <= end)
            .OrderBy(s => s.AssignmentDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        var list = new List<MyAssignedShiftItemDto>();
        foreach (var assignment in assignments)
        {
            var date = assignment.AssignmentDate.Date;
            var shiftStart = date.Add(assignment.StartTime);
            var shiftEnd = date.Add(assignment.EndTime);
            if (shiftEnd <= shiftStart)
                shiftEnd = shiftEnd.AddDays(1);

            var shiftName = string.IsNullOrWhiteSpace(assignment.Notes)
                ? await ResolveAssignedShiftNameAsync(ctx.orgId, assignment.StartTime, assignment.EndTime, date.DayOfWeek) ?? "Assigned Shift"
                : assignment.Notes.Trim();

            list.Add(new MyAssignedShiftItemDto
            {
                ShiftAssignmentID = assignment.ShiftAssignmentID,
                Date = date,
                DayName = date.ToString("dddd"),
                DepartmentName = departmentName,
                ShiftName = shiftName,
                ShiftStart = shiftStart,
                ShiftEnd = shiftEnd,
                Source = "ASSIGNMENT"
            });
        }

        return Ok(list);
    }

    [HttpGet("my/attendance")]
    public async Task<ActionResult<List<MyAttendanceItemDto>>> GetMyAttendance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var ctx = await ResolveEmployeeContextAsync();
        if (!ctx.ok)
            return Forbid();

        var config = await _db.OrgAttendanceConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == ctx.orgId)
            ?? new OrgAttendanceConfig { OrgID = ctx.orgId, EarliestClockInMinutes = 0, LatestClockInMinutes = 0, UseGracePeriodForLate = false, MarkUndertimeBasedOnSchedule = true };
        var policy = await _db.OrgAttendanceOvertimePolicies.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == ctx.orgId)
            ?? new OrgAttendanceOvertimePolicy { OrgID = ctx.orgId, Enabled = false };

        await ApplyAutoClockOutForEmployeeAsync(ctx.orgId, ctx.empId, config, policy, ctx.employee!);

        var start = DateOnly.FromDateTime((from?.Date ?? DateTime.UtcNow.Date.AddDays(-14)));
        var end = DateOnly.FromDateTime((to?.Date ?? DateTime.UtcNow.Date));
        if (end < start)
            (start, end) = (end, start);

        var rows = await _db.AttendanceLogs.AsNoTracking()
            .Where(a => a.OrgID == ctx.orgId && a.EmpID == ctx.empId && a.LogDate >= start && a.LogDate <= end)
            .OrderByDescending(a => a.LogDate)
            .ThenByDescending(a => a.AttendanceID)
            .ToListAsync();

        var overtimeMap = await _db.OvertimeRequests.AsNoTracking()
            .Where(x => x.OrgID == ctx.orgId && x.EmpID == ctx.empId && x.OTDate >= start && x.OTDate <= end)
            .GroupBy(x => x.OTDate)
            .Select(g => new
            {
                g.Key,
                Minutes = (int)Math.Round(g.Sum(x => x.TotalHours) * 60m, MidpointRounding.AwayFromZero)
            })
            .ToListAsync();
        var overtimeByDay = overtimeMap.ToDictionary(x => x.Key, x => x.Minutes);

        var list = new List<MyAttendanceItemDto>();
        foreach (var dayGroup in rows.GroupBy(x => x.LogDate).OrderByDescending(x => x.Key))
        {
            var latest = dayGroup.OrderByDescending(x => x.AttendanceID).First();
            var day = latest.LogDate.ToDateTime(TimeOnly.MinValue);
            var resolved = await ResolveEffectiveShiftAsync(ctx.orgId, ctx.employee!, day, config);

            var timeIn = latest.TimeIn;
            var timeOut = latest.TimeOut;

            var worked = 0;
            var late = 0;
            var undertime = 0;
            var overtime = overtimeByDay.TryGetValue(latest.LogDate, out var overtimeFromRequest)
                ? overtimeFromRequest
                : 0;

            if (timeIn.HasValue && timeOut.HasValue)
            {
                worked = (int)Math.Max(0, Math.Floor((timeOut.Value - timeIn.Value).TotalMinutes));
                if (resolved.ok)
                {
                    var lateBase = resolved.start;
                    if (config.UseGracePeriodForLate)
                        lateBase = lateBase.AddMinutes(resolved.graceMinutes);
                    late = (int)Math.Max(0, Math.Floor((timeIn.Value - lateBase).TotalMinutes));

                    if (config.MarkUndertimeBasedOnSchedule)
                        undertime = (int)Math.Max(0, Math.Floor((resolved.end - timeOut.Value).TotalMinutes));

                    overtime = Math.Max(overtime, ComputeOvertimeMinutes(policy, resolved.end, timeOut.Value));
                }

                if ((latest.Remarks ?? string.Empty).Contains("AUTO_TEST_UNDERTIME", StringComparison.OrdinalIgnoreCase) && undertime <= 0)
                    undertime = 1;
            }

            var hasTimeIn = timeIn.HasValue;
            var hasTimeOut = timeOut.HasValue;
            var status = ComputeDayStatus(hasTimeIn, hasTimeOut, late, undertime, overtime);

            list.Add(new MyAttendanceItemDto
            {
                AttendanceID = latest.AttendanceID,
                LogDate = latest.LogDate,
                TimeIn = timeIn,
                TimeOut = timeOut,
                MethodIn = latest.MethodIn,
                MethodOut = latest.MethodOut,
                Remarks = latest.Remarks,
                WorkedMinutes = worked,
                LateMinutes = late,
                UndertimeMinutes = undertime,
                OvertimeMinutes = overtime,
                Status = status
            });
        }

        return Ok(list);
    }

    [HttpPost("my/punch")]
    public async Task<IActionResult> Punch([FromBody] PunchRequest req)
    {
        var ctx = await ResolveEmployeeContextAsync();
        if (!ctx.ok)
            return Forbid();

        if (req is null || string.IsNullOrWhiteSpace(req.Action))
            return BadRequest(new { ok = false, message = "Invalid punch request." });

        var config = await _db.OrgAttendanceConfigs.FirstOrDefaultAsync(x => x.OrgID == ctx.orgId)
            ?? new OrgAttendanceConfig { OrgID = ctx.orgId, EarliestClockInMinutes = 0, LatestClockInMinutes = 0, PreventDoubleClockIn = true };
        var policy = await _db.OrgAttendanceOvertimePolicies.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == ctx.orgId)
            ?? new OrgAttendanceOvertimePolicy { OrgID = ctx.orgId, Enabled = false };

        var now = ResolveCaptureTimestamp(req.DeviceTimestamp);
        var testMode = EnableRapidScanTestMode && req.TestMode;
        var action = req.Action.Trim().ToUpperInvariant();

        if (!testMode)
            await ApplyAutoClockOutForEmployeeAsync(ctx.orgId, ctx.empId, config, policy, ctx.employee!);

        var shift = await ResolveEffectiveShiftAsync(ctx.orgId, ctx.employee!, now.Date, config);
        if (!shift.ok && !testMode)
            return BadRequest(new { ok = false, message = "No shift configured for today." });

        var openLog = await _db.AttendanceLogs
            .Where(a => a.OrgID == ctx.orgId && a.EmpID == ctx.empId && a.LogDate == DateOnly.FromDateTime(now))
            .OrderByDescending(a => a.AttendanceID)
            .FirstOrDefaultAsync();

        if (action == "IN")
        {
            if (openLog is not null && openLog.TimeIn.HasValue && !openLog.TimeOut.HasValue)
                return BadRequest(new { ok = false, message = "You are already timed in." });

            var from = shift.ok ? shift.start.AddMinutes(-(config.EarliestClockInMinutes ?? 0)) : now;
            var until = shift.ok ? shift.start.AddMinutes(config.LatestClockInMinutes ?? 0) : now;
            var lateMinutes = shift.ok ? Math.Max(0, (int)Math.Floor((now - until).TotalMinutes)) : 0;
            var earlyMinutes = shift.ok ? Math.Max(0, (int)Math.Floor((from - now).TotalMinutes)) : 0;
            var timingTag = lateMinutes > 0
                ? $"LATE_{lateMinutes}m"
                : (earlyMinutes > 0 ? $"EARLY_{earlyMinutes}m" : null);

            var row = new AttendanceLog
            {
                OrgID = ctx.orgId,
                EmpID = ctx.empId,
                LogDate = DateOnly.FromDateTime(now),
                TimeIn = now,
                MethodIn = "SELF",
                GeoLat = req.Latitude,
                GeoLong = req.Longitude,
                GeoStatus = req.Latitude.HasValue && req.Longitude.HasValue ? "CAPTURED" : null,
                DeviceInfo = req.DeviceInfo,
                Remarks = BuildPunchRemarks(testMode, timingTag)
            };
            _db.AttendanceLogs.Add(row);
            await _db.SaveChangesAsync();
            var msg = lateMinutes > 0
                ? $"Time in recorded at {now:hh:mm tt}. Marked late by {lateMinutes} minute(s)."
                : $"Time in recorded at {now:hh:mm tt}.";
            return Ok(new { ok = true, action = "TIME_IN", lateMinutes, earlyMinutes, message = msg });
        }

        if (action == "OUT")
        {
            if (openLog is null || !openLog.TimeIn.HasValue || openLog.TimeOut.HasValue)
                return BadRequest(new { ok = false, message = "No active time-in found." });

            if (!testMode && shift.ok && now < shift.start)
                return BadRequest(new { ok = false, message = $"Clock-out is allowed after shift start at {shift.start:hh:mm tt}." });

            openLog.TimeOut = now;
            openLog.MethodOut = "SELF";
            openLog.GeoLat = req.Latitude;
            openLog.GeoLong = req.Longitude;
            openLog.GeoStatus = req.Latitude.HasValue && req.Longitude.HasValue ? "CAPTURED" : openLog.GeoStatus;
            openLog.DeviceInfo = req.DeviceInfo;

            decimal overtimeHours = 0m;
            if (!testMode && shift.ok)
                await UpsertAutoOvertimeAsync(ctx.orgId, ctx.empId, shift.end, now, "SELF", policy);

            if (testMode)
            {
                var elapsed = (now - openLog.TimeIn.Value).TotalSeconds;
                if (elapsed >= 20)
                {
                    overtimeHours = Math.Round((decimal)elapsed / 3600m, 4);
                    var date = DateOnly.FromDateTime(now);
                    var existing = await _db.OvertimeRequests.FirstOrDefaultAsync(x => x.OrgID == ctx.orgId && x.EmpID == ctx.empId && x.OTDate == date && x.Reason != null && x.Reason.StartsWith("AUTO_TEST:", StringComparison.OrdinalIgnoreCase));
                    if (existing is null)
                    {
                        _db.OvertimeRequests.Add(new OvertimeRequest
                        {
                            OrgID = ctx.orgId,
                            EmpID = ctx.empId,
                            OTDate = date,
                            StartTime = openLog.TimeIn.Value,
                            EndTime = now,
                            TotalHours = overtimeHours,
                            Reason = "AUTO_TEST: Overtime generated for rapid punch mode.",
                            Status = "SystemGenerated"
                        });
                    }
                    else
                    {
                        existing.StartTime = openLog.TimeIn.Value;
                        existing.EndTime = now;
                        existing.TotalHours = overtimeHours;
                        existing.Status = "SystemGenerated";
                    }
                }

                if (elapsed < 14)
                {
                    if (string.IsNullOrWhiteSpace(openLog.Remarks))
                        openLog.Remarks = "AUTO_TEST_UNDERTIME";
                    else if (!openLog.Remarks.Contains("AUTO_TEST_UNDERTIME", StringComparison.OrdinalIgnoreCase))
                        openLog.Remarks += " | AUTO_TEST_UNDERTIME";
                }
            }

            await _db.SaveChangesAsync();
            var msg = overtimeHours > 0
                ? $"Time out recorded at {now:hh:mm tt}. Test overtime generated."
                : $"Time out recorded at {now:hh:mm tt}.";
            return Ok(new { ok = true, action = "TIME_OUT", overtimeHours, message = msg });
        }

        return BadRequest(new { ok = false, message = "Unsupported action." });
    }

    private async Task<(bool ok, int orgId, int empId, Employee? employee)> ResolveEmployeeContextAsync()
    {
        var orgClaim = User.FindFirstValue("orgId");
        var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(orgClaim, out var orgId) || !int.TryParse(userClaim, out var userId))
            return (false, 0, 0, null);

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.OrgID == orgId && e.UserID == userId && !e.IsArchived);
        if (employee is null)
            return (false, 0, 0, null);

        return (true, orgId, employee.EmpID, employee);
    }

    private async Task<(bool ok, DateTime start, DateTime end, string source, int graceMinutes, string shiftName)> ResolveEffectiveShiftAsync(int orgId, Employee employee, DateTime date, OrgAttendanceConfig config)
    {
        var assignment = await _db.ShiftAssignments.AsNoTracking().FirstOrDefaultAsync(s => s.OrgID == orgId && s.EmpID == employee.EmpID && s.AssignmentDate == date.Date);
        if (assignment is not null)
        {
            var start = date.Date.Add(assignment.StartTime);
            var end = date.Date.Add(assignment.EndTime);
            if (end <= start) end = end.AddDays(1);
            var assignedName = string.IsNullOrWhiteSpace(assignment.Notes)
                ? await ResolveAssignedShiftNameAsync(orgId, assignment.StartTime, assignment.EndTime, date.DayOfWeek) ?? "Assigned Shift"
                : assignment.Notes.Trim();
            return (true, start, end, "ASSIGNMENT", 0, assignedName);
        }

        var templateCode = !string.IsNullOrWhiteSpace(employee.AssignedShiftTemplateCode)
            ? employee.AssignedShiftTemplateCode
            : await _db.Departments.AsNoTracking().Where(d => d.DepID == employee.DepartmentID).Select(d => d.DefaultShiftTemplateCode).FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(templateCode))
            templateCode = config.DefaultShiftTemplateCode;
        if (string.IsNullOrWhiteSpace(templateCode))
            return (false, default, default, "NONE", 0, "No shift assigned");

        var template = await _db.OrgShiftTemplates.AsNoTracking().Include(t => t.WorkDays)
            .FirstOrDefaultAsync(t => t.OrgID == orgId && t.Code == templateCode && t.IsActive);
        if (template is null || string.IsNullOrWhiteSpace(template.FixedStartTime) || string.IsNullOrWhiteSpace(template.FixedEndTime))
            return (false, default, default, "NONE", 0, "No shift assigned");

        var dayCode = date.DayOfWeek.ToString()[..3].ToUpperInvariant();
        if (template.WorkDays.Count > 0 && !template.WorkDays.Any(x => x.DayCode.Trim().ToUpperInvariant().StartsWith(dayCode, StringComparison.Ordinal)))
            return (false, default, default, "NONE", 0, "No shift assigned");

        if (!TimeSpan.TryParse(template.FixedStartTime, out var startTs) || !TimeSpan.TryParse(template.FixedEndTime, out var endTs))
            return (false, default, default, "NONE", 0, "No shift assigned");

        var startAt = date.Date.Add(startTs);
        var endAt = date.Date.Add(endTs);
        if (endAt <= startAt) endAt = endAt.AddDays(1);
        var source = !string.IsNullOrWhiteSpace(employee.AssignedShiftTemplateCode)
            ? "EMP_TEMPLATE"
            : (!string.IsNullOrWhiteSpace(templateCode) && templateCode == config.DefaultShiftTemplateCode ? "ORG_DEFAULT" : "DEPT_TEMPLATE");
        return (true, startAt, endAt, source, template.FixedGraceMinutes ?? 0, template.Name);
    }

    private async Task<string?> ResolveAssignedShiftNameAsync(int orgId, TimeSpan assignmentStart, TimeSpan assignmentEnd, DayOfWeek dayOfWeek)
    {
        var dayCode = dayOfWeek.ToString()[..3].ToUpperInvariant();
        var templates = await _db.OrgShiftTemplates.AsNoTracking()
            .Include(t => t.WorkDays)
            .Where(t => t.OrgID == orgId && t.IsActive && t.FixedStartTime != null && t.FixedEndTime != null)
            .ToListAsync();

        foreach (var template in templates)
        {
            if (!TimeSpan.TryParse(template.FixedStartTime, out var templateStart) || !TimeSpan.TryParse(template.FixedEndTime, out var templateEnd))
                continue;

            if (templateStart != assignmentStart || templateEnd != assignmentEnd)
                continue;

            if (template.WorkDays.Count > 0 && !template.WorkDays.Any(x => x.DayCode.Trim().ToUpperInvariant().StartsWith(dayCode, StringComparison.Ordinal)))
                continue;

            return template.Name;
        }

        return null;
    }

    private async Task ApplyAutoClockOutForEmployeeAsync(int orgId, int empId, OrgAttendanceConfig config, OrgAttendanceOvertimePolicy policy, Employee employee)
    {
        if (!config.AutoClockOutEnabled || !config.AutoClockOutMaxHours.HasValue || config.AutoClockOutMaxHours.Value <= 0)
            return;

        var now = DateTime.UtcNow;
        var max = TimeSpan.FromHours((double)config.AutoClockOutMaxHours.Value);
        var openLogs = await _db.AttendanceLogs
            .Where(x => x.OrgID == orgId && x.EmpID == empId && x.TimeIn.HasValue && !x.TimeOut.HasValue)
            .ToListAsync();
        if (openLogs.Count == 0)
            return;

        foreach (var row in openLogs)
        {
            if (!row.TimeIn.HasValue)
                continue;
            var autoOut = row.TimeIn.Value.Add(max);
            if (autoOut > now)
                continue;

            row.TimeOut = autoOut;
            row.MethodOut = "AUTO";
            row.Remarks = string.IsNullOrWhiteSpace(row.Remarks)
                ? "Auto clock-out applied by organization policy."
                : row.Remarks + " | Auto clock-out applied by organization policy.";

            var shift = await ResolveEffectiveShiftAsync(orgId, employee, row.LogDate.ToDateTime(TimeOnly.MinValue), config);
            if (shift.ok)
                await UpsertAutoOvertimeAsync(orgId, empId, shift.end, autoOut, "AUTO", policy);
        }

        await _db.SaveChangesAsync();
    }

    private async Task UpsertAutoOvertimeAsync(int orgId, int empId, DateTime shiftEnd, DateTime actualEnd, string methodOut, OrgAttendanceOvertimePolicy policy)
    {
        var overtimeMinutes = ComputeOvertimeMinutes(policy, shiftEnd, actualEnd);
        if (overtimeMinutes <= 0)
            return;

        var hours = Math.Round((decimal)overtimeMinutes / 60m, 2);
        var date = DateOnly.FromDateTime(actualEnd);
        var status = policy.AutoApprove ? "Approved" : (string.Equals(methodOut, "AUTO", StringComparison.OrdinalIgnoreCase) ? "NeedsReview" : "SystemGenerated");

        var existing = await _db.OvertimeRequests
            .FirstOrDefaultAsync(x => x.OrgID == orgId && x.EmpID == empId && x.OTDate == date && (x.Status == "Pending" || x.Status == "SystemGenerated" || x.Status == "NeedsReview"));
        if (existing is null)
        {
            _db.OvertimeRequests.Add(new OvertimeRequest
            {
                OrgID = orgId,
                EmpID = empId,
                OTDate = date,
                StartTime = shiftEnd,
                EndTime = actualEnd,
                TotalHours = hours,
                Reason = $"AUTO: Computed from attendance clock-out ({methodOut}).",
                Status = status,
                ApprovedAt = status == "Approved" ? DateTime.UtcNow : null,
                ApprovedByUserID = status == "Approved" ? GetUserId() : null
            });
            return;
        }

        existing.StartTime = shiftEnd;
        existing.EndTime = actualEnd;
        existing.TotalHours = hours;
        existing.Reason = $"AUTO: Computed from attendance clock-out ({methodOut}).";
        existing.Status = status;
        existing.ApprovedAt = status == "Approved" ? DateTime.UtcNow : null;
        existing.ApprovedByUserID = status == "Approved" ? GetUserId() : null;
    }

    private static int ComputeOvertimeMinutes(OrgAttendanceOvertimePolicy policy, DateTime shiftEnd, DateTime actualEnd)
    {
        if (!policy.Enabled)
            return 0;
        var raw = (actualEnd - shiftEnd).TotalMinutes - policy.MicroOtBufferMinutes;
        if (raw <= 0)
            return 0;

        var rounded = ApplyRounding(raw, policy.RoundingMinutes, policy.RoundingDirection);
        if (rounded < Math.Max(1, policy.MinimumBlockMinutes))
            return 0;
        return rounded;
    }

    private static int ApplyRounding(double minutes, int roundingMinutes, string? direction)
    {
        if (roundingMinutes <= 0)
            return (int)Math.Round(minutes, MidpointRounding.AwayFromZero);

        var blocks = minutes / roundingMinutes;
        var mode = (direction ?? "NEAREST").Trim().ToUpperInvariant();
        var roundedBlocks = mode switch
        {
            "UP" => Math.Ceiling(blocks),
            "DOWN" => Math.Floor(blocks),
            _ => Math.Round(blocks, MidpointRounding.AwayFromZero)
        };
        return (int)(roundedBlocks * roundingMinutes);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static string? BuildPunchRemarks(bool testMode, string? timingTag)
    {
        var parts = new List<string>();
        if (testMode)
            parts.Add("AUTO_TEST: Rapid punch mode enabled.");
        if (!string.IsNullOrWhiteSpace(timingTag))
            parts.Add(timingTag);
        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    private static string ComputeDayStatus(bool hasTimeIn, bool hasTimeOut, int lateMinutes, int undertimeMinutes, int overtimeMinutes)
    {
        if (!hasTimeIn)
            return "No Record";
        if (!hasTimeOut)
            return "Timed In";
        if (lateMinutes > 0 && undertimeMinutes > 0)
            return "Late-In + Undertime";
        if (undertimeMinutes > 0)
            return "Undertime";
        if (lateMinutes > 0)
            return "Late-In";
        if (overtimeMinutes > 0)
            return "Overtime";
        return "Present";
    }

    private static DateTime ResolveCaptureTimestamp(string? deviceTimestamp)
    {
        if (!string.IsNullOrWhiteSpace(deviceTimestamp))
        {
            if (DateTime.TryParse(deviceTimestamp, out var parsedDateTime))
                return DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Unspecified);

            if (DateTimeOffset.TryParse(deviceTimestamp, out var parsedOffset))
                return DateTime.SpecifyKind(parsedOffset.DateTime, DateTimeKind.Unspecified);
        }
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }

    public class MyScheduleItemDto
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? SiteName { get; set; }
        public string ShiftName { get; set; } = "No shift assigned";
        public DateTime? ShiftStart { get; set; }
        public DateTime? ShiftEnd { get; set; }
        public string Source { get; set; } = "ASSIGNMENT";
        public DateTime? ClockInFrom { get; set; }
        public DateTime? ClockInUntil { get; set; }
    }

    public class MyAttendanceItemDto
    {
        public int AttendanceID { get; set; }
        public DateOnly LogDate { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public string? MethodIn { get; set; }
        public string? MethodOut { get; set; }
        public string? Remarks { get; set; }
        public int WorkedMinutes { get; set; }
        public int LateMinutes { get; set; }
        public int UndertimeMinutes { get; set; }
        public int OvertimeMinutes { get; set; }
        public string Status { get; set; } = "Incomplete";
    }

    public class MyAssignedShiftItemDto
    {
        public int ShiftAssignmentID { get; set; }
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string ShiftName { get; set; } = "Assigned Shift";
        public DateTime ShiftStart { get; set; }
        public DateTime ShiftEnd { get; set; }
        public string Source { get; set; } = "ASSIGNMENT";
    }

    public class PunchRequest
    {
        public string Action { get; set; } = "IN";
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? DeviceInfo { get; set; }
        public string? DeviceTimestamp { get; set; }
        public bool TestMode { get; set; }
    }
}
