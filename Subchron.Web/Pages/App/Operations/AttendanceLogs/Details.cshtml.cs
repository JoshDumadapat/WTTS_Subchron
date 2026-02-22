using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.App.Operations.AttendanceLogs
{
    public class DetailsModel : PageModel
    {
        [FromQuery(Name = "id")]
        public string LogId { get; set; } = "";

        public void OnGet()
        {
        }
    }
}
