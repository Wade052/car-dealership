using car_dealership.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using car_dealership.Data;

// Import mode runs from the terminal; it never starts the web server or creates users.
var importCatalog = args.Contains("--import-catalog");
var builder = WebApplication.CreateBuilder(importCatalog ? Array.Empty<string>() : args);

// Keep terminal import progress readable instead of printing every SQL statement.
if (importCatalog)
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (importCatalog)
{
    int ReadNumber(string option, int fallback)
    {
        var index = Array.IndexOf(args, option);
        if (index < 0) return fallback;
        if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out var value))
            throw new ArgumentException($"{option} requires a whole number.");
        return value;
    }

    var from = ReadNumber("--from", 1996);
    var to = ReadNumber("--to", DateTime.UtcNow.Year + 1);
    int? make = args.Contains("--make-id") ? ReadNumber("--make-id", 0) : null;
    // Use a comma-separated list to finish only the makes needed for inventory.
    HashSet<string>? makes = null;
    var makesIndex = Array.IndexOf(args, "--makes");
    if (makesIndex >= 0)
    {
        if (make.HasValue || makesIndex + 1 >= args.Length || args[makesIndex + 1].StartsWith("--"))
            throw new ArgumentException("Use --makes with a comma-separated list, without --make-id.");
        makes = args[makesIndex + 1]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (makes.Count == 0) throw new ArgumentException("--makes requires at least one make.");
    }
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
    await using var importScope = app.Services.CreateAsyncScope();
    var database = importScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("DealershipCatalogImporter/1.0");
    try
    {
        // Apply pending schema migrations before writing catalog data.
        await database.Database.MigrateAsync(cancellation.Token);
        Environment.ExitCode = await new VehicleCatalogImporter(database, http)
            .RunAsync(from, to, make, cancellation.Token, makes);
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
        Console.WriteLine("Import stopped. Completed batches were saved; rerun to resume.");
        Environment.ExitCode = 2;
    }
    return;
}

// This is used to check if the current user has the Role "Manager"

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Manager"))
    {
        var result = await roleManager.CreateAsync(
            new IdentityRole("Manager"));

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<IdentityUser>>();

    var user = await userManager.FindByEmailAsync("Test2@gmail.com");

    // Seed the demo manager so a fresh database has a usable login.
    const string managerPassword = "Abc123!";
    if (user is null)
    {
        user = new IdentityUser
        {
            UserName = "Test2@gmail.com",
            Email = "Test2@gmail.com",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, managerPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
    else
    {
        // Keep the existing demo account usable with the seeded password.
        if (!await userManager.CheckPasswordAsync(user, managerPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, managerPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }
    }

    if(!await userManager.IsInRoleAsync(user, "Manager"))
    {
        var result = await userManager.AddToRoleAsync(user, "Manager");

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
