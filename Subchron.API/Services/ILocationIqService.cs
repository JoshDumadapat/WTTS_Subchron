using Subchron.API.Models.Location;

namespace Subchron.API.Services;

public interface ILocationIqService
{
    Task<IReadOnlyList<LocationAutocompleteResult>> AutocompleteAsync(string query, int limit = 5, CancellationToken ct = default);
    Task<LocationAutocompleteResult?> ReverseAsync(decimal lat, decimal lon, CancellationToken ct = default);
}
