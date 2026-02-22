using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.App.Operations.ScanStation
{
    public class DetailsModel : PageModel
    {
        [FromQuery(Name = "id")]
        public string StationId { get; set; } = "";

        public void OnGet()
        {
        }
    }
}
