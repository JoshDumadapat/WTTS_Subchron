using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.App.Operations.ScanStation
{
    public class KioskModel : PageModel
    {
        [FromQuery(Name = "id")]
        public string StationId { get; set; } = "";

        [FromQuery(Name = "idEntry")]
        public bool IdEntryEnabled { get; set; } = true;

        public void OnGet()
        {
        }
    }
}
