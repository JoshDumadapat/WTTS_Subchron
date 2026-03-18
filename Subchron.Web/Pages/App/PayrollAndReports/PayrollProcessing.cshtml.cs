using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.App.PayrollAndReports;

public class PayrollProcessingModel : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }
}
