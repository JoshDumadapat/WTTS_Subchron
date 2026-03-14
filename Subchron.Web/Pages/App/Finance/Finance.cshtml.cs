using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Finance;

public class FinanceModel : PageModel
{
    private readonly IHttpClientFactory _http;

    public FinanceModel(IHttpClientFactory http)
    {
        _http = http;
    }

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

    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetCashflow => TotalIncome - TotalExpenses;
    public decimal TotalTax { get; set; }

    public string Currency { get; set; } = "PHP";
    public string? LoadError { get; set; }

    public List<TransactionRow> Transactions { get; set; } = new();
    public List<TrendPoint> Trend { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnGetExportPdfAsync(string? searchTerm, string? typeFilter, DateTime? dateStart, DateTime? dateEnd)
    {
        var client = CreateAuthorizedClient();
        if (client == null)
            return new EmptyResult();

        var data = await FetchTransactionsAsync(client, searchTerm, typeFilter, dateStart, dateEnd, 1, 1000);
        if (data == null)
            return new EmptyResult();

        QuestPDF.Settings.License = LicenseType.Community;
        var rows = data.Items
            .OrderByDescending(x => x.Date)
            .ToList();
        var pdf = BuildFinancePdf(rows, data.TotalIncome, data.TotalExpenses, dateStart, dateEnd, string.IsNullOrWhiteSpace(data.Currency) ? "PHP" : data.Currency);
        return File(pdf, "application/pdf", $"finance-transactions-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf");
    }

    private async Task LoadDataAsync()
    {
        CurrentPage = Math.Max(1, CurrentPage);
        PageSize = Math.Clamp(PageSize, 5, 100);

        var client = CreateAuthorizedClient();
        if (client == null)
        {
            LoadError = "Your session has expired. Please sign in again.";
            return;
        }

        var data = await FetchTransactionsAsync(client, SearchTerm, TypeFilter, DateStart, DateEnd, CurrentPage, PageSize);
        if (data == null)
        {
            LoadError = "Unable to load finance transactions right now.";
            return;
        }

        TotalIncome = data.TotalIncome;
        TotalExpenses = data.TotalExpenses;
        TotalCount = data.TotalCount;
        Currency = string.IsNullOrWhiteSpace(data.Currency) ? "PHP" : data.Currency;
        CurrentPage = Math.Max(1, data.Page);
        PageSize = Math.Clamp(data.PageSize, 5, 100);
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        Transactions = data.Items
            .Select(x => new TransactionRow
            {
                Id = x.Id,
                Date = x.Date,
                Description = x.Description,
                Amount = x.Amount,
                TaxAmount = x.TaxAmount,
                Type = string.Equals(x.Type, "Expense", StringComparison.OrdinalIgnoreCase) ? "Expense" : "Income",
                ReferenceNumber = x.ReferenceNumber,
                Category = x.Category,
                Notes = x.Notes,
                Tin = x.Tin
            })
            .ToList();

        TotalTax = Transactions.Sum(x => Math.Max(0m, x.TaxAmount));
        Trend = data.Trend
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .Select(x => new TrendPoint
            {
                Label = new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy"),
                Income = x.Income,
                Expense = x.Expense
            })
            .ToList();
    }

    private static byte[] BuildFinancePdf(List<TransactionItem> rows, decimal totalIncome, decimal totalExpenses, DateTime? dateStart, DateTime? dateEnd, string currency)
    {
        var totalTax = rows.Sum(x => Math.Max(0m, x.TaxAmount));
        var net = totalIncome - totalExpenses;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Background("#0F766E").Padding(10).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Finance Transactions Report").FontSize(14).Bold().FontColor(Colors.White);
                            c.Item().Text($"Range: {(dateStart?.ToString("yyyy-MM-dd") ?? "-")} to {(dateEnd?.ToString("yyyy-MM-dd") ?? "-")}").FontSize(8).FontColor("#CCFBF1");
                        });
                        r.ConstantItem(180).AlignRight().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#CCFBF1");
                    });

                    col.Item().PaddingTop(6).Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        void Metric(string label, decimal value, string color)
                        {
                            t.Cell().Border(1).BorderColor("#E2E8F0").Padding(6).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8).FontColor("#64748B");
                                c.Item().Text(value.ToString("N2") + " " + currency).Bold().FontColor(color);
                            });
                        }

                        Metric("Total Income", totalIncome, "#047857");
                        Metric("Total Expenses", totalExpenses, "#BE123C");
                        Metric("Net Cashflow", net, net >= 0 ? "#0C4A6E" : "#B45309");
                        Metric("Accumulated Tax", totalTax, "#92400E");
                    });
                });

                page.Content().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.1f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(2.8f);
                        c.RelativeColumn(1.3f);
                        c.RelativeColumn(1.1f);
                        c.RelativeColumn(1.1f);
                        c.RelativeColumn(1f);
                    });

                    void Header(string text)
                    {
                        t.Cell().Background("#F1F5F9").BorderBottom(1).BorderColor("#CBD5E1").Padding(4).Text(text).Bold().FontSize(8);
                    }

                    Header("Date");
                    Header("Reference");
                    Header("Description");
                    Header("Category");
                    Header("Type");
                    Header("Amount");
                    Header("Tax");

                    for (var i = 0; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        var bg = i % 2 == 0 ? "#FFFFFF" : "#F8FAFC";
                        var typeColor = string.Equals(row.Type, "Expense", StringComparison.OrdinalIgnoreCase) ? "#BE123C" : "#047857";

                        t.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).Text(row.Date.ToString("yyyy-MM-dd")).FontSize(8);
                        t.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).Text(string.IsNullOrWhiteSpace(row.ReferenceNumber) ? "-" : row.ReferenceNumber).FontSize(8);
                        t.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).Text(string.IsNullOrWhiteSpace(row.Description) ? "-" : row.Description).FontSize(8);
                        t.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).Text(string.IsNullOrWhiteSpace(row.Category) ? "-" : row.Category).FontSize(8);
                        t.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).Text(string.IsNullOrWhiteSpace(row.Type) ? "-" : row.Type).FontSize(8).FontColor(typeColor);
                        t.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).AlignRight().Text(row.Amount.ToString("N2")).FontSize(8).FontColor(typeColor);
                        t.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).AlignRight().Text(row.TaxAmount.ToString("N2")).FontSize(8);
                    }
                });
            });
        }).GeneratePdf();
    }

    private static List<string> BuildQuery(string? searchTerm, string? typeFilter, DateTime? dateStart, DateTime? dateEnd, int page, int pageSize)
    {
        var query = new List<string>
        {
            "page=" + page,
            "pageSize=" + pageSize
        };

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query.Add("search=" + Uri.EscapeDataString(searchTerm.Trim()));
        if (!string.IsNullOrWhiteSpace(typeFilter))
            query.Add("type=" + Uri.EscapeDataString(typeFilter.Trim()));
        if (dateStart.HasValue)
            query.Add("dateStart=" + Uri.EscapeDataString(dateStart.Value.ToString("yyyy-MM-dd")));
        if (dateEnd.HasValue)
            query.Add("dateEnd=" + Uri.EscapeDataString(dateEnd.Value.ToString("yyyy-MM-dd")));

        return query;
    }

    private static async Task<TransactionsResponse?> FetchTransactionsAsync(HttpClient client, string? searchTerm, string? typeFilter, DateTime? dateStart, DateTime? dateEnd, int page, int pageSize)
    {
        var query = BuildQuery(searchTerm, typeFilter, dateStart, dateEnd, page, pageSize);
        var resp = await client.GetAsync("api/superadmin/sales/transactions?" + string.Join("&", query));
        if (!resp.IsSuccessStatusCode)
            return null;

        return await resp.Content.ReadFromJsonAsync<TransactionsResponse>();
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

    public sealed class TransactionRow
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Income";
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? Tin { get; set; }
        public decimal TaxAmount { get; set; }
        public string? Category { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class TrendPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }

    private sealed class TransactionsResponse
    {
        public List<TransactionItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public string Currency { get; set; } = "PHP";
        public List<TrendItem> Trend { get; set; } = new();
    }

    private sealed class TransactionItem
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Income";
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? Tin { get; set; }
        public decimal TaxAmount { get; set; }
        public string? Category { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class TrendItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }
}
