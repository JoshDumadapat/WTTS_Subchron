using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Auth
{
    public class SignupModel : PageModel
    {
        private readonly IConfiguration _config;

        public SignupModel(IConfiguration config)
        {
            _config = config;
        }

        public string RecaptchaSiteKey { get; private set; } = string.Empty;
        public string ApiBaseUrl { get; private set; } = string.Empty;

        public void OnGet()
        {
            RecaptchaSiteKey = _config["Recaptcha:SiteKey"] ?? string.Empty;
            ApiBaseUrl = _config["ApiBaseUrl"] ?? string.Empty;
        }
    }
}
