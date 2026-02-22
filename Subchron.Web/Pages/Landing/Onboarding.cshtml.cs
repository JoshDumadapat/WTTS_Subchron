using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Landing
{
    public class OnboardingModel : PageModel
    {
        private readonly IConfiguration _config;

        public OnboardingModel(IConfiguration config)
        {
            _config = config;
        }

        public string ApiBaseUrl { get; private set; } = "";

        public void OnGet()
        {
            ApiBaseUrl = _config["ApiBaseUrl"] ?? "";
        }
    }
}
