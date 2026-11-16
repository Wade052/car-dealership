using System.Diagnostics;
using car_dealership.Data;
using car_dealership.Models.enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using car_dealership.Models;

namespace car_dealership.Controllers;

public class HomeController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var featured = await context.Cars.AsNoTracking()
            .Where(car => car.Status == Status.Available && (car.Photos.Any() || car.ImageUrl != null && car.ImageUrl != ""))
            .OrderByDescending(car => car.Year).ThenByDescending(car => car.Id)
            .Include(car => car.CarModel).ThenInclude(model => model!.Make)
            .Include(car => car.Photos)
            .FirstOrDefaultAsync();

        return View(featured);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
