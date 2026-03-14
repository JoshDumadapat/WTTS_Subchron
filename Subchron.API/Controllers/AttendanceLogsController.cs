using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;

namespace Subchron.API.Controllers;

[ApiController]
[Authorize]
[Route("api/attendance-logs")]
public class AttendanceLogsController : ControllerBase
{
    private readonly TenantDbContext _db;

    public AttendanceLogsController(TenantDbContext db)
    {
        _db = db;
    }

    [HttpGet("current")]
    public async Task<ActionResult<List<AttendanceLogItemDto>>> Current([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? departmentId, [FromQuery] int? empId, [FromQuery] string? method)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var fromDate = DateOnly.FromDateTime((from?.Date ?? DateTime.UtcNow.Date.AddDays(-14)));
        var toDate = DateOnly.FromDateTime((to?.Date ?? DateTime.UtcNow.Date));
        if (toDate < fromDate)
            (fromDate, toDate) = (toDate, fromDate);

        var query = _db.AttendanceLogs.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.LogDate >= fromDate && x.LogDate <= toDate)
            .Join(_db.Employees.AsNoTracking(), a => a.EmpID, e => e.EmpID, (a, e) => new { a, e });

        if (departmentId.HasValue)
            query = query.Where(x => x.e.DepartmentID == departmentId.Value);
        if (empId.HasValue)
            query = query.Where(x => x.e.EmpID == empId.Value);
        if (!string.IsNullOrWhiteSpace(method))
        {
            var m = method.Trim().ToUpperInvariant();
            query = query.Where(x => ((x.a.MethodOut ?? x.a.MethodIn ?? string.Empty).ToUpper()) == m);
        }

        var rawRows = await query
            .OrderByDescending(x => x.a.LogDate)
            .ThenByDescending(x => x.a.TimeIn)
            .Select(x => new AttendanceLogRawDto
            {
                AttendanceID = x.a.AttendanceID,
                EmpID = x.e.EmpID,
                EmployeeName = (x.e.FirstName + " " + x.e.LastName).Trim(),
                EmpNumber = x.e.EmpNumber,
                DepartmentID = x.e.DepartmentID,
                AssignedShiftTemplateCode = x.e.AssignedShiftTemplateCode,
                DepartmentName = _db.Departments.Where(d => d.DepID == x.e.DepartmentID).Select(d => d.DepartmentName).FirstOrDefault(),
                LogDate = x.a.LogDate,
                TimeIn = x.a.TimeIn,
                TimeOut = x.a.TimeOut,
                MethodIn = x.a.MethodIn,
                MethodOut = x.a.MethodOut,
                Method = x.a.MethodOut ?? x.a.MethodIn,
                Station = ExtractStationName(x.a.Remarks),
                Remarks = x.a.Remarks,
                Status = x.a.TimeIn.HasValue && x.a.TimeOut.HasValue ? "Present" : "Timed In"
            })
            .ToListAsync();

        var overtimeMap = await _db.OvertimeRequests.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.OTDate >= fromDate && x.OTDate <= toDate)
            .GroupBy(x => new { x.EmpID, x.OTDate })
            .Select(g => new
            {
                g.Key.EmpID,
                g.Key.OTDate,
                Minutes = (int)Math.Round(g.Sum(x => x.TotalHours) * 60m, MidpointRounding.AwayFromZero)
            })
            .ToListAsync();
        var overtimeByDay = overtimeMap.ToDictionary(x => (x.EmpID, x.OTDate), x => x.Minutes);

        var config = await _db.OrgAttendanceConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value)
            ?? new Models.Entities.OrgAttendanceConfig
            {
                OrgID = orgId.Value,
                EarliestClockInMinutes = 0,
                LatestClockInMinutes = 0,
                UseGracePeriodForLate = false,
                MarkUndertimeBasedOnSchedule = true
            };
        var policy = await _db.OrgAttendanceOvertimePolicies.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value)
            ?? new Models.Entities.OrgAttendanceOvertimePolicy { OrgID = orgId.Value, Enabled = false };

        var rows = new List<AttendanceLogItemDto>();
        foreach (var group in rawRows.GroupBy(x => new { x.EmpID, x.LogDate }))
        {
            var latest = group.OrderByDescending(x => x.AttendanceID).First();
            var hasTimeIn = group.Any(x => x.TimeIn.HasValue);
            var hasTimeOut = group.Any(x => x.TimeOut.HasValue);
            var timeIn = hasTimeIn ? group.Where(x => x.TimeIn.HasValue).Min(x => x.TimeIn!.Value) : (DateTime?)null;
            var timeOut = hasTimeOut ? group.Where(x => x.TimeOut.HasValue).Max(x => x.TimeOut!.Value) : (DateTime?)null;

            var workedMinutes = hasTimeIn && hasTimeOut
                ? (int)Math.Max(0, Math.Floor((timeOut!.Value - timeIn!.Value).TotalMinutes))
                : 0;

            var logDateAsDate = group.Key.LogDate.ToDateTime(TimeOnly.MinValue);
            var shift = await ResolveEffectiveShiftAsync(
                orgId.Value,
                group.Key.EmpID,
                logDateAsDate,
                config,
                latest.DepartmentID,
                latest.AssignedShiftTemplateCode);

            var lateMinutes = 0;
            var undertimeMinutes = 0;
            var overtimeMinutes = overtimeByDay.TryGetValue((group.Key.EmpID, group.Key.LogDate), out var otFromReq)
                ? otFromReq
                : 0;

            if (timeIn.HasValue && shift.ok)
            {
                var lateBase = shift.start;
                if (config.UseGracePeriodForLate)
                    lateBase = lateBase.AddMinutes(shift.graceMinutes);
                lateMinutes = (int)Math.Max(0, Math.Floor((timeIn.Value - lateBase).TotalMinutes));
            }

            if (timeOut.HasValue && shift.ok && config.MarkUndertimeBasedOnSchedule)
            {
                undertimeMinutes = (int)Math.Max(0, Math.Floor((shift.end - timeOut.Value).TotalMinutes));
            }

            if (timeOut.HasValue && shift.ok)
            {
                var computedOt = ComputeOvertimeMinutes(policy, shift.end, timeOut.Value);
                if (computedOt > overtimeMinutes)
                    overtimeMinutes = computedOt;
            }

            var status = ComputeDayStatus(hasTimeIn, hasTimeOut, lateMinutes, undertimeMinutes, overtimeMinutes);

            rows.Add(new AttendanceLogItemDto
            {
                AttendanceID = latest.AttendanceID,
                EmpID = latest.EmpID,
                EmployeeName = latest.EmployeeName,
                EmpNumber = latest.EmpNumber,
                DepartmentName = latest.DepartmentName,
                LogDate = latest.LogDate,
                TimeIn = hasTimeIn ? timeIn : null,
                TimeOut = hasTimeOut ? timeOut : null,
                Method = latest.Method,
                Station = latest.Station,
                Remarks = latest.Remarks,
                Status = status,
                WorkedMinutes = workedMinutes,
                LateMinutes = lateMinutes,
                UndertimeMinutes = undertimeMinutes,
                OvertimeMinutes = overtimeMinutes
            });
        }

        rows = rows
            .OrderByDescending(x => x.LogDate)
            .ThenBy(x => x.EmployeeName)
            .ToList();

        return Ok(rows);
    }

    [HttpGet("current/{attendanceId:int}")]
    public async Task<ActionResult<AttendanceLogDetailDto>> CurrentDetail(int attendanceId)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var target = await _db.AttendanceLogs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.AttendanceID == attendanceId);
        if (target is null)
            return NotFound(new { ok = false, message = "Attendance log not found." });

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.EmpID == target.EmpID);
        if (employee is null)
            return NotFound(new { ok = false, message = "Employee not found." });

        var departmentName = await _db.Departments.AsNoTracking()
            .Where(d => d.DepID == employee.DepartmentID)
            .Select(d => d.DepartmentName)
            .FirstOrDefaultAsync();

        var dayRows = await _db.AttendanceLogs.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.EmpID == target.EmpID && x.LogDate == target.LogDate)
            .OrderByDescending(x => x.AttendanceID)
            .ToListAsync();

        var hasTimeIn = dayRows.Any(x => x.TimeIn.HasValue);
        var hasTimeOut = dayRows.Any(x => x.TimeOut.HasValue);
        var timeIn = hasTimeIn ? dayRows.Where(x => x.TimeIn.HasValue).Min(x => x.TimeIn!.Value) : (DateTime?)null;
        var timeOut = hasTimeOut ? dayRows.Where(x => x.TimeOut.HasValue).Max(x => x.TimeOut!.Value) : (DateTime?)null;
        var workedMinutes = hasTimeIn && hasTimeOut
            ? (int)Math.Max(0, Math.Floor((timeOut!.Value - timeIn!.Value).TotalMinutes))
            : 0;

        var config = await _db.OrgAttendanceConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value)
            ?? new Models.Entities.OrgAttendanceConfig
            {
                OrgID = orgId.Value,
                EarliestClockInMinutes = 0,
                LatestClockInMinutes = 0,
                UseGracePeriodForLate = false,
                MarkUndertimeBasedOnSchedule = true
            };
        var policy = await _db.OrgAttendanceOvertimePolicies.AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == orgId.Value)
            ?? new Models.Entities.OrgAttendanceOvertimePolicy { OrgID = orgId.Value, Enabled = false };

        var shift = await ResolveEffectiveShiftAsync(orgId.Value, employee.EmpID, target.LogDate.ToDateTime(TimeOnly.MinValue), config, employee.DepartmentID, employee.AssignedShiftTemplateCode);
        var lateMinutes = 0;
        var undertimeMinutes = 0;
        var overtimeMinutes = 0;

        if (timeIn.HasValue && shift.ok)
        {
            var lateBase = shift.start;
            if (config.UseGracePeriodForLate)
                lateBase = lateBase.AddMinutes(shift.graceMinutes);
            lateMinutes = (int)Math.Max(0, Math.Floor((timeIn.Value - lateBase).TotalMinutes));
        }

        if (timeOut.HasValue && shift.ok && config.MarkUndertimeBasedOnSchedule)
            undertimeMinutes = (int)Math.Max(0, Math.Floor((shift.end - timeOut.Value).TotalMinutes));

        if (timeOut.HasValue && shift.ok)
            overtimeMinutes = ComputeOvertimeMinutes(policy, shift.end, timeOut.Value);

        var overtimeFromRequests = await _db.OvertimeRequests.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.EmpID == employee.EmpID && x.OTDate == target.LogDate)
            .SumAsync(x => (decimal?)x.TotalHours) ?? 0m;
        var overtimeFromRequestMinutes = (int)Math.Round(overtimeFromRequests * 60m, MidpointRounding.AwayFromZero);
        if (overtimeFromRequestMinutes > overtimeMinutes)
            overtimeMinutes = overtimeFromRequestMinutes;

        var status = ComputeDayStatus(hasTimeIn, hasTimeOut, lateMinutes, undertimeMinutes, overtimeMinutes);

        return Ok(new AttendanceLogDetailDto
        {
            AttendanceID = attendanceId,
            EmpID = employee.EmpID,
            EmployeeName = (employee.FirstName + " " + employee.LastName).Trim(),
            EmpNumber = employee.EmpNumber,
            DepartmentName = departmentName,
            LogDate = target.LogDate,
            TimeIn = timeIn,
            TimeOut = timeOut,
            WorkedMinutes = workedMinutes,
            LateMinutes = lateMinutes,
            UndertimeMinutes = undertimeMinutes,
            OvertimeMinutes = overtimeMinutes,
            Status = status,
            Entries = dayRows.Select(x => new AttendanceLogEntryDto
            {
                AttendanceID = x.AttendanceID,
                TimeIn = x.TimeIn,
                TimeOut = x.TimeOut,
                MethodIn = x.MethodIn,
                MethodOut = x.MethodOut,
                Station = ExtractStationName(x.Remarks),
                Remarks = x.Remarks,
                Status = x.TimeIn.HasValue && x.TimeOut.HasValue ? "Present" : "Timed In"
            }).ToList()
        });
    }

    [HttpGet("current/export.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? departmentId, [FromQuery] int? empId, [FromQuery] string? method)
    {
        var dataResult = await Current(from, to, departmentId, empId, method);
        if (dataResult.Result is ForbidResult)
            return Forbid();

        var rows = dataResult.Value ?? new List<AttendanceLogItemDto>();
        var sb = new StringBuilder();
        sb.AppendLine("AttendanceID,Employee,EmpNumber,Department,Date,TimeIn,TimeOut,Method,Station,Status,Remarks");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",",
                Csv(r.AttendanceID.ToString()),
                Csv(r.EmployeeName),
                Csv(r.EmpNumber),
                Csv(r.DepartmentName),
                Csv(r.LogDate.ToString("yyyy-MM-dd")),
                Csv(r.TimeIn?.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(r.TimeOut?.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(r.Method),
                Csv(r.Station),
                Csv(r.Status),
                Csv(r.Remarks)));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"attendance-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private int? GetUserOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static string? ExtractStationName(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
            return null;
        var marker = "Station:";
        var idx = remarks.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;
        var value = remarks[(idx + marker.Length)..].Trim();
        var sep = value.IndexOf('|');
        if (sep >= 0)
            value = value[..sep].Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string Csv(string? value)
    {
        var v = value ?? string.Empty;
        var escaped = v.Replace("\"", "\"\"");
        return "\"" + escaped + "\"";
    }

    private async Task<(bool ok, DateTime start, DateTime end, int graceMinutes)> ResolveEffectiveShiftAsync(
        int orgId,
        int empId,
        DateTime date,
        Models.Entities.OrgAttendanceConfig config,
        int? departmentId,
        string? assignedShiftTemplateCode)
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

        var templateCode = !string.IsNullOrWhiteSpace(assignedShiftTemplateCode)
            ? assignedShiftTemplateCode
            : await _db.Departments.AsNoTracking()
                .Where(d => departmentId.HasValue && d.DepID == departmentId.Value)
                .Select(d => d.DefaultShiftTemplateCode)
                .FirstOrDefaultAsync();

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

    private static int ComputeOvertimeMinutes(Models.Entities.OrgAttendanceOvertimePolicy policy, DateTime shiftEnd, DateTime actualEnd)
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

    private class AttendanceLogRawDto
    {
        public int AttendanceID { get; set; }
        public int EmpID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmpNumber { get; set; }
        public int? DepartmentID { get; set; }
        public string? AssignedShiftTemplateCode { get; set; }
        public string? DepartmentName { get; set; }
        public DateOnly LogDate { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public string? MethodIn { get; set; }
        public string? MethodOut { get; set; }
        public string? Method { get; set; }
        public string? Station { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Timed In";
    }

    public class AttendanceLogItemDto
    {
        public int AttendanceID { get; set; }
        public int EmpID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmpNumber { get; set; }
        public string? DepartmentName { get; set; }
        public DateOnly LogDate { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public string? Method { get; set; }
        public string? Station { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Timed In";
        public int WorkedMinutes { get; set; }
        public int LateMinutes { get; set; }
        public int UndertimeMinutes { get; set; }
        public int OvertimeMinutes { get; set; }
    }

    public class AttendanceLogDetailDto
    {
        public int AttendanceID { get; set; }
        public int EmpID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmpNumber { get; set; }
        public string? DepartmentName { get; set; }
        public DateOnly LogDate { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public int WorkedMinutes { get; set; }
        public int LateMinutes { get; set; }
        public int UndertimeMinutes { get; set; }
        public int OvertimeMinutes { get; set; }
        public string Status { get; set; } = "Timed In";
        public List<AttendanceLogEntryDto> Entries { get; set; } = new();
    }

    public class AttendanceLogEntryDto
    {
        public int AttendanceID { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public string? MethodIn { get; set; }
        public string? MethodOut { get; set; }
        public string? Station { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Timed In";
    }
}
