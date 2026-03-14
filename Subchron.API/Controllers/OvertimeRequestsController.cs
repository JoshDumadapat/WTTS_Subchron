using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Services;

namespace Subchron.API.Controllers;

[ApiController]
[Authorize]
[Route("api/overtime-requests")]
public class OvertimeRequestsController : ControllerBase
{
    private readonly TenantDbContext _db;
    private readonly IAuditService _audit;

    public OvertimeRequestsController(TenantDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<OvertimeRequestDto>>> Mine()
    {
        var ctx = await ResolveContextAsync();
        if (!ctx.ok)
            return Forbid();
        if (ctx.empId <= 0)
            return Ok(new List<OvertimeRequestDto>());

        var rows = await QueryBase(ctx.orgId)
            .Where(x => x.EmpID == ctx.empId)
            .OrderByDescending(x => x.OTDate)
            .ThenByDescending(x => x.OTRequestID)
            .ToListAsync();
        return Ok(rows);
    }

    [HttpGet("queue")]
    public async Task<ActionResult<List<OvertimeRequestDto>>> Queue([FromQuery] string? status)
    {
        var ctx = await ResolveContextAsync();
        if (!ctx.ok)
            return Forbid();

        var query = QueryBase(ctx.orgId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status.Trim());

        var rows = await query.OrderByDescending(x => x.OTDate).ThenByDescending(x => x.OTRequestID).ToListAsync();
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOvertimeRequest req)
    {
        var ctx = await ResolveContextAsync();
        if (!ctx.ok)
            return Forbid();
        if (ctx.empId <= 0)
            return BadRequest(new { ok = false, message = "Employee profile not found for this account." });

        if (req is null || req.StartTime >= req.EndTime)
            return BadRequest(new { ok = false, message = "Invalid overtime range." });

        var totalHours = (decimal)(req.EndTime - req.StartTime).TotalHours;
        var row = new OvertimeRequest
        {
            OrgID = ctx.orgId,
            EmpID = ctx.empId,
            OTDate = DateOnly.FromDateTime(req.OTDate.Date),
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            TotalHours = Math.Round(totalHours, 2),
            Reason = string.IsNullOrWhiteSpace(req.Reason) ? "Manual request" : req.Reason.Trim(),
            Status = "Pending"
        };

        _db.OvertimeRequests.Add(row);
        await _db.SaveChangesAsync();
        await SafeAuditAsync(ctx.orgId, ctx.userId, "OvertimeRequestCreated", nameof(OvertimeRequest), row.OTRequestID, $"Employee #{ctx.empId} filed overtime request.");
        return Ok(new { ok = true, id = row.OTRequestID });
    }

    [HttpPost("{id:int}/action")]
    public async Task<IActionResult> Action(int id, [FromBody] OvertimeActionRequest req)
    {
        var ctx = await ResolveContextAsync();
        if (!ctx.ok)
            return Forbid();

        var row = await _db.OvertimeRequests.FirstOrDefaultAsync(x => x.OrgID == ctx.orgId && x.OTRequestID == id);
        if (row is null)
            return NotFound(new { ok = false, message = "Overtime request not found." });

        var action = (req.Action ?? string.Empty).Trim().ToLowerInvariant();
        if (action == "approve")
        {
            row.Status = "Approved";
            row.ApprovedAt = DateTime.UtcNow;
            row.ApprovedByUserID = ctx.userId;
        }
        else if (action == "reject")
        {
            row.Status = "Rejected";
            row.ApprovedAt = DateTime.UtcNow;
            row.ApprovedByUserID = ctx.userId;
        }
        else
        {
            return BadRequest(new { ok = false, message = "Unsupported action." });
        }

        await _db.SaveChangesAsync();
        await SafeAuditAsync(ctx.orgId, ctx.userId, "OvertimeRequestUpdated", nameof(OvertimeRequest), row.OTRequestID, $"Overtime request #{row.OTRequestID} set to {row.Status}.");
        return Ok(new { ok = true });
    }

    private IQueryable<OvertimeRequestDto> QueryBase(int orgId)
    {
        return _db.OvertimeRequests.AsNoTracking()
            .Where(x => x.OrgID == orgId)
            .Join(_db.Employees.AsNoTracking(), ot => ot.EmpID, e => e.EmpID, (ot, e) => new OvertimeRequestDto
            {
                OTRequestID = ot.OTRequestID,
                EmpID = ot.EmpID,
                EmployeeName = (e.FirstName + " " + e.LastName).Trim(),
                OTDate = ot.OTDate,
                StartTime = ot.StartTime,
                EndTime = ot.EndTime,
                TotalHours = ot.TotalHours,
                Reason = ot.Reason,
                Status = ot.Status,
                ApprovedAt = ot.ApprovedAt,
                Source = ot.Reason != null && ot.Reason.StartsWith("AUTO:", StringComparison.OrdinalIgnoreCase) ? "AUTO" : "MANUAL"
            });
    }

    private async Task<(bool ok, int orgId, int? userId, int empId)> ResolveContextAsync()
    {
        var orgClaim = User.FindFirstValue("orgId");
        var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(orgClaim, out var orgId) || !int.TryParse(userClaim, out var userId))
            return (false, 0, null, 0);

        var empId = await _db.Employees.AsNoTracking()
            .Where(e => e.OrgID == orgId && e.UserID == userId && !e.IsArchived)
            .Select(e => e.EmpID)
            .FirstOrDefaultAsync();

        return (true, orgId, userId, empId);
    }

    private async Task SafeAuditAsync(int orgId, int? userId, string action, string entityName, int entityId, string description)
    {
        try
        {
            await _audit.LogTenantAsync(orgId, userId, action, entityName, entityId, description,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }
        catch
        {
            // no-op
        }
    }

    public class CreateOvertimeRequest
    {
        public DateTime OTDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Reason { get; set; }
    }

    public class OvertimeActionRequest
    {
        public string? Action { get; set; }
    }

    public class OvertimeRequestDto
    {
        public int OTRequestID { get; set; }
        public int EmpID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateOnly OTDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalHours { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? ApprovedAt { get; set; }
        public string Source { get; set; } = "MANUAL";
    }
}
