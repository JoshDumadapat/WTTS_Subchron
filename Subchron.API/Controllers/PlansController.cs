using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;

namespace Subchron.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly SubchronDbContext _db;

    public PlansController(SubchronDbContext db)
    {
        _db = db;
    }

    // Returns active plans with display prices for signup and billing; Standard has a 7-day trial then is charged.
    [HttpGet]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _db.Plans
            .Where(p => p.IsActive)
            .Select(p => new { p.PlanID, p.PlanName, p.BasePrice })
            .ToListAsync();

        var result = plans.Select(p => new
        {
            p.PlanID,
            p.PlanName,
            BasePrice = GetDisplayPrice(p.PlanName),
            IsFreeTrial = p.PlanName == "Standard",
            TrialDays = p.PlanName == "Standard" ? 7 : 0
        }).ToList();
        return Ok(result);
    }

    // Display price for each plan; Standard is charged this after the 7-day trial.
    private static decimal GetDisplayPrice(string planName)
    {
        return planName switch
        {
            "Basic" => 2499m,
            "Standard" => 5999m,
            "Enterprise" => 8999m,
            _ => 0m
        };
    }
}
