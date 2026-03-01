using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subchron.API.Services;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/location")]
public class LocationController : ControllerBase
{
    private readonly ILocationIqService _location;

    public LocationController(ILocationIqService location)
    {
        _location = location;
    }

    [AllowAnonymous]
    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] int limit = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
            return Ok(Array.Empty<object>());
        var results = await _location.AutocompleteAsync(q.Trim(), Math.Clamp(limit, 3, 10), ct);
        return Ok(results);
    }

    [AllowAnonymous]
    [HttpGet("reverse")]
    public async Task<IActionResult> Reverse([FromQuery] decimal lat, [FromQuery] decimal lon, CancellationToken ct = default)
    {
        var result = await _location.ReverseAsync(lat, lon, ct);
        return result == null ? NotFound() : Ok(result);
    }
}
