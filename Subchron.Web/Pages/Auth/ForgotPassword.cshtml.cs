using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Subchron.Web.Pages.Auth
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly IConfiguration _config;

        public ForgotPasswordModel(IConfiguration config)
        {
            _config = config;
        }

        public string ApiBaseUrl { get; private set; } = "";
        public string RecaptchaSiteKey { get; private set; } = "";

        public IActionResult OnGet()
        {
            // If already authenticated, redirect to appropriate dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoleDashboard();
            }

            ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
            RecaptchaSiteKey = _config["Recaptcha:SiteKey"] ?? "";

            return Page();
        }

        private IActionResult RedirectToRoleDashboard()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value
                       ?? User.FindFirst("role")?.Value
                       ?? "";

            var dest = role switch
            {
                "SuperAdmin" => "/SuperAdmin/Dashboard",
                "Employee" => "/Employee/Dashboard",
                "OrgAdmin" => "/App/Dashboard",
                "HR" => "/App/Dashboard",
                "Manager" => "/App/Dashboard",
                "Supervisor" => "/App/Dashboard",
                "Payroll" => "/App/Dashboard",
                _ => "/App/Dashboard"
            };

            return RedirectToPage(dest);
        }
    }
}
