using car_dealership.Data;
using car_dealership.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace car_dealership.Controllers;

public class CarsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly string _photoDirectory;

    public CarsController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _photoDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "car-photos");
    }

    [HttpGet]
    public async Task<IActionResult> Index(InventorySearch search)
    {
        var inventory = _context.Cars.AsNoTracking();
        var makes = await inventory.Where(car => car.CarModel != null)
            .Select(car => new { car.CarModel!.MakeId, car.CarModel.Make.Name })
            .Distinct().OrderBy(make => make.Name).ToListAsync();
        search.Makes = makes.Select(make => new SelectListItem(make.Name, make.MakeId.ToString())).ToList();

        var years = await inventory.Select(car => car.Year).Distinct().OrderByDescending(year => year).ToListAsync();
        search.Years = years.Select(year => new SelectListItem(year.ToString(), year.ToString())).ToList();
        // Round up for the slider so the most expensive car is within reach.
        var highestPrice = await inventory.MaxAsync(car => (double?)car.Price) ?? 0;
        search.SliderMaxPrice = Math.Max(1, Math.Ceiling(highestPrice));

        if (search.MinPrice < 0)
            ModelState.AddModelError(nameof(search.MinPrice), "Enter a minimum price of zero or more.");
        if (search.MaxPrice < 0)
            ModelState.AddModelError(nameof(search.MaxPrice), "Enter a maximum price of zero or more.");
        if (search.MinPrice > search.MaxPrice)
            ModelState.AddModelError(nameof(search.MaxPrice), "Maximum price must be at least the minimum price.");

        if (!ModelState.IsValid) return View(search);

        if (search.Type is { } type)
            inventory = inventory.Where(car => car.Type == type);
        if (search.MakeId is { } makeId)
            inventory = inventory.Where(car => car.CarModel != null && car.CarModel.MakeId == makeId);
        if (search.Location is { } location)
            inventory = inventory.Where(car => car.Location == location);
        if (search.Year is { } year)
            inventory = inventory.Where(car => car.Year == year);
        if (search.MinPrice is { } minPrice)
            inventory = inventory.Where(car => car.Price >= minPrice);
        if (search.MaxPrice is { } maxPrice)
            inventory = inventory.Where(car => car.Price <= maxPrice);

        search.Cars = await inventory.Include(car => car.CarModel).ThenInclude(model => model!.Make)
            .Include(car => car.Photos).OrderBy(car => car.Id).ToListAsync();
        return View(search);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var car = await _context.Cars.AsNoTracking()
            .Include(car => car.CarModel).ThenInclude(model => model!.Make)
            .Include(car => car.Photos).SingleOrDefaultAsync(car => car.Id == id);
        return car is null ? NotFound() : View(car);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var car = await FindCarForColorEdit(id);
        if (car is null) return NotFound();

        return ShowColorEditForm(car, new EditCarColorsInput
        {
            Trim = car.Trim,
            Location = car.Location,
            ExteriorColor = car.ExteriorColor,
            InteriorColor = car.InteriorColor,
            Status = car.Status
        });
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, EditCarColorsInput input)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var car = await FindCarForColorEdit(id);
        if (car is null) return NotFound();
        if (!ModelState.IsValid) return ShowColorEditForm(car, input);
        if (input.Status != Models.enums.Status.Sold &&
            await _context.CarPurchases.AnyAsync(purchase => purchase.CarId == id))
        {
            ModelState.AddModelError(nameof(input.Status), "A completed demo purchase keeps this listing marked Sold.");
            return ShowColorEditForm(car, input);
        }

        // Update only the fields exposed by the listing editor.
        car.Trim = input.Trim?.Trim();
        car.Location = input.Location;
        car.ExteriorColor = input.ExteriorColor?.Trim() ?? "";
        car.InteriorColor = input.InteriorColor?.Trim() ?? "";
        car.Status = input.Status!.Value;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction(nameof(Details), new { id = car.Id });
    }

    private Task<Car?> FindCarForColorEdit(int id) => _context.Cars
        .Include(car => car.CarModel).ThenInclude(model => model!.Make)
        .SingleOrDefaultAsync(car => car.Id == id);

    private IActionResult ShowColorEditForm(Car car, EditCarColorsInput input)
    {
        input.CarId = car.Id;
        input.VehicleName = $"{car.Year} {car.CarModel?.Make.Name} {car.CarModel?.Name}".Trim();
        return View(nameof(Edit), input);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public Task<IActionResult> Create() => ShowCreateForm(new CreateCarInput());

    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(CreateCarInput.MaxPhotos * CreateCarInput.MaxPhotoBytes + 1024 * 1024)]
    public async Task<IActionResult> Create(CreateCarInput input)
    {
        // Verify the submitted make, model, and year against the imported catalog.
        if (ModelState.IsValid && !await _context.CarModelYears.AnyAsync(entry =>
                entry.CarModelId == input.CarModelId && entry.CarModel.MakeId == input.MakeId &&
                entry.Year == input.Year))
        {
            ModelState.AddModelError(nameof(input.Year), "Choose a year available for that make and model.");
        }

        var acceptedPhotos = new List<(IFormFile File, string Extension)>();
        if (input.Photos.Count > CreateCarInput.MaxPhotos)
        {
            ModelState.AddModelError(nameof(input.Photos), $"Choose up to {CreateCarInput.MaxPhotos} photos.");
        }
        else
        {
            for (var index = 0; index < input.Photos.Count; index++)
            {
                var photo = input.Photos[index];
                if (photo.Length == 0 || photo.Length > CreateCarInput.MaxPhotoBytes)
                {
                    ModelState.AddModelError(nameof(input.Photos), $"Photo {index + 1} must be between 1 byte and 5 MB.");
                    continue;
                }

                var extension = await GetPhotoExtension(photo);
                if (extension is null)
                    ModelState.AddModelError(nameof(input.Photos), $"Photo {index + 1} must be a JPEG, PNG, or WebP image.");
                else
                    acceptedPhotos.Add((photo, extension));
            }
        }

        if (!ModelState.IsValid) return await ShowCreateForm(input);

        var car = new Car
        {
            CarModelId = input.CarModelId,
            Year = input.Year,
            Trim = input.Trim?.Trim(),
            Location = input.Location,
            Price = input.Price!.Value,
            Mileage = input.Mileage!.Value,
            VIN = input.VIN.ToUpperInvariant(),
            ExteriorColor = input.ExteriorColor?.Trim() ?? "",
            InteriorColor = input.InteriorColor?.Trim() ?? "",
            Type = input.Type!.Value,
            Fuel = input.Fuel!.Value,
            Transmission = input.Transmission!.Value,
            Drivetrain = input.Drivetrain!.Value,
            Engine = input.Engine?.Trim() ?? "",
            Horsepower = input.Horsepower,
            Doors = input.Doors,
            Seats = input.Seats,
            Condition = input.Condition!.Value,
            Title = input.Title!.Value,
            Status = input.Status!.Value,
            HasWarranty = input.HasWarranty,
            WarrantyExpiration = input.WarrantyExpiration?.Date,
            Description = input.Description?.Trim() ?? ""
        };

        var writtenPaths = new List<string>();
        try
        {
            if (acceptedPhotos.Count > 0)
                Directory.CreateDirectory(_photoDirectory);

            foreach (var (file, extension) in acceptedPhotos)
            {
                var name = $"{Guid.NewGuid():N}{extension}";
                var path = Path.Combine(_photoDirectory, name);
                await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                writtenPaths.Add(path);
                await file.CopyToAsync(output);
                car.Photos.Add(new CarPhoto
                {
                    Url = Url.Action(nameof(Photo), "Cars", new { name })
                        ?? throw new InvalidOperationException("Could not generate a photo URL."),
                    DisplayOrder = car.Photos.Count
                });
            }

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // A failed save should not leave part of a gallery on disk.
            foreach (var path in writtenPaths)
                System.IO.File.Delete(path);
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Photo(string? name)
    {
        if (string.IsNullOrEmpty(name)) return NotFound();
        var extension = Path.GetExtension(name);
        var contentType = extension switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };

        // Serve only generated image filenames from the inventory photo directory.
        if (contentType is null || !Guid.TryParseExact(name[..^extension.Length], "N", out _))
            return NotFound();

        var path = Path.Combine(_photoDirectory, name);
        return System.IO.File.Exists(path) ? PhysicalFile(path, contentType) : NotFound();
    }

    private async Task<IActionResult> ShowCreateForm(CreateCarInput input)
    {
        var makes = await _context.CarModelYears
            .Select(entry => new { entry.CarModel.MakeId, entry.CarModel.Make.Name })
            .Distinct().OrderBy(make => make.Name).ToListAsync();
        input.Makes = makes.Select(make => new SelectListItem(make.Name, make.MakeId.ToString())).ToList();
        return View(nameof(Create), input);
    }

    private static async Task<string?> GetPhotoExtension(IFormFile photo)
    {
        await using var stream = photo.OpenReadStream();
        var header = new byte[12];
        var count = await stream.ReadAtLeastAsync(header, header.Length, false);
        if (count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ".png";
        if (count >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255)
            return ".jpg";
        if (count == 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return ".webp";
        return null;
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public async Task<IActionResult> ModelOptions(int makeId)
    {
        var models = await _context.CarModelYears
            .Where(entry => entry.CarModel.MakeId == makeId)
            .Select(entry => new { id = entry.CarModelId, name = entry.CarModel.Name })
            .Distinct().OrderBy(model => model.name).ToListAsync();
        return Json(models);
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public async Task<IActionResult> YearOptions(int carModelId)
    {
        var years = await _context.CarModelYears
            .Where(entry => entry.CarModelId == carModelId)
            .Select(entry => entry.Year).Distinct().OrderByDescending(year => year)
            .Select(year => new { id = year, name = year }).ToListAsync();
        return Json(years);
    }
}

