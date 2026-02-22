using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Auth;

public class VerifyEmailModel : PageModel
{
    public string Email { get; set; } = "";
    public string PlanId { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "";

    public IActionResult OnGet([FromQuery] string? email, [FromQuery] string? planId, [FromServices] IConfiguration config)
    {
        Email = email ?? "";
        PlanId = planId ?? "";
        ApiBaseUrl = config["ApiBaseUrl"] ?? "";

        // If no email, redirect to signup
        if (string.IsNullOrWhiteSpace(Email))
            return RedirectToPage("/Auth/Signup");

        return Page();
    }
}
