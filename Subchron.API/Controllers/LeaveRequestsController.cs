using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Authorization;
using Subchron.API.Data;
using Subchron.API.Models.Entities;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly TenantDbContext _db;

    public LeaveRequestsController(TenantDbContext db)
    {
        _db = db;
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

    private bool CanAccessLeaveManagement()
    {
        return RoleModuleAccess.CanAccessModule(User, AppModule.LeaveManagement);
    }

    // List leave requests with optional filters. Requires LeaveManagement module access.
    [HttpGet]
    public async Task<ActionResult<PagedLeaveResult>> List(
        [FromQuery] string? status,
        [FromQuery] int? empId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var query = _db.LeaveRequests.AsNoTracking()
            .Include(lr => lr.Employee)
            .Where(lr => lr.OrgID == orgId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(lr => lr.Status == status.Trim());
        if (empId.HasValue)
            query = query.Where(lr => lr.EmpID == empId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(lr =>
                EF.Functions.Like(lr.Employee.FirstName, term) ||
                EF.Functions.Like(lr.Employee.LastName, term) ||
                EF.Functions.Like(lr.Employee.EmpNumber, term) ||
                (lr.Reason != null && EF.Functions.Like(lr.Reason, term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(lr => lr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(Math.Min(Math.Max(pageSize, 1), 100))
            .Select(lr => new LeaveRequestDto
            {
                LeaveRequestID = lr.LeaveRequestID,
                OrgID = lr.OrgID,
                EmpID = lr.EmpID,
                EmployeeName = lr.Employee.FirstName + " " + lr.Employee.LastName,
                EmpNumber = lr.Employee.EmpNumber,
                LeaveType = lr.LeaveType,
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                Status = lr.Status,
                Reason = lr.Reason,
                ReviewedByUserName = null,
                ReviewedAt = lr.ReviewedAt,
                ReviewNotes = lr.ReviewNotes,
                CreatedAt = lr.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedLeaveResult { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    // Create a leave request (e.g. submitted by employee or HR).
    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Create([FromBody] CreateLeaveRequestDto req)
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpID == req.EmpID && e.OrgID == orgId.Value && !e.IsArchived);
        if (emp == null)
            return BadRequest(new { ok = false, message = "Employee not found or not active." });

        var leaveType = string.IsNullOrWhiteSpace(req.LeaveType) ? "Vacation" : req.LeaveType.Trim().Length > 40 ? req.LeaveType.Trim()[..40] : req.LeaveType.Trim();
        var lr = new LeaveRequest
        {
            OrgID = orgId.Value,
            EmpID = req.EmpID,
            LeaveType = leaveType,
            StartDate = req.StartDate.Date,
            EndDate = req.EndDate.Date,
            Status = "Pending",
            Reason = req.Reason?.Trim(),
            CreatedByUserID = userId
        };
        _db.LeaveRequests.Add(lr);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = lr.LeaveRequestID }, new LeaveRequestDto
        {
            LeaveRequestID = lr.LeaveRequestID,
            OrgID = lr.OrgID,
            EmpID = lr.EmpID,
            EmployeeName = emp.FirstName + " " + emp.LastName,
            EmpNumber = emp.EmpNumber,
            LeaveType = lr.LeaveType,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            Status = lr.Status,
            Reason = lr.Reason,
            CreatedAt = lr.CreatedAt
        });
    }

    // Get a single leave request by ID.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveRequestDto>> Get(int id)
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var lr = await _db.LeaveRequests.AsNoTracking()
            .Include(lr => lr.Employee)
            .FirstOrDefaultAsync(lr => lr.LeaveRequestID == id && lr.OrgID == orgId.Value);
        if (lr == null)
            return NotFound();

        return Ok(new LeaveRequestDto
        {
            LeaveRequestID = lr.LeaveRequestID,
            OrgID = lr.OrgID,
            EmpID = lr.EmpID,
            EmployeeName = lr.Employee.FirstName + " " + lr.Employee.LastName,
            EmpNumber = lr.Employee.EmpNumber,
            LeaveType = lr.LeaveType,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            Status = lr.Status,
            Reason = lr.Reason,
            ReviewedByUserName = null,
            ReviewedAt = lr.ReviewedAt,
            ReviewNotes = lr.ReviewNotes,
            CreatedAt = lr.CreatedAt
        });
    }

    // Approve a leave request.
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ReviewBody? body)
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue || !userId.HasValue)
            return Forbid();

        var lr = await _db.LeaveRequests.FirstOrDefaultAsync(lr => lr.LeaveRequestID == id && lr.OrgID == orgId.Value);
        if (lr == null)
            return NotFound();
        if (lr.Status != "Pending")
            return BadRequest(new { ok = false, message = "Request is no longer pending." });

        lr.Status = "Approved";
        lr.ReviewedByUserID = userId;
        lr.ReviewedAt = DateTime.UtcNow;
        lr.ReviewNotes = body?.Notes?.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { ok = true, message = "Leave request approved." });
    }

    // Decline a leave request.
    [HttpPost("{id:int}/decline")]
    public async Task<IActionResult> Decline(int id, [FromBody] ReviewBody? body)
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue || !userId.HasValue)
            return Forbid();

        var lr = await _db.LeaveRequests.FirstOrDefaultAsync(lr => lr.LeaveRequestID == id && lr.OrgID == orgId.Value);
        if (lr == null)
            return NotFound();
        if (lr.Status != "Pending")
            return BadRequest(new { ok = false, message = "Request is no longer pending." });

        lr.Status = "Declined";
        lr.ReviewedByUserID = userId;
        lr.ReviewedAt = DateTime.UtcNow;
        lr.ReviewNotes = body?.Notes?.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { ok = true, message = "Leave request declined." });
    }

    public class ReviewBody
    {
        public string? Notes { get; set; }
    }

    public class LeaveRequestDto
    {
        public int LeaveRequestID { get; set; }
        public int OrgID { get; set; }
        public int EmpID { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmpNumber { get; set; }
        public string LeaveType { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "";
        public string? Reason { get; set; }
        public string? ReviewedByUserName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PagedLeaveResult
    {
        public List<LeaveRequestDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class CreateLeaveRequestDto
    {
        public int EmpID { get; set; }
        public string? LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
    }
}
