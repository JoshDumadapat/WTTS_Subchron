using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly SubchronDbContext _db;

    public AuditLogsController(SubchronDbContext db)
    {
        _db = db;
    }

    private int? GetUserOrgId()
    {
        var claim = User.FindFirstValue("orgId");
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id) ? id : null;
    }

    private bool IsSuperAdmin()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    // List audit logs. SuperAdmin sees all; org users see only their org.
    [HttpGet]
    public async Task<ActionResult<List<AuditLogDto>>> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? action,
        [FromQuery] string? entityName,
        [FromQuery] string? search,
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var orgId = GetUserOrgId();
        if (!orgId.HasValue && !IsSuperAdmin())
            return Forbid();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .AsQueryable();

        if (orgId.HasValue)
            query = query.Where(a => a.OrgID == orgId.Value);
        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action.Trim());
        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName.Trim());
        if (userId.HasValue)
            query = query.Where(a => a.UserID == userId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                (a.Details != null && a.Details.ToLower().Contains(term)) ||
                (a.EntityName != null && a.EntityName.ToLower().Contains(term)) ||
                (a.Action != null && a.Action.ToLower().Contains(term)) ||
                (a.User != null && a.User.Name != null && a.User.Name.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();
        var list = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                AuditID = a.AuditID,
                OrgID = a.OrgID,
                UserID = a.UserID,
                UserName = a.User != null ? a.User.Name : null,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityID = a.EntityID,
                Details = a.Details,
                CreatedAt = a.CreatedAt,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent
            })
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", total.ToString());
        return Ok(list);
    }
}

public class AuditLogDto
{
    public int AuditID { get; set; }
    public int? OrgID { get; set; }
    public int? UserID { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = "";
    public string? EntityName { get; set; }
    public int? EntityID { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
