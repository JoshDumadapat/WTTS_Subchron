using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.App.Operations.OvertimeRequests
{
    public class DetailsModel : PageModel
    {
        [FromQuery(Name = "id")]
        public string RequestId { get; set; } = "";

        public void OnGet()
        {
        }
    }
}
