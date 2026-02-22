namespace Subchron.API.Models.Auth;

public class SignupRequest
{
    // Org
    public string OrgName { get; set; } = null!;
    public string OrgCode { get; set; } = null!;

    // Plan & pricing choices
    public int PlanId { get; set; } = 1;
    public string AttendanceMode { get; set; } = "QR";      // QR/BioGeo/Hybrid
    public string BillingCycle { get; set; } = "Monthly"; // Defafult

    // Admin user
    public string AdminName { get; set; } = null!;
    public string AdminEmail { get; set; } = null!;
    public string Password { get; set; } = null!;

    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }


    // reCAPTCHA v2 token (g-recaptcha-response)
    public string RecaptchaToken { get; set; } = null!;
}
