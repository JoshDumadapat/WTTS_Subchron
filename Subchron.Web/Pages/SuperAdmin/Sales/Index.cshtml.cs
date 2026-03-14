using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.SuperAdmin.Sales;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _http;

    public IndexModel(IHttpClientFactory http)
    {
        _http = http;
    }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit => TotalIncome - TotalExpenses;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateStart { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateEnd { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    public List<TransactionViewModel> Transactions { get; set; } = new();

    public SelectList TypeOptions { get; set; } = new SelectList(new[] { "Income", "Expense" });

    [BindProperty]
    public TransactionInputModel NewTransaction { get; set; } = new();

    public List<string> TrendLabels { get; set; } = new();
    public List<decimal> TrendIncome { get; set; } = new();
    public List<decimal> TrendExpenses { get; set; } = new();

    [TempData]
    public string? FlashMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        TypeOptions = new SelectList(new[] { "Income", "Expense" });
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        NewTransaction.Description = (NewTransaction.Description ?? string.Empty).Trim();
        NewTransaction.ReferenceNumber = (NewTransaction.ReferenceNumber ?? string.Empty).Trim().ToUpperInvariant();
        NewTransaction.Tin = NormalizeTin(NewTransaction.Tin);

        if (NewTransaction.TaxAmount < 0)
            ModelState.AddModelError("NewTransaction.TaxAmount", "VAT/Tax cannot be negative.");

        if (!ModelState.IsValid)
        {
            TypeOptions = new SelectList(new[] { "Income", "Expense" });
            await LoadDataAsync();
            return Page();
        }

        var client = CreateAuthorizedClient();
        if (client == null)
        {
            FlashMessage = "Your session expired. Please sign in again.";
            return RedirectToPage("/Auth/Login");
        }

        var payload = new
        {
            Date = DateTime.UtcNow.Date,
            Description = NewTransaction.Description,
            Amount = NewTransaction.Amount,
            ReferenceNumber = NewTransaction.ReferenceNumber,
            Tin = NewTransaction.Tin,
            TaxAmount = NewTransaction.TaxAmount,
            Category = NewTransaction.Category,
            Notes = NewTransaction.Notes
        };

        var resp = await client.PostAsJsonAsync("api/superadmin/sales/expenses", payload);
        if (!resp.IsSuccessStatusCode)
        {
            var serverMsg = await resp.Content.ReadAsStringAsync();
            FlashMessage = string.IsNullOrWhiteSpace(serverMsg)
                ? "Could not save expense record."
                : serverMsg;
            TypeOptions = new SelectList(new[] { "Income", "Expense" });
            await LoadDataAsync();
            return Page();
        }

        FlashMessage = "Expense record saved.";
        TypeOptions = new SelectList(new[] { "Income", "Expense" });
        return RedirectToPage(new
        {
            SearchTerm,
            TypeFilter,
            DateStart,
            DateEnd,
            CurrentPage,
            PageSize
        });
    }

    private async Task LoadDataAsync()
    {
        CurrentPage = Math.Max(1, CurrentPage);
        PageSize = Math.Clamp(PageSize, 5, 100);

        var client = CreateAuthorizedClient();
        if (client == null)
            return;

        var query = new List<string>
        {
            "page=" + CurrentPage,
            "pageSize=" + PageSize
        };

        if (!string.IsNullOrWhiteSpace(SearchTerm))
            query.Add("search=" + Uri.EscapeDataString(SearchTerm.Trim()));
        if (!string.IsNullOrWhiteSpace(TypeFilter))
            query.Add("type=" + Uri.EscapeDataString(TypeFilter.Trim()));
        if (DateStart.HasValue)
            query.Add("dateStart=" + Uri.EscapeDataString(DateStart.Value.ToString("yyyy-MM-dd")));
        if (DateEnd.HasValue)
            query.Add("dateEnd=" + Uri.EscapeDataString(DateEnd.Value.ToString("yyyy-MM-dd")));

        var resp = await client.GetAsync("api/superadmin/sales/transactions?" + string.Join("&", query));
        if (!resp.IsSuccessStatusCode)
            return;

        var data = await resp.Content.ReadFromJsonAsync<SalesTransactionsResponse>();
        if (data == null)
            return;

        TotalIncome = data.TotalIncome;
        TotalExpenses = data.TotalExpenses;
        TotalCount = data.TotalCount;
        CurrentPage = Math.Max(1, data.Page);
        PageSize = Math.Clamp(data.PageSize, 5, 100);
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        Transactions = data.Items
            .Select(t => new TransactionViewModel
            {
                Id = t.Id,
                Date = t.Date,
                Description = t.Description,
                Amount = t.Amount,
                Type = string.Equals(t.Type, "Expense", StringComparison.OrdinalIgnoreCase)
                    ? TransactionType.Expense
                    : TransactionType.Income,
                ReferenceNumber = t.ReferenceNumber,
                Category = t.Category,
                Notes = t.Notes,
                TaxAmount = t.TaxAmount,
                Tin = t.Tin ?? string.Empty
            })
            .ToList();

        TrendLabels = data.Trend
            .OrderBy(t => t.Year)
            .ThenBy(t => t.Month)
            .Select(t => new DateTime(t.Year, t.Month, 1).ToString("MMM"))
            .ToList();
        TrendIncome = data.Trend
            .OrderBy(t => t.Year)
            .ThenBy(t => t.Month)
            .Select(t => t.Income)
            .ToList();
        TrendExpenses = data.Trend
            .OrderBy(t => t.Year)
            .ThenBy(t => t.Month)
            .Select(t => t.Expense)
            .ToList();
    }

    private HttpClient? CreateAuthorizedClient()
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string? NormalizeTin(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;
        if (digits.Length > 12)
            digits = digits[..12];

        var groups = new List<string>();
        for (var i = 0; i < digits.Length; i += 3)
            groups.Add(digits.Substring(i, Math.Min(3, digits.Length - i)));

        return string.Join("-", groups);
    }

    public class TransactionViewModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Tin { get; set; } = string.Empty;
        public decimal TaxAmount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class TransactionInputModel
    {
        [Required]
        [StringLength(120, ErrorMessage = "Description must be 120 characters or less.")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10000000, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public TransactionType Type { get; set; } = TransactionType.Expense;

        [Required(ErrorMessage = "Reference Number (OR/SI) is required")]
        [StringLength(40, ErrorMessage = "Reference number must be 40 characters or less.")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [RegularExpression(@"^\d{3}-\d{3}-\d{3}(-\d{3})?$", ErrorMessage = "TIN must be in 000-000-000 or 000-000-000-000 format.")]
        public string? Tin { get; set; }

        [Range(0, 10000000, ErrorMessage = "VAT/Tax cannot be negative.")]
        public decimal TaxAmount { get; set; }

        [StringLength(40, ErrorMessage = "Category must be 40 characters or less.")]
        public string? Category { get; set; }

        [StringLength(255, ErrorMessage = "Notes must be 255 characters or less.")]
        public string? Notes { get; set; }
    }

    public enum TransactionType
    {
        Income,
        Expense
    }

    private sealed class SalesTransactionsResponse
    {
        public List<SalesTransactionItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public List<SalesTrendPoint> Trend { get; set; } = new();
    }

    private sealed class SalesTransactionItem
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Income";
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? Tin { get; set; }
        public decimal TaxAmount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    private sealed class SalesTrendPoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }
}
