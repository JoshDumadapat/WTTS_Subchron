using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.Employee;

public class PayHistoryModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public PayHistoryModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnGetHistoryAsync()
        => await ProxyGetAsync("/api/payroll-processing/my/history", "[]");

    public async Task<IActionResult> OnGetPayslipAsync(int runId)
        => await ProxyGetAsync("/api/payroll-processing/my/payslip?runId=" + runId, "{}");

    public async Task<IActionResult> OnGetPayslipPdfAsync(int runId)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
            return new EmptyResult();

        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.GetAsync(baseUrl + "/api/payroll-processing/my/payslip?runId=" + runId);
        if (!resp.IsSuccessStatusCode)
            return new EmptyResult();

        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<MyPayslipResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (data?.Item == null || data.Run == null)
            return new EmptyResult();

        QuestPDF.Settings.License = LicenseType.Community;
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Employee Payslip").FontSize(16).Bold();
                    col.Item().Text("Period: " + data.Run.PeriodStart.ToString("yyyy-MM-dd") + " to " + data.Run.PeriodEnd.ToString("yyyy-MM-dd"));
                    col.Item().Text(data.Item.EmployeeName + " (" + data.Item.EmpNumber + ")").Bold();
                    col.Item().Text("Department: " + data.Item.DepartmentName);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); });
                        void Row(string k, string v)
                        {
                            t.Cell().Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text(k);
                            t.Cell().Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).AlignRight().Text(v);
                        }
                        Row("Worked Hours", data.Item.WorkedHours.ToString("0.00"));
                        Row("Overtime Hours", data.Item.OvertimeHours.ToString("0.00"));
                        Row("Base Pay", "PHP " + data.Item.BasePay.ToString("N2"));
                        Row("Overtime Pay", "PHP " + data.Item.OvertimePay.ToString("N2"));
                        Row("Allowances", "PHP " + data.Item.Allowances.ToString("N2"));
                        Row("Gross Pay", "PHP " + data.Item.GrossPay.ToString("N2"));
                        Row("Deductions", "PHP " + data.Item.Deductions.ToString("N2"));
                        Row("Tax", "PHP " + data.Item.Tax.ToString("N2"));
                        Row("Net Pay", "PHP " + data.Item.NetPay.ToString("N2"));
                    });

                    col.Item().PaddingTop(8).Text("Formula").Bold();
                    col.Item().Text(string.IsNullOrWhiteSpace(data.Item.FormulaSummary) ? "Net = (Base + OT + Allowances) - Deductions" : data.Item.FormulaSummary);
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", "payslip-" + runId + ".pdf");
    }

    private async Task<IActionResult> ProxyGetAsync(string path, string fallback)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
            return new ContentResult { Content = fallback, ContentType = "application/json", StatusCode = 200 };

        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await client.GetAsync(baseUrl + path);
            var body = await resp.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)resp.StatusCode, ContentType = "application/json", Content = string.IsNullOrWhiteSpace(body) ? fallback : body };
        }
        catch
        {
            return new ContentResult { Content = fallback, ContentType = "application/json", StatusCode = 200 };
        }
    }

    private class MyPayslipResponse
    {
        public RunInfo? Run { get; set; }
        public ItemInfo? Item { get; set; }
    }

    private class RunInfo
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    private class ItemInfo
    {
        public string EmpNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public decimal WorkedHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal BasePay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal Allowances { get; set; }
        public decimal GrossPay { get; set; }
        public decimal Deductions { get; set; }
        public decimal Tax { get; set; }
        public decimal NetPay { get; set; }
        public string FormulaSummary { get; set; } = string.Empty;
    }
}
