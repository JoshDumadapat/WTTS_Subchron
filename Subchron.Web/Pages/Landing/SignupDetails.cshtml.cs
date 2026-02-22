using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Auth
{
    public class SignupDetailsModel : PageModel
    {
        private readonly IConfiguration _config;

        public SignupDetailsModel(IConfiguration config)
        {
            _config = config;
        }

        public string ApiBaseUrl { get; private set; } = "";
        public string RecaptchaSiteKey { get; private set; } = "";

        public void OnGet()
        {
            ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
            RecaptchaSiteKey = _config["Recaptcha:SiteKey"] ?? "";
        }
    }
}
