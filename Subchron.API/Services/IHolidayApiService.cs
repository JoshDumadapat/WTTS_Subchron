namespace Subchron.API.Services;

public interface IHolidayApiService
{
    Task<IReadOnlyList<HolidayApiHoliday>> GetPhilippinesHolidaysAsync(int year, CancellationToken ct = default);
}

public record HolidayApiHoliday(
    string Name,
    DateTime Date,
    string Type,
    bool IsPublic
);
