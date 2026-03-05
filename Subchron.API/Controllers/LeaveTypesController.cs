using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Authorization;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Models.LeaveTypes;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/leave-types")]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly TenantDbContext _db;

    public LeaveTypesController(TenantDbContext db)
    {
        _db = db;
    }

    private int? GetUserOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id) ? id : null;
    }

    private bool CanAccessLeaveManagement()
    {
        return RoleModuleAccess.CanAccessModule(User, AppModule.LeaveManagement);
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaveTypeDto>>> List()
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var items = await _db.LeaveTypes.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value)
            .OrderBy(x => x.LeaveTypeName)
            .Select(x => new LeaveTypeDto
            {
                LeaveTypeID = x.LeaveTypeID,
                OrgID = x.OrgID,
                LeaveTypeName = x.LeaveTypeName,
                DefaultDaysPerYear = x.DefaultDaysPerYear,
                IsPaid = x.IsPaid,
                IsActive = x.IsActive,
                AccrualType = x.AccrualType,
                CarryOverType = x.CarryOverType,
                CarryOverMaxDays = x.CarryOverMaxDays,
                AppliesTo = x.AppliesTo,
                RequireApproval = x.RequireApproval,
                RequireDocument = x.RequireDocument,
                AllowNegativeBalance = x.AllowNegativeBalance
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveTypeDto>> Get(int id)
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var item = await _db.LeaveTypes.AsNoTracking()
            .Where(x => x.OrgID == orgId.Value && x.LeaveTypeID == id)
            .Select(x => new LeaveTypeDto
            {
                LeaveTypeID = x.LeaveTypeID,
                OrgID = x.OrgID,
                LeaveTypeName = x.LeaveTypeName,
                DefaultDaysPerYear = x.DefaultDaysPerYear,
                IsPaid = x.IsPaid,
                IsActive = x.IsActive,
                AccrualType = x.AccrualType,
                CarryOverType = x.CarryOverType,
                CarryOverMaxDays = x.CarryOverMaxDays,
                AppliesTo = x.AppliesTo,
                RequireApproval = x.RequireApproval,
                RequireDocument = x.RequireDocument,
                AllowNegativeBalance = x.AllowNegativeBalance
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<LeaveTypeDto>> Create([FromBody] CreateLeaveTypeRequest req)
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var trimmedName = (req.LeaveTypeName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            return BadRequest(new { ok = false, message = "Leave type name is required." });

        var exists = await _db.LeaveTypes
            .AnyAsync(x => x.OrgID == orgId.Value && x.LeaveTypeName == trimmedName);
        if (exists)
            return Conflict(new { ok = false, message = "Leave type name already exists." });

        var carryOverMax = req.CarryOverType == LeaveCarryOverType.None ? null : req.CarryOverMaxDays;
        var leaveType = new LeaveType
        {
            OrgID = orgId.Value,
            LeaveTypeName = trimmedName,
            DefaultDaysPerYear = req.DefaultDaysPerYear,
            IsPaid = req.IsPaid,
            IsActive = req.IsActive,
            AccrualType = req.AccrualType,
            CarryOverType = req.CarryOverType,
            CarryOverMaxDays = carryOverMax,
            AppliesTo = req.AppliesTo,
            RequireApproval = req.RequireApproval,
            RequireDocument = req.RequireDocument,
            AllowNegativeBalance = req.AllowNegativeBalance
        };

        _db.LeaveTypes.Add(leaveType);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = leaveType.LeaveTypeID }, new LeaveTypeDto
        {
            LeaveTypeID = leaveType.LeaveTypeID,
            OrgID = leaveType.OrgID,
            LeaveTypeName = leaveType.LeaveTypeName,
            DefaultDaysPerYear = leaveType.DefaultDaysPerYear,
            IsPaid = leaveType.IsPaid,
            IsActive = leaveType.IsActive,
            AccrualType = leaveType.AccrualType,
            CarryOverType = leaveType.CarryOverType,
            CarryOverMaxDays = leaveType.CarryOverMaxDays,
            AppliesTo = leaveType.AppliesTo,
            RequireApproval = leaveType.RequireApproval,
            RequireDocument = leaveType.RequireDocument,
            AllowNegativeBalance = leaveType.AllowNegativeBalance
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LeaveTypeDto>> Update(int id, [FromBody] UpdateLeaveTypeRequest req)
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var leaveType = await _db.LeaveTypes
            .FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.LeaveTypeID == id);
        if (leaveType == null)
            return NotFound();

        var trimmedName = (req.LeaveTypeName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            return BadRequest(new { ok = false, message = "Leave type name is required." });

        var exists = await _db.LeaveTypes
            .AnyAsync(x => x.OrgID == orgId.Value && x.LeaveTypeID != id && x.LeaveTypeName == trimmedName);
        if (exists)
            return Conflict(new { ok = false, message = "Leave type name already exists." });

        var carryOverMax = req.CarryOverType == LeaveCarryOverType.None ? null : req.CarryOverMaxDays;

        leaveType.LeaveTypeName = trimmedName;
        leaveType.DefaultDaysPerYear = req.DefaultDaysPerYear;
        leaveType.IsPaid = req.IsPaid;
        leaveType.IsActive = req.IsActive;
        leaveType.AccrualType = req.AccrualType;
        leaveType.CarryOverType = req.CarryOverType;
        leaveType.CarryOverMaxDays = carryOverMax;
        leaveType.AppliesTo = req.AppliesTo;
        leaveType.RequireApproval = req.RequireApproval;
        leaveType.RequireDocument = req.RequireDocument;
        leaveType.AllowNegativeBalance = req.AllowNegativeBalance;

        await _db.SaveChangesAsync();

        return Ok(new LeaveTypeDto
        {
            LeaveTypeID = leaveType.LeaveTypeID,
            OrgID = leaveType.OrgID,
            LeaveTypeName = leaveType.LeaveTypeName,
            DefaultDaysPerYear = leaveType.DefaultDaysPerYear,
            IsPaid = leaveType.IsPaid,
            IsActive = leaveType.IsActive,
            AccrualType = leaveType.AccrualType,
            CarryOverType = leaveType.CarryOverType,
            CarryOverMaxDays = leaveType.CarryOverMaxDays,
            AppliesTo = leaveType.AppliesTo,
            RequireApproval = leaveType.RequireApproval,
            RequireDocument = leaveType.RequireDocument,
            AllowNegativeBalance = leaveType.AllowNegativeBalance
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CanAccessLeaveManagement())
            return Forbid();

        var orgId = GetUserOrgId();
        if (!orgId.HasValue)
            return Forbid();

        var leaveType = await _db.LeaveTypes
            .FirstOrDefaultAsync(x => x.OrgID == orgId.Value && x.LeaveTypeID == id);
        if (leaveType == null)
            return NotFound();

        leaveType.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }
}
