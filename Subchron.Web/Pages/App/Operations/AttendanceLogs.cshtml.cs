using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Subchron.Web.Pages.Auth;

namespace Subchron.Web.Pages.App.Operations
{
    public class AttendanceLogsModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public AttendanceLogsModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnGetRowsAsync(DateTime? from, DateTime? to, int? departmentId, int? empId, string? method)
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return new JsonResult(new List<object>());

            try
            {
                var q = new List<string>();
                if (from.HasValue) q.Add("from=" + Uri.EscapeDataString(from.Value.ToString("O")));
                if (to.HasValue) q.Add("to=" + Uri.EscapeDataString(to.Value.ToString("O")));
                if (departmentId.HasValue) q.Add("departmentId=" + departmentId.Value);
                if (empId.HasValue) q.Add("empId=" + empId.Value);
                if (!string.IsNullOrWhiteSpace(method)) q.Add("method=" + Uri.EscapeDataString(method));
                var url = baseUrl + "/api/attendance-logs/current" + (q.Count > 0 ? "?" + string.Join("&", q) : "");

                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var resp = await client.GetAsync(url);
                var body = await resp.Content.ReadAsStringAsync();
                return new ContentResult { StatusCode = (int)resp.StatusCode, ContentType = "application/json", Content = string.IsNullOrWhiteSpace(body) ? "[]" : body };
            }
            catch
            {
                return new JsonResult(new List<object>());
            }
        }

        public async Task<IActionResult> OnGetEmployeesAsync()
        {
            return await ProxyGetAsync("/api/employees?archivedOnly=false");
        }

        public async Task<IActionResult> OnGetDepartmentsAsync()
        {
            return await ProxyGetAsync("/api/departments");
        }

        public async Task<IActionResult> OnGetExportCsvAsync(DateTime? from, DateTime? to, int? departmentId, int? empId, string? method)
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return new EmptyResult();

            try
            {
                var q = new List<string>();
                if (from.HasValue) q.Add("from=" + Uri.EscapeDataString(from.Value.ToString("O")));
                if (to.HasValue) q.Add("to=" + Uri.EscapeDataString(to.Value.ToString("O")));
                if (departmentId.HasValue) q.Add("departmentId=" + departmentId.Value);
                if (empId.HasValue) q.Add("empId=" + empId.Value);
                if (!string.IsNullOrWhiteSpace(method)) q.Add("method=" + Uri.EscapeDataString(method));
                var url = baseUrl + "/api/attendance-logs/current/export.csv" + (q.Count > 0 ? "?" + string.Join("&", q) : "");

                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                    return new EmptyResult();

                var bytes = await resp.Content.ReadAsByteArrayAsync();
                var fileName = resp.Content.Headers.ContentDisposition?.FileNameStar ?? resp.Content.Headers.ContentDisposition?.FileName ?? $"attendance-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
                return File(bytes, "text/csv", fileName.Trim('"'));
            }
            catch
            {
                return new EmptyResult();
            }
        }

        public async Task<IActionResult> OnGetExportReportAsync(
            DateTime? from,
            DateTime? to,
            int? departmentId,
            int? empId,
            string? method,
            string? status,
            string? format,
            decimal? hourlyRate,
            bool includeRows = false,
            bool includeInsights = true)
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return new EmptyResult();

            try
            {
                var rows = await FetchAttendanceRowsAsync(baseUrl, token, from, to, departmentId, empId, method);
                if (!string.IsNullOrWhiteSpace(status))
                    rows = rows.Where(x => string.Equals(x.Status, status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

                var org = await FetchOrgProfileAsync(baseUrl, token);
                var summary = BuildSummary(rows, from, to, hourlyRate ?? 100m, org);

                var requested = (format ?? "csv").Trim().ToLowerInvariant();
                if (requested == "pdf")
                {
                    QuestPDF.Settings.License = LicenseType.Community;
                    var logoBytes = await TryFetchLogoAsync(baseUrl, org.LogoUrl);
                    var pdf = BuildPdf(summary, includeRows, includeInsights, logoBytes);
                    return File(pdf, "application/pdf", $"attendance-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf");
                }

                var csv = BuildCsv(summary, includeRows, includeInsights);
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"attendance-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
            }
            catch
            {
                return new EmptyResult();
            }
        }

        private async Task<List<AttendanceRowDto>> FetchAttendanceRowsAsync(string baseUrl, string token, DateTime? from, DateTime? to, int? departmentId, int? empId, string? method)
        {
            var q = new List<string>();
            if (from.HasValue) q.Add("from=" + Uri.EscapeDataString(from.Value.ToString("O")));
            if (to.HasValue) q.Add("to=" + Uri.EscapeDataString(to.Value.ToString("O")));
            if (departmentId.HasValue) q.Add("departmentId=" + departmentId.Value);
            if (empId.HasValue) q.Add("empId=" + empId.Value);
            if (!string.IsNullOrWhiteSpace(method)) q.Add("method=" + Uri.EscapeDataString(method));

            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var url = baseUrl + "/api/attendance-logs/current" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return new List<AttendanceRowDto>();

            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return new List<AttendanceRowDto>();

            return JsonSerializer.Deserialize<List<AttendanceRowDto>>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<AttendanceRowDto>();
        }

        private async Task<OrgProfileDto> FetchOrgProfileAsync(string baseUrl, string token)
        {
            var client = _http.CreateClient("Subchron.API");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.GetAsync(baseUrl + "/api/org-profile/current");
            if (!resp.IsSuccessStatusCode)
                return new OrgProfileDto();

            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return new OrgProfileDto();

            return JsonSerializer.Deserialize<OrgProfileDto>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new OrgProfileDto();
        }

        private async Task<byte[]?> TryFetchLogoAsync(string baseUrl, string? logoUrl)
        {
            if (string.IsNullOrWhiteSpace(logoUrl))
                return null;

            var resolved = logoUrl.Trim();
            if (resolved.StartsWith("~/", StringComparison.Ordinal))
                resolved = baseUrl + resolved.Substring(1);
            else if (resolved.StartsWith("/", StringComparison.Ordinal))
                resolved = baseUrl + resolved;

            try
            {
                var client = _http.CreateClient();
                return await client.GetByteArrayAsync(resolved);
            }
            catch
            {
                return null;
            }
        }

        private static AttendanceExportSummary BuildSummary(List<AttendanceRowDto> rows, DateTime? from, DateTime? to, decimal hourlyRate, OrgProfileDto org)
        {
            int SafeWorkedMinutes(AttendanceRowDto row)
            {
                if (row.WorkedMinutes > 0) return row.WorkedMinutes;
                if (row.TimeIn.HasValue && row.TimeOut.HasValue)
                {
                    var diff = (int)Math.Floor((row.TimeOut.Value - row.TimeIn.Value).TotalMinutes);
                    return Math.Max(0, diff);
                }
                return 0;
            }

            var statusOrder = new[]
            {
                "Present",
                "Timed In",
                "Late-In",
                "Undertime",
                "Overtime",
                "Late-In + Undertime",
                "No Record"
            };

            var statusCounts = statusOrder.ToDictionary(s => s, s => rows.Count(x => string.Equals(x.Status, s, StringComparison.OrdinalIgnoreCase)));
            var totalWorkedMinutes = rows.Sum(SafeWorkedMinutes);
            var totalLateMinutes = rows.Sum(x => Math.Max(0, x.LateMinutes));
            var totalUndertimeMinutes = rows.Sum(x => Math.Max(0, x.UndertimeMinutes));
            var totalOvertimeMinutes = rows.Sum(x => Math.Max(0, x.OvertimeMinutes));
            var estimatedCost = (decimal)totalWorkedMinutes / 60m * Math.Max(0m, hourlyRate);

            var insights = new List<string>();
            var total = Math.Max(1, rows.Count);
            var latePct = statusCounts["Late-In"] * 100m / total;
            var undertimePct = statusCounts["Undertime"] * 100m / total;
            var timedInPct = statusCounts["Timed In"] * 100m / total;
            var presentPct = statusCounts["Present"] * 100m / total;

            insights.Add($"Present days: {presentPct:0.0}% of tracked records.");
            if (latePct >= 15m)
                insights.Add($"Late arrivals are elevated ({latePct:0.0}%). Consider schedule reminders or grace-window review.");
            if (undertimePct >= 10m)
                insights.Add($"Undertime is notable ({undertimePct:0.0}%). Review shift end adherence and workload planning.");
            if (timedInPct > 0m)
                insights.Add($"There are open timed-in records ({timedInPct:0.0}%). Check for missed time-out entries.");
            if (totalOvertimeMinutes > totalUndertimeMinutes)
                insights.Add("Overtime minutes exceed undertime minutes. Verify overtime approvals and staffing distribution.");
            if (insights.Count == 1)
                insights.Add("Attendance behavior looks stable. Continue monitoring weekly variance.");

            return new AttendanceExportSummary
            {
                CompanyName = string.IsNullOrWhiteSpace(org.OrgName) ? "Organization" : org.OrgName.Trim(),
                LogoUrl = org.LogoUrl,
                Address = string.Join(", ", new[] { org.AddressLine1, org.AddressLine2, org.City, org.StateProvince, org.PostalCode, org.Country }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Contact = string.Join(" | ", new[] { org.ContactPhone, org.ContactEmail }.Where(x => !string.IsNullOrWhiteSpace(x))),
                From = from?.Date,
                To = to?.Date,
                GeneratedAt = DateTime.Now,
                HourlyRate = Math.Max(0m, hourlyRate),
                TotalRecords = rows.Count,
                UniqueEmployees = rows.Select(x => x.EmpID).Distinct().Count(),
                TotalWorkedMinutes = totalWorkedMinutes,
                TotalLateMinutes = totalLateMinutes,
                TotalUndertimeMinutes = totalUndertimeMinutes,
                TotalOvertimeMinutes = totalOvertimeMinutes,
                EstimatedLaborCost = estimatedCost,
                StatusCounts = statusCounts,
                Insights = insights,
                Rows = rows
                    .OrderBy(x => x.LogDate)
                    .ThenBy(x => x.EmployeeName)
                    .ToList()
            };
        }

        private static string BuildCsv(AttendanceExportSummary s, bool includeRows, bool includeInsights)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ATTENDANCE PERFORMANCE REPORT");
            sb.AppendLine();
            sb.AppendLine("REPORT INFORMATION");
            sb.AppendLine("Field,Value");
            sb.AppendLine($"Company,{Csv(s.CompanyName)}");
            sb.AppendLine($"Address,{Csv(s.Address)}");
            sb.AppendLine($"Contact,{Csv(s.Contact)}");
            sb.AppendLine($"Date Range,{Csv((s.From?.ToString("yyyy-MM-dd") ?? "-") + " to " + (s.To?.ToString("yyyy-MM-dd") ?? "-"))}");
            sb.AppendLine($"Generated At,{Csv(s.GeneratedAt.ToString("yyyy-MM-dd HH:mm"))}");
            sb.AppendLine($"Hourly Rate (PHP),{s.HourlyRate.ToString("0.00", CultureInfo.InvariantCulture)}");
            sb.AppendLine();

            sb.AppendLine("KPI SNAPSHOT");
            sb.AppendLine("Total Records,Unique Employees,Worked Hours,Late Hours,Undertime Hours,Overtime Hours,Estimated Labor Cost (PHP)");
            sb.AppendLine(string.Join(",",
                s.TotalRecords.ToString(CultureInfo.InvariantCulture),
                s.UniqueEmployees.ToString(CultureInfo.InvariantCulture),
                (s.TotalWorkedMinutes / 60m).ToString("0.00", CultureInfo.InvariantCulture),
                (s.TotalLateMinutes / 60m).ToString("0.00", CultureInfo.InvariantCulture),
                (s.TotalUndertimeMinutes / 60m).ToString("0.00", CultureInfo.InvariantCulture),
                (s.TotalOvertimeMinutes / 60m).ToString("0.00", CultureInfo.InvariantCulture),
                s.EstimatedLaborCost.ToString("0.00", CultureInfo.InvariantCulture)));
            sb.AppendLine();

            sb.AppendLine("STATUS DISTRIBUTION");
            sb.AppendLine("Status,Count,Share (%)");
            foreach (var kv in s.StatusCounts)
            {
                var pct = s.TotalRecords > 0 ? ((decimal)kv.Value / s.TotalRecords) * 100m : 0m;
                sb.AppendLine($"{Csv(kv.Key)},{kv.Value},{pct.ToString("0.00", CultureInfo.InvariantCulture)}");
            }

            if (includeInsights)
            {
                sb.AppendLine();
                sb.AppendLine("INSIGHTS AND RECOMMENDATIONS");
                sb.AppendLine("No,Insight");
                for (var i = 0; i < s.Insights.Count; i++)
                    sb.AppendLine($"{i + 1},{Csv(s.Insights[i])}");
            }

            if (includeRows)
            {
                sb.AppendLine();
                sb.AppendLine("DETAILED ATTENDANCE ROWS");
                sb.AppendLine("Employee,Employee Number,Department,Date,Time In,Time Out,Worked Hours,Late Minutes,Undertime Minutes,Overtime Minutes,Method,Status,Attendance Flag");
                foreach (var r in s.Rows)
                {
                    var worked = SafeWorkedMinutes(r);
                    var flag = worked <= 0
                        ? "No Work Hours"
                        : (r.LateMinutes > 0 || r.UndertimeMinutes > 0 ? "Needs Review" : "Normal");

                    sb.AppendLine(string.Join(",",
                        Csv(r.EmployeeName),
                        Csv(r.EmpNumber),
                        Csv(r.DepartmentName),
                        Csv(r.LogDate.ToString("yyyy-MM-dd")),
                        Csv(r.TimeIn?.ToString("yyyy-MM-dd hh:mm tt")),
                        Csv(r.TimeOut?.ToString("yyyy-MM-dd hh:mm tt")),
                        Csv((worked / 60m).ToString("0.00", CultureInfo.InvariantCulture)),
                        Csv(r.LateMinutes.ToString(CultureInfo.InvariantCulture)),
                        Csv(r.UndertimeMinutes.ToString(CultureInfo.InvariantCulture)),
                        Csv(r.OvertimeMinutes.ToString(CultureInfo.InvariantCulture)),
                        Csv(r.Method),
                        Csv(r.Status),
                        Csv(flag)));
                }
            }

            return sb.ToString();
        }

        private static byte[] BuildPdf(AttendanceExportSummary s, bool includeRows, bool includeInsights, byte[]? logoBytes)
        {
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor("#0F172A"));

                    page.Header().PaddingBottom(10).Column(col =>
                    {
                        col.Item().Background("#0F766E").Padding(12).Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(48).Height(48).Image(logoBytes);

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(s.CompanyName).FontSize(15).Bold().FontColor(Colors.White);
                                c.Item().Text("Attendance Workforce Report").FontSize(10).FontColor("#CCFBF1");
                            });

                            row.ConstantItem(170).AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text($"Generated: {s.GeneratedAt:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#CCFBF1");
                                c.Item().AlignRight().Text($"Range: {(s.From?.ToString("yyyy-MM-dd") ?? "-")} to {(s.To?.ToString("yyyy-MM-dd") ?? "-")}").FontSize(8).FontColor("#CCFBF1");
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(s.Address) || !string.IsNullOrWhiteSpace(s.Contact))
                        {
                            col.Item().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                            {
                                if (!string.IsNullOrWhiteSpace(s.Address)) c.Item().Text("Address: " + s.Address).FontSize(9).FontColor("#334155");
                                if (!string.IsNullOrWhiteSpace(s.Contact)) c.Item().Text("Contact: " + s.Contact).FontSize(9).FontColor("#334155");
                            });
                        }
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text("KPI Snapshot").Bold().FontSize(11);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            void Kpi(string label, string value)
                            {
                                t.Cell().Border(1).BorderColor("#D1FAE5").Background("#ECFDF5").Padding(8).Column(c =>
                                {
                                    c.Item().Text(label).FontSize(8).FontColor("#047857");
                                    c.Item().Text(value).FontSize(13).Bold().FontColor("#064E3B");
                                });
                            }

                            Kpi("Total Records", s.TotalRecords.ToString(CultureInfo.InvariantCulture));
                            Kpi("Unique Employees", s.UniqueEmployees.ToString(CultureInfo.InvariantCulture));
                            Kpi("Worked Hours", (s.TotalWorkedMinutes / 60m).ToString("0.00", CultureInfo.InvariantCulture));
                            Kpi("Late Hours", (s.TotalLateMinutes / 60m).ToString("0.00", CultureInfo.InvariantCulture));
                            Kpi("Undertime Hours", (s.TotalUndertimeMinutes / 60m).ToString("0.00", CultureInfo.InvariantCulture));
                            Kpi("Overtime Hours", (s.TotalOvertimeMinutes / 60m).ToString("0.00", CultureInfo.InvariantCulture));
                        });

                        col.Item().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text("Hourly Rate (PHP): " + s.HourlyRate.ToString("0.00", CultureInfo.InvariantCulture)).SemiBold();
                            row.RelativeItem().AlignRight().Text("Estimated Labor Cost: PHP " + s.EstimatedLaborCost.ToString("N2", CultureInfo.InvariantCulture)).SemiBold();
                        });

                        col.Item().PaddingTop(2).Text("Status Distribution").Bold().FontSize(11);
                        foreach (var kv in s.StatusCounts)
                        {
                            var pct = s.TotalRecords > 0 ? (decimal)kv.Value / s.TotalRecords : 0m;
                            var pctWidth = Math.Max(0f, Math.Min(100f, (float)(pct * 100m)));

                            col.Item().Border(1).BorderColor("#E2E8F0").Padding(6).Row(r =>
                            {
                                r.RelativeItem(2).Text(kv.Key).SemiBold();
                                r.RelativeItem(4).AlignMiddle().Height(10).Border(1).BorderColor("#CBD5E1").Background("#E2E8F0").Row(rr =>
                                {
                                    rr.RelativeItem(pctWidth <= 0 ? 0.01f : pctWidth).Background("#0F766E");
                                    rr.RelativeItem(Math.Max(0.01f, 100f - pctWidth));
                                });
                                r.ConstantItem(40).AlignRight().Text(kv.Value.ToString(CultureInfo.InvariantCulture));
                                r.ConstantItem(55).AlignRight().Text((pct * 100m).ToString("0.0", CultureInfo.InvariantCulture) + "%").FontColor("#475569");
                            });
                        }

                        if (includeInsights)
                        {
                            col.Item().PaddingTop(4).Text("Insights and Recommendations").Bold().FontSize(11);
                            col.Item().Background("#FFF7ED").Border(1).BorderColor("#FED7AA").Padding(8).Column(c =>
                            {
                                c.Spacing(4);
                                for (var i = 0; i < s.Insights.Count; i++)
                                    c.Item().Text($"{i + 1}. {s.Insights[i]}").FontColor("#7C2D12");
                            });
                        }

                        if (includeRows)
                        {
                            col.Item().PageBreak();
                            col.Item().Text("Detailed Attendance Rows").Bold().FontSize(11);
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2.2f);
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(1.4f);
                                    c.RelativeColumn(1.3f);
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(1f);
                                });

                                void Header(string text)
                                {
                                    t.Cell().Background("#F1F5F9").BorderBottom(1).BorderColor("#CBD5E1").PaddingVertical(4).PaddingHorizontal(3)
                                        .Text(text).FontSize(8).SemiBold().FontColor("#334155");
                                }

                                Header("Employee");
                                Header("Date");
                                Header("Department");
                                Header("Worked (h)");
                                Header("Late (m)");
                                Header("Status");

                                for (var i = 0; i < s.Rows.Count; i++)
                                {
                                    var r = s.Rows[i];
                                    var worked = SafeWorkedMinutes(r);
                                    var rowBg = i % 2 == 0 ? "#FFFFFF" : "#F8FAFC";

                                    t.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).Text(r.EmployeeName ?? "-").FontSize(8);
                                    t.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).Text(r.LogDate.ToString("yyyy-MM-dd")).FontSize(8);
                                    t.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).Text(r.DepartmentName ?? "-").FontSize(8);
                                    t.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).AlignRight().Text((worked / 60m).ToString("0.00", CultureInfo.InvariantCulture)).FontSize(8);
                                    t.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).AlignRight().Text(r.LateMinutes.ToString(CultureInfo.InvariantCulture)).FontSize(8);
                                    t.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(3).Text(r.Status ?? "-").FontSize(8);
                                }
                            });
                        }
                    });
                });
            }).GeneratePdf();

            return pdf;
        }

        private static string Csv(string? value)
        {
            var v = value ?? string.Empty;
            var escaped = v.Replace("\"", "\"\"");
            return "\"" + escaped + "\"";
        }

        private static int SafeWorkedMinutes(AttendanceRowDto row)
        {
            if (row.WorkedMinutes > 0)
                return row.WorkedMinutes;

            if (row.TimeIn.HasValue && row.TimeOut.HasValue)
                return Math.Max(0, (int)Math.Floor((row.TimeOut.Value - row.TimeIn.Value).TotalMinutes));

            return 0;
        }

        private async Task<IActionResult> ProxyGetAsync(string path)
        {
            var token = User.FindFirst(CompleteLoginModel.AccessTokenClaimType)?.Value;
            var baseUrl = (_config["ApiBaseUrl"] ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(baseUrl))
                return new JsonResult(new List<object>());

            try
            {
                var client = _http.CreateClient("Subchron.API");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var resp = await client.GetAsync(baseUrl + path);
                var body = await resp.Content.ReadAsStringAsync();
                return new ContentResult { StatusCode = (int)resp.StatusCode, ContentType = "application/json", Content = string.IsNullOrWhiteSpace(body) ? "[]" : body };
            }
            catch
            {
                return new JsonResult(new List<object>());
            }
        }

        private sealed class AttendanceRowDto
        {
            public int AttendanceID { get; set; }
            public int EmpID { get; set; }
            public string? EmployeeName { get; set; }
            public string? EmpNumber { get; set; }
            public string? DepartmentName { get; set; }
            public DateOnly LogDate { get; set; }
            public DateTime? TimeIn { get; set; }
            public DateTime? TimeOut { get; set; }
            public string? Method { get; set; }
            public string? Status { get; set; }
            public int WorkedMinutes { get; set; }
            public int LateMinutes { get; set; }
            public int UndertimeMinutes { get; set; }
            public int OvertimeMinutes { get; set; }
        }

        private sealed class OrgProfileDto
        {
            public string? OrgName { get; set; }
            public string? LogoUrl { get; set; }
            public string? AddressLine1 { get; set; }
            public string? AddressLine2 { get; set; }
            public string? City { get; set; }
            public string? StateProvince { get; set; }
            public string? PostalCode { get; set; }
            public string? Country { get; set; }
            public string? ContactEmail { get; set; }
            public string? ContactPhone { get; set; }
        }

        private sealed class AttendanceExportSummary
        {
            public string CompanyName { get; set; } = "Organization";
            public string? LogoUrl { get; set; }
            public string? Address { get; set; }
            public string? Contact { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public DateTime GeneratedAt { get; set; }
            public decimal HourlyRate { get; set; }
            public int TotalRecords { get; set; }
            public int UniqueEmployees { get; set; }
            public int TotalWorkedMinutes { get; set; }
            public int TotalLateMinutes { get; set; }
            public int TotalUndertimeMinutes { get; set; }
            public int TotalOvertimeMinutes { get; set; }
            public decimal EstimatedLaborCost { get; set; }
            public Dictionary<string, int> StatusCounts { get; set; } = new();
            public List<string> Insights { get; set; } = new();
            public List<AttendanceRowDto> Rows { get; set; } = new();
        }
    }
}
