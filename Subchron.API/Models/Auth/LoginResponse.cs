namespace Subchron.API.Models.Auth
{
    public class LoginResponse
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }

        public string? Jwt { get; set; }
        // Controller uses `Token`; keep both for compatibility.
        public string? Token { get; set; }

        public int UserId { get; set; }
        public int? OrgId { get; set; }
        // Organization display name for the sidebar (when user has an org).
        public string? OrgName { get; set; }
        public string? Role { get; set; }
        public string? Name { get; set; }

        // TOTP related
        public bool RequiresTotp { get; set; }
        public string? SessionToken { get; set; }

        // reCAPTCHA - indicates if captcha is now required for next attempt
        public bool RequiresCaptcha { get; set; }
    }
}
