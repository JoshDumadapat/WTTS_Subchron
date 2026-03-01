namespace Subchron.API.Models.Location;

public record LocationIqPlaceDto(
    string display_name,
    string? lat,
    string? lon,
    LocationIqAddressDto? address
);

public record LocationIqAddressDto(
    string? house_number,
    string? road,
    string? suburb,
    string? city,
    string? town,
    string? village,
    string? state,
    string? postcode,
    string? country
);

public record LocationAutocompleteResult(
    string DisplayName,
    string? AddressLine,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    decimal? Lat,
    decimal? Lon
);
