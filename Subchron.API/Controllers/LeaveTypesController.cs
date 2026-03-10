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
            .Select(x => ToDto(x))
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
            .Select(x => ToDto(x))
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

        req.StatutoryCode = ResolveStatutoryCode(req.LeaveCategory, req.StatutoryCode, trimmedName);

        var createValidation = ValidateLeaveTypeRequest(req);
        if (!string.IsNullOrEmpty(createValidation))
            return BadRequest(new { ok = false, message = createValidation });

        if (req.IsSystemProtected && req.LeaveCategory != LeaveCategory.Statutory)
            return BadRequest(new { ok = false, message = "Only statutory leave types can be marked as protected." });

        var exists = await _db.LeaveTypes
            .AnyAsync(x => x.OrgID == orgId.Value && x.IsActive && x.LeaveTypeName.ToLower() == trimmedName.ToLower());
        if (exists)
            return Conflict(new { ok = false, message = "Leave type name already exists." });

        if (req.StatutoryCode != LeaveStatutoryCode.None)
        {
            var statutoryExists = await _db.LeaveTypes
                .AnyAsync(x => x.OrgID == orgId.Value && x.IsActive && x.StatutoryCode == req.StatutoryCode);
            if (statutoryExists)
                return Conflict(new { ok = false, message = "This statutory leave type is already added." });
        }

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
            AllowNegativeBalance = req.AllowNegativeBalance,
            LeaveCategory = req.LeaveCategory,
            CompensationSource = req.CompensationSource,
            PaidStatus = req.PaidStatus,
            StatutoryCode = req.StatutoryCode,
            MinServiceMonths = req.MinServiceMonths,
            AdvanceFilingDays = req.AdvanceFilingDays,
            AllowRetroactiveFiling = req.AllowRetroactiveFiling,
            MaxConsecutiveDays = req.MaxConsecutiveDays,
            FilingUnit = req.FilingUnit,
            DeductBalanceOn = req.DeductBalanceOn,
            ApproverRole = req.RequireApproval ? req.ApproverRole : LeaveApproverRole.AutoApprove,
            LeaveExpiryRule = req.LeaveExpiryRule,
            LeaveExpiryCustomMonths = req.LeaveExpiryRule == LeaveExpiryRule.CustomMonths ? req.LeaveExpiryCustomMonths : null,
            AllowLeaveOnRestDay = req.AllowLeaveOnRestDay,
            AllowLeaveOnHoliday = req.AllowLeaveOnHoliday,
            RequiresLegalQualification = req.RequiresLegalQualification,
            RequiresHrValidation = req.RequiresHrValidation,
            CanOrgOverride = req.CanOrgOverride,
            IsSystemProtected = req.IsSystemProtected
        };

        _db.LeaveTypes.Add(leaveType);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = leaveType.LeaveTypeID }, ToDto(leaveType));
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

        req.StatutoryCode = ResolveStatutoryCode(req.LeaveCategory, req.StatutoryCode, trimmedName);

        var updateValidation = ValidateLeaveTypeRequest(req);
        if (!string.IsNullOrEmpty(updateValidation))
            return BadRequest(new { ok = false, message = updateValidation });

        if (leaveType.IsSystemProtected)
        {
            if (leaveType.LeaveTypeName != trimmedName
                || leaveType.LeaveCategory != req.LeaveCategory
                || leaveType.StatutoryCode != req.StatutoryCode
                || leaveType.CompensationSource != req.CompensationSource
                || leaveType.PaidStatus != req.PaidStatus
                || leaveType.IsSystemProtected != req.IsSystemProtected)
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "Protected leave identity fields cannot be modified. Only workflow and filing settings are editable."
                });
            }
        }

        if ((leaveType.LeaveCategory == LeaveCategory.Statutory || leaveType.StatutoryCode != LeaveStatutoryCode.None)
            && req.LeaveCategory != LeaveCategory.Statutory)
        {
            return BadRequest(new
            {
                ok = false,
                message = "Statutory leave category is fixed and cannot be changed in edit mode."
            });
        }

        if (req.IsSystemProtected && req.LeaveCategory != LeaveCategory.Statutory)
            return BadRequest(new { ok = false, message = "Only statutory leave types can be marked as protected." });

        var exists = await _db.LeaveTypes
            .AnyAsync(x => x.OrgID == orgId.Value && x.LeaveTypeID != id && x.IsActive && x.LeaveTypeName.ToLower() == trimmedName.ToLower());
        if (exists)
            return Conflict(new { ok = false, message = "Leave type name already exists." });

        if (req.StatutoryCode != LeaveStatutoryCode.None)
        {
            var statutoryExists = await _db.LeaveTypes
                .AnyAsync(x => x.OrgID == orgId.Value && x.LeaveTypeID != id && x.IsActive && x.StatutoryCode == req.StatutoryCode);
            if (statutoryExists)
                return Conflict(new { ok = false, message = "This statutory leave type is already added." });
        }

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
        leaveType.LeaveCategory = req.LeaveCategory;
        leaveType.CompensationSource = req.CompensationSource;
        leaveType.PaidStatus = req.PaidStatus;
        leaveType.StatutoryCode = req.StatutoryCode;
        leaveType.MinServiceMonths = req.MinServiceMonths;
        leaveType.AdvanceFilingDays = req.AdvanceFilingDays;
        leaveType.AllowRetroactiveFiling = req.AllowRetroactiveFiling;
        leaveType.MaxConsecutiveDays = req.MaxConsecutiveDays;
        leaveType.FilingUnit = req.FilingUnit;
        leaveType.DeductBalanceOn = req.DeductBalanceOn;
        leaveType.ApproverRole = req.RequireApproval ? req.ApproverRole : LeaveApproverRole.AutoApprove;
        leaveType.LeaveExpiryRule = req.LeaveExpiryRule;
        leaveType.LeaveExpiryCustomMonths = req.LeaveExpiryRule == LeaveExpiryRule.CustomMonths ? req.LeaveExpiryCustomMonths : null;
        leaveType.AllowLeaveOnRestDay = req.AllowLeaveOnRestDay;
        leaveType.AllowLeaveOnHoliday = req.AllowLeaveOnHoliday;
        leaveType.RequiresLegalQualification = req.RequiresLegalQualification;
        leaveType.RequiresHrValidation = req.RequiresHrValidation;
        leaveType.CanOrgOverride = req.CanOrgOverride;
        leaveType.IsSystemProtected = req.IsSystemProtected;

        await _db.SaveChangesAsync();

        return Ok(ToDto(leaveType));
    }

    private static string? ValidateLeaveTypeRequest(CreateLeaveTypeRequest req)
    {
        if (!Enum.IsDefined(typeof(LeaveAccrualType), req.AccrualType))
            return "Invalid accrual type.";
        if (!Enum.IsDefined(typeof(LeaveCarryOverType), req.CarryOverType))
            return "Invalid carry over type.";
        if (!Enum.IsDefined(typeof(LeaveAppliesTo), req.AppliesTo))
            return "Invalid applies-to value.";
        if (!Enum.IsDefined(typeof(LeaveCategory), req.LeaveCategory))
            return "Invalid leave category.";
        if (!Enum.IsDefined(typeof(LeaveCompensationSource), req.CompensationSource))
            return "Invalid compensation source.";
        if (!Enum.IsDefined(typeof(LeavePaidStatus), req.PaidStatus))
            return "Invalid paid status.";
        if (!Enum.IsDefined(typeof(LeaveStatutoryCode), req.StatutoryCode))
            return "Invalid statutory code.";
        if (!Enum.IsDefined(typeof(LeaveFilingUnit), req.FilingUnit))
            return "Invalid filing unit.";
        if (!Enum.IsDefined(typeof(LeaveDeductionTiming), req.DeductBalanceOn))
            return "Invalid deduction timing.";
        if (!Enum.IsDefined(typeof(LeaveApproverRole), req.ApproverRole))
            return "Invalid approver role.";
        if (!Enum.IsDefined(typeof(LeaveExpiryRule), req.LeaveExpiryRule))
            return "Invalid leave expiry rule.";

        if (req.LeaveCategory == LeaveCategory.Statutory && req.StatutoryCode == LeaveStatutoryCode.None)
            return "Statutory leave category requires a statutory code.";
        if (req.LeaveCategory != LeaveCategory.Statutory && req.StatutoryCode != LeaveStatutoryCode.None)
            return "Non-statutory leave categories must use statutory code 'None'.";

        if (req.CompensationSource == LeaveCompensationSource.Unpaid)
        {
            if (req.PaidStatus != LeavePaidStatus.Unpaid)
                return "Unpaid compensation source requires paid status 'Unpaid'.";
            if (req.IsPaid)
                return "Unpaid compensation source cannot be marked as paid.";
        }

        if (req.PaidStatus == LeavePaidStatus.Paid && !req.IsPaid)
            return "Paid status 'Paid' requires IsPaid=true.";
        if (req.PaidStatus == LeavePaidStatus.Unpaid && req.IsPaid)
            return "Paid status 'Unpaid' requires IsPaid=false.";

        if (req.CarryOverType == LeaveCarryOverType.None && req.CarryOverMaxDays.HasValue)
            return "Carry over max days must be empty when carry over type is None.";
        if (req.CarryOverType == LeaveCarryOverType.MaxDays && (!req.CarryOverMaxDays.HasValue || req.CarryOverMaxDays.Value <= 0))
            return "Carry over max days is required and must be greater than zero for MaxDays carry over type.";

        if (req.LeaveExpiryRule == LeaveExpiryRule.CustomMonths)
        {
            if (!req.LeaveExpiryCustomMonths.HasValue || req.LeaveExpiryCustomMonths.Value < 1 || req.LeaveExpiryCustomMonths.Value > 120)
                return "Custom leave expiry requires months between 1 and 120.";
        }
        else if (req.LeaveExpiryCustomMonths.HasValue)
        {
            return "Leave expiry custom months must be empty unless expiry rule is CustomMonths.";
        }

        if (!req.RequireApproval && req.ApproverRole != LeaveApproverRole.AutoApprove)
            return "Approver role must be AutoApprove when approval is not required.";
        if (req.RequireApproval && req.ApproverRole == LeaveApproverRole.AutoApprove)
            return "Select a manual approver role when approval is required.";

        if (req.RequiresHrValidation && !req.RequireApproval)
            return "HR validation requires approval to be enabled.";

        return null;
    }

    private static string? ValidateLeaveTypeRequest(UpdateLeaveTypeRequest req)
        => ValidateLeaveTypeRequest(new CreateLeaveTypeRequest
        {
            LeaveTypeName = req.LeaveTypeName,
            DefaultDaysPerYear = req.DefaultDaysPerYear,
            IsPaid = req.IsPaid,
            IsActive = req.IsActive,
            AccrualType = req.AccrualType,
            CarryOverType = req.CarryOverType,
            CarryOverMaxDays = req.CarryOverMaxDays,
            AppliesTo = req.AppliesTo,
            RequireApproval = req.RequireApproval,
            RequireDocument = req.RequireDocument,
            AllowNegativeBalance = req.AllowNegativeBalance,
            LeaveCategory = req.LeaveCategory,
            CompensationSource = req.CompensationSource,
            PaidStatus = req.PaidStatus,
            StatutoryCode = req.StatutoryCode,
            MinServiceMonths = req.MinServiceMonths,
            AdvanceFilingDays = req.AdvanceFilingDays,
            AllowRetroactiveFiling = req.AllowRetroactiveFiling,
            MaxConsecutiveDays = req.MaxConsecutiveDays,
            FilingUnit = req.FilingUnit,
            DeductBalanceOn = req.DeductBalanceOn,
            ApproverRole = req.ApproverRole,
            LeaveExpiryRule = req.LeaveExpiryRule,
            LeaveExpiryCustomMonths = req.LeaveExpiryCustomMonths,
            AllowLeaveOnRestDay = req.AllowLeaveOnRestDay,
            AllowLeaveOnHoliday = req.AllowLeaveOnHoliday,
            RequiresLegalQualification = req.RequiresLegalQualification,
            RequiresHrValidation = req.RequiresHrValidation,
            CanOrgOverride = req.CanOrgOverride,
            IsSystemProtected = req.IsSystemProtected
        });

    private static LeaveStatutoryCode ResolveStatutoryCode(LeaveCategory category, LeaveStatutoryCode requested, string leaveTypeName)
    {
        if (category != LeaveCategory.Statutory)
            return LeaveStatutoryCode.None;

        if (requested != LeaveStatutoryCode.None)
            return requested;

        var key = (leaveTypeName ?? string.Empty).Trim().ToLowerInvariant();
        if (key.Contains("service incentive") || key == "sil")
            return LeaveStatutoryCode.ServiceIncentiveLeave;
        if (key.Contains("maternity") || key == "ml")
            return LeaveStatutoryCode.Maternity;
        if (key.Contains("paternity") || key == "pl")
            return LeaveStatutoryCode.Paternity;
        if (key.Contains("solo parent") || key == "spl")
            return LeaveStatutoryCode.SoloParent;
        if (key.Contains("vawc"))
            return LeaveStatutoryCode.ViolenceAgainstWomenChildren;
        if (key.Contains("special leave for women") || key.Contains("special women") || key == "slw")
            return LeaveStatutoryCode.SpecialLeaveForWomen;

        return LeaveStatutoryCode.None;
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

        if (leaveType.IsSystemProtected)
            return BadRequest(new { ok = false, message = "Protected leave types cannot be deleted." });

        leaveType.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { ok = true });
    }

    private static LeaveTypeDto ToDto(LeaveType x)
        => new()
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
            AllowNegativeBalance = x.AllowNegativeBalance,
            LeaveCategory = x.LeaveCategory,
            CompensationSource = x.CompensationSource,
            PaidStatus = x.PaidStatus,
            StatutoryCode = x.StatutoryCode,
            MinServiceMonths = x.MinServiceMonths,
            AdvanceFilingDays = x.AdvanceFilingDays,
            AllowRetroactiveFiling = x.AllowRetroactiveFiling,
            MaxConsecutiveDays = x.MaxConsecutiveDays,
            FilingUnit = x.FilingUnit,
            DeductBalanceOn = x.DeductBalanceOn,
            ApproverRole = x.ApproverRole,
            LeaveExpiryRule = x.LeaveExpiryRule,
            LeaveExpiryCustomMonths = x.LeaveExpiryCustomMonths,
            AllowLeaveOnRestDay = x.AllowLeaveOnRestDay,
            AllowLeaveOnHoliday = x.AllowLeaveOnHoliday,
            RequiresLegalQualification = x.RequiresLegalQualification,
            RequiresHrValidation = x.RequiresHrValidation,
            CanOrgOverride = x.CanOrgOverride,
            IsSystemProtected = x.IsSystemProtected
        };
}
