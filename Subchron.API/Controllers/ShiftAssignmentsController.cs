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
public class ShiftAssignmentsController : ControllerBase
{
    private readonly TenantDbContext _db;

    public ShiftAssignmentsController(TenantDbContext db)
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

    private bool CanAccessShiftSchedule()
    {
        return RoleModuleAccess.CanAccessModule(User, AppModule.ShiftSchedule);
    }

    // List shift assignments for a date range. Optional filter by department or employee.
    [HttpGet]
    public async Task<ActionResult<List<ShiftAssignmentDto>>> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId,
        [FromQuery] int? empId)
    {
        if (!CanAccessShiftSchedule())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var fromDate = from?.Date ?? DateTime.UtcNow.Date;
        var toDate = to?.Date ?? fromDate.AddMonths(1);

        var query = _db.ShiftAssignments.AsNoTracking()
            .Include(s => s.Employee)
            .Where(s => s.OrgID == orgId.Value && s.AssignmentDate >= fromDate && s.AssignmentDate <= toDate);

        if (empId.HasValue)
            query = query.Where(s => s.EmpID == empId.Value);
        if (departmentId.HasValue)
            query = query.Where(s => s.Employee.DepartmentID == departmentId.Value);

        var list = await query
            .OrderBy(s => s.AssignmentDate)
            .ThenBy(s => s.StartTime)
            .Select(s => new ShiftAssignmentDto
            {
                ShiftAssignmentID = s.ShiftAssignmentID,
                OrgID = s.OrgID,
                EmpID = s.EmpID,
                EmployeeName = s.Employee.FirstName + " " + s.Employee.LastName,
                EmpNumber = s.Employee.EmpNumber,
                DepartmentID = s.Employee.DepartmentID,
                AssignmentDate = s.AssignmentDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    // Get assignments for a single date (for day-detail view). 
    [HttpGet("by-date")]
    public async Task<ActionResult<List<ShiftAssignmentDto>>> ByDate(
        [FromQuery] DateTime date,
        [FromQuery] int? departmentId)
    {
        if (!CanAccessShiftSchedule())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var d = date.Date;
        var query = _db.ShiftAssignments.AsNoTracking()
            .Include(s => s.Employee)
            .Where(s => s.OrgID == orgId.Value && s.AssignmentDate == d);

        if (departmentId.HasValue)
            query = query.Where(s => s.Employee.DepartmentID == departmentId.Value);

        var list = await query
            .OrderBy(s => s.StartTime)
            .Select(s => new ShiftAssignmentDto
            {
                ShiftAssignmentID = s.ShiftAssignmentID,
                OrgID = s.OrgID,
                EmpID = s.EmpID,
                EmployeeName = s.Employee.FirstName + " " + s.Employee.LastName,
                EmpNumber = s.Employee.EmpNumber,
                DepartmentID = s.Employee.DepartmentID,
                AssignmentDate = s.AssignmentDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    // Create a shift assignment.
    [HttpPost]
    public async Task<ActionResult<ShiftAssignmentDto>> Create([FromBody] CreateShiftRequest req)
    {
        if (!CanAccessShiftSchedule())
            return Forbid();

        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpID == req.EmpID && e.OrgID == orgId.Value && !e.IsArchived);
        if (emp == null)
            return BadRequest(new { ok = false, message = "Employee not found or not active." });

        var assignment = new ShiftAssignment
        {
            OrgID = orgId.Value,
            EmpID = req.EmpID,
            AssignmentDate = req.AssignmentDate.Date,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            Notes = req.Notes?.Trim(),
            CreatedByUserID = userId
        };
        _db.ShiftAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = assignment.ShiftAssignmentID }, new ShiftAssignmentDto
        {
            ShiftAssignmentID = assignment.ShiftAssignmentID,
            OrgID = assignment.OrgID,
            EmpID = assignment.EmpID,
            EmployeeName = emp.FirstName + " " + emp.LastName,
            EmpNumber = emp.EmpNumber,
            DepartmentID = emp.DepartmentID,
            AssignmentDate = assignment.AssignmentDate,
            StartTime = assignment.StartTime,
            EndTime = assignment.EndTime,
            Notes = assignment.Notes,
            CreatedAt = assignment.CreatedAt
        });
    }

    // Get a single shift assignment.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShiftAssignmentDto>> Get(int id)
    {
        if (!CanAccessShiftSchedule())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var s = await _db.ShiftAssignments.AsNoTracking()
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.ShiftAssignmentID == id && s.OrgID == orgId.Value);
        if (s == null)
            return NotFound();

        return Ok(new ShiftAssignmentDto
        {
            ShiftAssignmentID = s.ShiftAssignmentID,
            OrgID = s.OrgID,
            EmpID = s.EmpID,
            EmployeeName = s.Employee.FirstName + " " + s.Employee.LastName,
            EmpNumber = s.Employee.EmpNumber,
            DepartmentID = s.Employee.DepartmentID,
            AssignmentDate = s.AssignmentDate,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt
        });
    }

    // Update a shift assignment.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShiftRequest req)
    {
        if (!CanAccessShiftSchedule())
            return Forbid();

        var orgId = GetUserOrgId();
        var userId = GetUserId();
        if (!orgId.HasValue)
            return Forbid();

        var s = await _db.ShiftAssignments.FirstOrDefaultAsync(s => s.ShiftAssignmentID == id && s.OrgID == orgId.Value);
        if (s == null)
            return NotFound();

        if (req.EmpID.HasValue)
        {
            var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpID == req.EmpID.Value && e.OrgID == orgId.Value && !e.IsArchived);
            if (emp == null)
                return BadRequest(new { ok = false, message = "Employee not found or not active." });
            s.EmpID = req.EmpID.Value;
        }
        if (req.AssignmentDate.HasValue) s.AssignmentDate = req.AssignmentDate.Value.Date;
        if (req.StartTime.HasValue) s.StartTime = req.StartTime.Value;
        if (req.EndTime.HasValue) s.EndTime = req.EndTime.Value;
        if (req.Notes != null) s.Notes = req.Notes.Trim();
        s.UpdatedAt = DateTime.UtcNow;
        s.UpdatedByUserID = userId;
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // Delete a shift assignment.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CanAccessShiftSchedule())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var s = await _db.ShiftAssignments.FirstOrDefaultAsync(s => s.ShiftAssignmentID == id && s.OrgID == orgId.Value);
        if (s == null)
            return NotFound();

        _db.ShiftAssignments.Remove(s);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    public class CreateShiftRequest
    {
        public int EmpID { get; set; }
        public DateTime AssignmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateShiftRequest
    {
        public int? EmpID { get; set; }
        public DateTime? AssignmentDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? Notes { get; set; }
    }

    public class ShiftAssignmentDto
    {
        public int ShiftAssignmentID { get; set; }
        public int OrgID { get; set; }
        public int EmpID { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmpNumber { get; set; }
        public int? DepartmentID { get; set; }
        public DateTime AssignmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
