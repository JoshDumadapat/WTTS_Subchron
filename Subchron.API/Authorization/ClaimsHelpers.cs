using System.Security.Claims;

namespace Subchron.API.Authorization;

/// <summary>Shared helpers for reading org and user from claims. Use for OrgID filtering and audit logging.</summary>
public static class ClaimsHelpers
{
    /// <summary>Gets the current user's organization ID from claims (e.g. "orgId"). Null for SuperAdmin.</summary>
    public static int? GetOrgID(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("orgId");
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>Gets the current user's UserID from claims (ClaimTypes.NameIdentifier).</summary>
    public static int? GetUserID(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>True if the current user has role SuperAdmin.</summary>
    public static bool IsSuperAdmin(this ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
        return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
}
