using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;
using Subchron.API.Models.Entities;
using Subchron.API.Services;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/demo-requests")]
public class DemoRequestsController : ControllerBase
{
    private readonly SubchronDbContext _db;
    private readonly EmailService _email;
    private const string SuperAdminDemoEmail = "ivanjoshdumadapat30@gmail.com";

    public DemoRequestsController(SubchronDbContext db, EmailService email)
    {
        _db = db;
        _email = email;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDemoRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var row = new DemoRequest
        {
            OrgName = request.OrgName.Trim(),
            ContactName = request.ContactName.Trim(),
            Email = request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            OrgSize = string.IsNullOrWhiteSpace(request.OrgSize) ? null : request.OrgSize.Trim(),
            DesiredMode = string.IsNullOrWhiteSpace(request.DesiredMode) ? null : request.DesiredMode.Trim(),
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.DemoRequests.Add(row);
        await _db.SaveChangesAsync();

        try
        {
            var subject = "New Demo Request - " + row.OrgName;
            var body = BuildDemoRequestEmailBody(row);
            await _email.SendAsync(SuperAdminDemoEmail, subject, body);
        }
        catch
        {
            // Demo request is already saved; email failure should not block submit.
        }

        return Ok(new { ok = true, demoRequestId = row.DemoRequestID });
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<DemoRequestDto>>> List([FromQuery] string? status)
    {
        if (!IsSuperAdmin())
            return Forbid();

        var query = _db.DemoRequests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim();
            query = query.Where(x => x.Status == normalized);
        }

        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DemoRequestDto
            {
                DemoRequestID = x.DemoRequestID,
                OrgName = x.OrgName,
                ContactName = x.ContactName,
                Email = x.Email,
                Phone = x.Phone,
                OrgSize = x.OrgSize,
                DesiredMode = x.DesiredMode,
                Message = x.Message,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                ReviewedAt = x.ReviewedAt,
                ReviewedByUserID = x.ReviewedByUserID,
                OrgID = x.OrgID
            })
            .ToListAsync();

        return Ok(rows);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DemoRequestDto>> GetById(int id)
    {
        if (!IsSuperAdmin())
            return Forbid();

        var row = await _db.DemoRequests.AsNoTracking().FirstOrDefaultAsync(x => x.DemoRequestID == id);
        if (row == null)
            return NotFound(new { ok = false, message = "Demo request not found." });

        return Ok(new DemoRequestDto
        {
            DemoRequestID = row.DemoRequestID,
            OrgName = row.OrgName,
            ContactName = row.ContactName,
            Email = row.Email,
            Phone = row.Phone,
            OrgSize = row.OrgSize,
            DesiredMode = row.DesiredMode,
            Message = row.Message,
            Status = row.Status,
            CreatedAt = row.CreatedAt,
            ReviewedAt = row.ReviewedAt,
            ReviewedByUserID = row.ReviewedByUserID,
            OrgID = row.OrgID
        });
    }

    [Authorize]
    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewDemoRequest request)
    {
        if (!IsSuperAdmin())
            return Forbid();

        var action = (request.Action ?? string.Empty).Trim().ToLowerInvariant();
        if (action != "approve" && action != "reject")
            return BadRequest(new { ok = false, message = "Action must be approve or reject." });

        var row = await _db.DemoRequests.FirstOrDefaultAsync(x => x.DemoRequestID == id);
        if (row == null)
            return NotFound(new { ok = false, message = "Demo request not found." });

        if (!string.Equals(row.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { ok = false, message = "This demo request has already been reviewed." });

        row.Status = action == "approve" ? "Approved" : "Rejected";
        row.ReviewedAt = DateTime.UtcNow;
        row.ReviewedByUserID = GetUserId();

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, status = row.Status });
    }

    private bool IsSuperAdmin()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static string BuildDemoRequestEmailBody(DemoRequest row)
    {
        var created = row.CreatedAt.ToString("yyyy-MM-dd HH:mm 'UTC'");
        return $@"
<html><body style='font-family:Segoe UI,Arial,sans-serif;color:#0f172a;'>
  <h2 style='margin-bottom:8px;'>New Demo Request Submitted</h2>
  <p style='margin-top:0;color:#475569;'>A new demo request has been submitted from the landing page.</p>
  <table cellpadding='6' cellspacing='0' border='0' style='border-collapse:collapse;'>
    <tr><td><strong>Request ID:</strong></td><td>#{row.DemoRequestID}</td></tr>
    <tr><td><strong>Organization:</strong></td><td>{System.Net.WebUtility.HtmlEncode(row.OrgName)}</td></tr>
    <tr><td><strong>Contact:</strong></td><td>{System.Net.WebUtility.HtmlEncode(row.ContactName)}</td></tr>
    <tr><td><strong>Email:</strong></td><td>{System.Net.WebUtility.HtmlEncode(row.Email)}</td></tr>
    <tr><td><strong>Phone:</strong></td><td>{System.Net.WebUtility.HtmlEncode(row.Phone ?? "-")}</td></tr>
    <tr><td><strong>Org Size:</strong></td><td>{System.Net.WebUtility.HtmlEncode(row.OrgSize ?? "-")}</td></tr>
    <tr><td><strong>Desired Mode:</strong></td><td>{System.Net.WebUtility.HtmlEncode(row.DesiredMode ?? "-")}</td></tr>
    <tr><td><strong>Message:</strong></td><td>{System.Net.WebUtility.HtmlEncode(row.Message ?? "-")}</td></tr>
    <tr><td><strong>Submitted:</strong></td><td>{created}</td></tr>
  </table>
  <p style='margin-top:16px;'>Open SuperAdmin > Demo Requests to review.</p>
</body></html>";
    }

    public class CreateDemoRequest
    {
        [Required]
        [MaxLength(100)]
        public string OrgName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContactName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? OrgSize { get; set; }

        [MaxLength(20)]
        public string? DesiredMode { get; set; }

        [MaxLength(255)]
        public string? Message { get; set; }
    }

    public class ReviewDemoRequest
    {
        public string Action { get; set; } = string.Empty;
    }

    public class DemoRequestDto
    {
        public int DemoRequestID { get; set; }
        public string OrgName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? OrgSize { get; set; }
        public string? DesiredMode { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserID { get; set; }
        public int? OrgID { get; set; }
    }
}
