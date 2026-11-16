using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using car_dealership.Data;
using car_dealership.Models;
using car_dealership.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace car_dealership.Controllers;

[Authorize]
public sealed class PurchasesController(ApplicationDbContext context, IDataProtectionProvider protection) : Controller
{
    private readonly ITimeLimitedDataProtector _quotes = protection
        .CreateProtector("Motorline.DemoPurchase.v1").ToTimeLimitedDataProtector();

    [HttpGet]
    public async Task<IActionResult> Buy([FromRoute] int id)
    {
        var car = await FindVehicle(id);
        if (car is null) return NotFound();
        return View(new BuyCarInput { Vehicle = car, DownPayment = 0m });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Buy([FromRoute] int id, BuyCarInput input)
    {
        var car = await FindVehicle(id);
        if (car is null) return NotFound();
        input.Vehicle = car;
        if (car.Status != Status.Available) return View(input);

        if (input.Months is not (24 or 36 or 48 or 60 or 72))
            ModelState.AddModelError(nameof(input.Months), "Select one of the available terms.");
        if (input.DownPayment is { } down && (down > car.Price || decimal.Round(down, 2) != down))
            ModelState.AddModelError(nameof(input.DownPayment), "Enter a dollar amount up to the vehicle price, with at most two decimal places.");
        if (car.Price is < 0 or > 1_000_000_000m)
            ModelState.AddModelError(string.Empty, "This listing price is outside the range of the demo calculator.");
        if (!ModelState.IsValid) return View(input);

        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (buyerId is null) return Forbid();
        var downPayment = input.DownPayment!.Value;
        var annualRate = downPayment == car.Price ? 0m : PurchaseQuote.ExampleRate(input.CreditScore!.Value);
        var estimate = new PurchaseQuote(car.Id, buyerId, car.Price, downPayment, input.Months, annualRate);
        input.Estimate = estimate;
        // Keep the estimate tamper resistant and omit the credit score from the token and purchase record.
        input.ConfirmationToken = _quotes.Protect(JsonSerializer.Serialize(estimate), TimeSpan.FromMinutes(15));
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm([FromRoute] int id, string? confirmationToken)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (buyerId is null) return Forbid();

        PurchaseQuote? estimate = null;
        if (!string.IsNullOrWhiteSpace(confirmationToken))
        {
            try
            {
                estimate = JsonSerializer.Deserialize<PurchaseQuote>(_quotes.Unprotect(confirmationToken));
            }
            catch (CryptographicException)
            {
                // Invalid and expired quotes both require a fresh estimate.
            }
        }

        if (estimate is null || estimate.CarId != id || estimate.BuyerId != buyerId)
        {
            TempData["PurchaseMessage"] = "This estimate is unavailable or has expired. Calculate a fresh estimate.";
            return RedirectToAction(nameof(Buy), new { id });
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        var existing = await context.CarPurchases.AsNoTracking().SingleOrDefaultAsync(purchase => purchase.CarId == id);
        if (existing?.BuyerId == buyerId)
            return RedirectToAction(nameof(Receipt), new { id = existing.Id });

        // Compare numeric prices inside the transaction. SQLite stores these decimals as text.
        var currentPrice = await context.Cars.Where(car => car.Id == id)
            .Select(car => (decimal?)car.Price).SingleOrDefaultAsync();
        if (currentPrice != estimate.Price)
        {
            TempData["PurchaseMessage"] = "The listing price changed. Calculate a fresh estimate.";
            return RedirectToAction(nameof(Buy), new { id });
        }

        // The guarded update and purchase insert commit together. Only one buyer can claim the car.
        var updated = await context.Cars
            .Where(car => car.Id == id && car.Status == Status.Available
                && !context.CarPurchases.Any(purchase => purchase.CarId == car.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(car => car.Status, Status.Sold));
        if (updated == 0)
        {
            TempData["PurchaseMessage"] = "The listing changed or is no longer available. Review the current listing before confirming.";
            return RedirectToAction(nameof(Buy), new { id });
        }

        var purchase = new CarPurchase
        {
            CarId = id,
            BuyerId = buyerId,
            PurchasedAtUtc = DateTime.UtcNow,
            Price = estimate.Price,
            DownPayment = estimate.DownPayment,
            Months = estimate.Months,
            AnnualRate = estimate.AnnualRate
        };
        context.CarPurchases.Add(purchase);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction(nameof(Receipt), new { id = purchase.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Receipt([FromRoute] int id)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (buyerId is null) return Forbid();

        var purchase = await context.CarPurchases.AsNoTracking()
            .Include(purchase => purchase.Car.CarModel).ThenInclude(model => model!.Make)
            .SingleOrDefaultAsync(purchase => purchase.Id == id && purchase.BuyerId == buyerId);
        return purchase is null ? NotFound() : View(purchase);
    }

    private Task<Car?> FindVehicle(int id) => context.Cars.AsNoTracking()
        .Include(car => car.CarModel).ThenInclude(model => model!.Make)
        .Include(car => car.Photos)
        .SingleOrDefaultAsync(car => car.Id == id);
}
