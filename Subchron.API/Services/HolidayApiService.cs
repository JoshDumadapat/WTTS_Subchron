using System.Text.Json;
using Microsoft.Extensions.Options;
using Subchron.API.Models.Settings;

namespace Subchron.API.Services;

public class HolidayApiService : IHolidayApiService
{
    private readonly HttpClient _http;
    private readonly HolidayApiSettings _settings;

    public HolidayApiService(HttpClient http, IOptions<HolidayApiSettings> options)
    {
        _http = http;
        _settings = options.Value;
    }

    public async Task<IReadOnlyList<HolidayApiHoliday>> GetPhilippinesHolidaysAsync(int year, CancellationToken ct = default)
    {
        if (year < 1900 || year > 2200)
            return Array.Empty<HolidayApiHoliday>();
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return Array.Empty<HolidayApiHoliday>();

        var baseUrl = (_settings.BaseUrl ?? "https://holidayapi.com/v1").TrimEnd('/');
        var url = $"{baseUrl}/holidays?country=PH&year={year}&pretty=false&key={Uri.EscapeDataString(_settings.ApiKey)}";

        try
        {
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                return Array.Empty<HolidayApiHoliday>();

            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("holidays", out var holidaysEl) || holidaysEl.ValueKind != JsonValueKind.Array)
                return Array.Empty<HolidayApiHoliday>();

            var list = new List<HolidayApiHoliday>();
            foreach (var el in holidaysEl.EnumerateArray())
            {
                var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                var dateText = el.TryGetProperty("date", out var d) ? d.GetString() : null;
                var type = el.TryGetProperty("holiday_type", out var t) && t.ValueKind == JsonValueKind.String
                    ? t.GetString()
                    : el.TryGetProperty("type", out var t2) && t2.ValueKind == JsonValueKind.String
                        ? t2.GetString()
                        : "RegularHoliday";
                var isPublic = el.TryGetProperty("public", out var p) && p.ValueKind == JsonValueKind.True;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(dateText))
                    continue;
                if (!DateTime.TryParse(dateText, out var parsedDate))
                    continue;

                list.Add(new HolidayApiHoliday(name.Trim(), parsedDate.Date, NormalizeHolidayType(type, name), isPublic));
            }

            return list
                .GroupBy(x => new { Date = x.Date.Date, Name = x.Name.ToLowerInvariant() })
                .Select(g => g.First())
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Name)
                .ToList();
        }
        catch
        {
            return Array.Empty<HolidayApiHoliday>();
        }
    }

    private static string NormalizeHolidayType(string? apiType, string holidayName)
    {
        var name = (holidayName ?? string.Empty).ToLowerInvariant();
        var type = (apiType ?? string.Empty).ToLowerInvariant();

        if (name.Contains("special working")) return "SpecialWorkingHoliday";
        if (name.Contains("special non-working") || name.Contains("special non working")) return "SpecialNonWorkingHoliday";
        if (name.Contains("regular holiday")) return "RegularHoliday";
        if (name.Contains("double")) return "DoubleHoliday";

        if (type.Contains("special") && type.Contains("working")) return "SpecialWorkingHoliday";
        if (type.Contains("special")) return "SpecialNonWorkingHoliday";

        return "RegularHoliday";
    }
}
