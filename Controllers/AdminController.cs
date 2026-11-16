using car_dealership.Data;
using car_dealership.Models;
using car_dealership.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace car_dealership.Controllers;

[Authorize(Roles = "Manager")]
public sealed class AdminController(ApplicationDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var listings = await context.Cars.AsNoTracking()
            .Select(car => new { car.Status, car.Price })
            .ToListAsync(cancellationToken);
        var sold = listings.Count(car => car.Status == Status.Sold);

        return View(new InventorySummary(
            listings.Count,
            sold,
            listings.Count(car => car.Status == Status.Pending),
            listings.Count(car => car.Status == Status.Reserved),
            listings.Count(car => car.Status == Status.Available),
            listings.Where(car => car.Status != Status.Sold).Sum(car => car.Price)));
    }
}
