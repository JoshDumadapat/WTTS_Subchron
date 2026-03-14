using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.PayrollAndReports;

public class PayrollModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public PayrollModel(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnGetEmployeesAsync()
        => await ProxyGetAsync("/api/employees", "[]");

    public async Task<IActionResult> OnPostPreviewAsync([FromBody] PayrollPreviewRequest req)
        => await ProxyPostAsync("/api/payroll-processing/preview", req);

    public async Task<IActionResult> OnPostProcessAsync([FromBody] PayrollPreviewRequest req)
        => await ProxyPostAsync("/api/payroll-processing/process", req);

    public async Task<IActionResult> OnGetRunsAsync()
        => await ProxyGetAsync("/api/payroll-processing/runs", "[]");

    public async Task<IActionResult> OnGetRunDetailsAsync(int runId)
        => await ProxyGetAsync("/api/payroll-processing/runs/" + runId, "{}");

    public async Task<IActionResult> OnGetPayslipPdfAsync(int runId)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
            return new EmptyResult();

        var client = _http.CreateClient("Subchron.API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.GetAsync(baseUrl + "/api/payroll-processing/runs/" + runId);
        if (!resp.IsSuccessStatusCode)
            return new EmptyResult();

        var body = await resp.Content.ReadAsStringAsync();
        var details = JsonSerializer.Deserialize<RunDetailsResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (details?.Employees == null || details.Employees.Count == 0)
            return new EmptyResult();

        QuestPDF.Settings.License = LicenseType.Community;
        var pdf = Document.Create(container =>
        {
            foreach (var emp in details.Employees)
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
                        col.Item().Text("Payroll Run #" + details.Run.PayrollRunID + "   Period: " + details.Run.PeriodStart.ToString("yyyy-MM-dd") + " to " + details.Run.PeriodEnd.ToString("yyyy-MM-dd"));
                        col.Item().Text(emp.EmployeeName + " (" + emp.EmpNumber + ")").Bold();
                        col.Item().Text("Department: " + emp.DepartmentName);

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); });
                            void Row(string k, string v)
                            {
                                t.Cell().Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text(k);
                                t.Cell().Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).AlignRight().Text(v);
                            }

                            Row("Worked Hours", emp.WorkedHours.ToString("0.00"));
                            Row("Overtime Hours", emp.OvertimeHours.ToString("0.00"));
                            Row("Base Pay", "PHP " + emp.BasePay.ToString("N2"));
                            Row("Overtime Pay", "PHP " + emp.OvertimePay.ToString("N2"));
                            Row("Allowances", "PHP " + emp.Allowances.ToString("N2"));
                            Row("Gross Pay", "PHP " + emp.GrossPay.ToString("N2"));
                            Row("Deductions", "PHP " + emp.Deductions.ToString("N2"));
                            Row("Tax", "PHP " + emp.Tax.ToString("N2"));
                            Row("Net Pay", "PHP " + emp.NetPay.ToString("N2"));
                        });

                        col.Item().PaddingTop(8).Text("Formula").Bold();
                        col.Item().Text(string.IsNullOrWhiteSpace(emp.FormulaSummary) ? "Net = (Base + OT + Allowances) - Deductions" : emp.FormulaSummary);
                    });
                });
            }
        }).GeneratePdf();

        return File(pdf, "application/pdf", "payslips-run-" + runId + ".pdf");
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

    private async Task<IActionResult> ProxyPostAsync(string path, object payload)
    {
        var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
        var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
            return new JsonResult(new { ok = false, message = "Not authenticated." });

        try
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await client.PostAsJsonAsync(baseUrl + path, payload);
            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) body = resp.IsSuccessStatusCode ? "{\"ok\":true}" : "{\"ok\":false}";
            return new ContentResult { StatusCode = (int)resp.StatusCode, ContentType = "application/json", Content = body };
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, message = ex.Message });
        }
    }

    public class PayrollPreviewRequest
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Mode { get; set; } = "batch";
        public int? EmpID { get; set; }
    }

    private class RunDetailsResponse
    {
        public RunInfo Run { get; set; } = new();
        public List<RunEmployeeInfo> Employees { get; set; } = new();
    }

    private class RunInfo
    {
        public int PayrollRunID { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    private class RunEmployeeInfo
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
