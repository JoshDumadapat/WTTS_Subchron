using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Subchron.Web.Pages.Auth;

public class BillingModel : PageModel
{
    public string Token { get; set; } = "";
    public string ReturnPaymentIntentId { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "";
    public string PayMongoPublicKey { get; set; } = "";

    /// <summary>When true, page is used only to manage/update payment (e.g. from Settings); signup scripts are not run.</summary>
    public bool IsManagePaymentOnly { get; set; }

    public IActionResult OnGet(
        [FromQuery] string? token,
        [FromQuery] string? paymentIntentId,
        [FromServices] IConfiguration config,
        [FromQuery] bool manageOnly = false)
    {
        ApiBaseUrl = config["ApiBaseUrl"] ?? "";
        PayMongoPublicKey = config["PayMongo:PublicKey"] ?? "";
        IsManagePaymentOnly = manageOnly;

        if (string.IsNullOrWhiteSpace(token) && !manageOnly)
            return RedirectToPage("/Auth/Signup");

        Token = token ?? "";
        ReturnPaymentIntentId = paymentIntentId ?? "";

        return Page();
    }
}
