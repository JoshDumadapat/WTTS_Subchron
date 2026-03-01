using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Subchron.API.Models.Location;
using Subchron.API.Models.Settings;

namespace Subchron.API.Services;

public class LocationIqService : ILocationIqService
{
    private readonly HttpClient _http;
    private readonly LocationIqSettings _settings;

    public LocationIqService(HttpClient http, IOptions<LocationIqSettings> opts)
    {
        _http = http;
        _settings = opts.Value;
    }

    public async Task<IReadOnlyList<LocationAutocompleteResult>> AutocompleteAsync(string query, int limit = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<LocationAutocompleteResult>();

        var url = $"{_settings.BaseUrl}autocomplete?key={_settings.ApiKey}&q={Uri.EscapeDataString(query)}&limit={limit}&dedupe=1&format=json";
        var data = await _http.GetFromJsonAsync<List<LocationIqPlaceDto>>(url, ct) ?? new List<LocationIqPlaceDto>();
        return data.Select(Map).ToList();
    }

    public async Task<LocationAutocompleteResult?> ReverseAsync(decimal lat, decimal lon, CancellationToken ct = default)
    {
        var url = $"{_settings.BaseUrl}reverse?key={_settings.ApiKey}&lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}&format=json";
        var data = await _http.GetFromJsonAsync<LocationIqPlaceDto>(url, ct);
        return data == null ? null : Map(data);
    }

    private static LocationAutocompleteResult Map(LocationIqPlaceDto dto)
    {
        var address = dto.address;
        var city = address?.city ?? address?.town ?? address?.village;
        var line = string.Join(" ", new[] { address?.house_number, address?.road }.Where(s => !string.IsNullOrWhiteSpace(s)));
        decimal? lat = decimal.TryParse(dto.lat, NumberStyles.Any, CultureInfo.InvariantCulture, out var latVal) ? latVal : null;
        decimal? lon = decimal.TryParse(dto.lon, NumberStyles.Any, CultureInfo.InvariantCulture, out var lonVal) ? lonVal : null;
        return new LocationAutocompleteResult(
            DisplayName: dto.display_name,
            AddressLine: string.IsNullOrWhiteSpace(line) ? null : line,
            City: city,
            State: address?.state,
            PostalCode: address?.postcode,
            Country: address?.country,
            Lat: lat,
            Lon: lon
        );
    }
}
