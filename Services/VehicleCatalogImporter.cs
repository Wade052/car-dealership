using System.Net;
using System.Text.Json;
using car_dealership.Data;
using car_dealership.Models;
using Microsoft.EntityFrameworkCore;

namespace car_dealership.Services;

// Explicit command-line import only: ordinary website requests never call NHTSA.
public sealed class VehicleCatalogImporter(ApplicationDbContext db, HttpClient http)
{
    private const string Api = "https://vpic.nhtsa.dot.gov/api/vehicles/";
    private readonly string[] types = ["Passenger Car", "Truck", "Multipurpose Passenger Vehicle (MPV)"];

    public async Task<int> RunAsync(int from, int to, int? makeFilter, CancellationToken token, IReadOnlySet<string>? makeNames = null)
    {
        // NHTSA documents this endpoint for model years greater than 1995.
        if (from < 1996 || to < from || to > DateTime.UtcNow.Year + 1)
            throw new ArgumentException("Use years from 1996 through next calendar year.");

        if (makeFilter <= 0) throw new ArgumentException("Make ID must be positive.");
        var foundMake = false;
        var foundNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var failures = 0;
        foreach (var type in types)
        {
            var makePath = $"GetMakesForVehicleType/{Uri.EscapeDataString(type)}?format=json";
            string makeJson;
            try
            {
                // Cache discovery too, so a resumed run does not need these calls again.
                var cached = await db.CatalogImportRequests.FindAsync([makePath], token);
                if (cached?.CompletedAt is not null)
                    makeJson = cached.ResponseJson!;
                else
                {
                    makeJson = await FetchAsync(makePath, token);
                    using var check = JsonDocument.Parse(makeJson);
                    if (check.RootElement.GetProperty("Results").GetArrayLength() == 0)
                        throw new InvalidDataException("Make discovery returned no results.");
                    await CompleteAsync(makePath, makeJson, token);
                    await db.SaveChangesAsync(token);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested)
            {
                await RecordFailureAsync(makePath, ex, token);
                failures++;
                continue;
            }

            using var document = JsonDocument.Parse(makeJson);
            foreach (var entry in document.RootElement.GetProperty("Results").EnumerateArray())
            {
                var apiMakeId = entry.GetProperty("MakeId").GetInt32();
                if (makeFilter.HasValue && makeFilter != apiMakeId) continue;
                var makeName = entry.GetProperty("MakeName").GetString()!.Trim();
                if (makeNames is not null && !makeNames.Contains(makeName)) continue;
                foundMake = true;
                foundNames.Add(makeName);

                // Newest years first: a partial import is useful before the full run finishes.
                for (var year = to; year >= from; year--)
                {
                    token.ThrowIfCancellationRequested();
                    var path = $"GetModelsForMakeIdYear/makeId/{apiMakeId}/modelyear/{year}/vehicletype/{Uri.EscapeDataString(type)}?format=json";
                    if (await db.CatalogImportRequests.AnyAsync(r => r.Id == path && r.CompletedAt != null, token))
                        continue;

                    try
                    {
                        var json = await FetchAsync(path, token);
                        // One API result and its checkpoint commit together. A crash cannot
                        // leave a checkpoint that incorrectly says unsaved data was imported.
                        await using var transaction = await db.Database.BeginTransactionAsync(token);
                        await ImportModelsAsync(apiMakeId, makeName, year, type, json, token);
                        await CompleteAsync(path, json, token);
                        await db.SaveChangesAsync(token);
                        await transaction.CommitAsync(token);
                        Console.WriteLine($"Imported {makeName} / {year} / {type}");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested)
                    {
                        await RecordFailureAsync(path, ex, token);
                        failures++;
                    }
                    finally { db.ChangeTracker.Clear(); }
                }
            }
        }
        if (makeFilter.HasValue && !foundMake)
        {
            Console.Error.WriteLine("Requested make was not found in the supported vehicle categories.");
            failures++;
        }
        // Report typos instead of silently finishing with an incomplete list.
        if (makeNames is not null)
        {
            foreach (var missing in makeNames.Where(name => !foundNames.Contains(name)))
            {
                Console.Error.WriteLine($"Make '{missing}' was not found in the discovered categories.");
                failures++;
            }
        }
        Console.WriteLine($"Import finished. Failed requests: {failures}. Rerun the same command to retry them.");
        return failures == 0 ? 0 : 1;
    }

    private async Task ImportModelsAsync(int apiMakeId, string makeName, int year,
        string type, string json, CancellationToken token)
    {
        using var document = JsonDocument.Parse(json);
        var results = document.RootElement.GetProperty("Results");
        if (results.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("API Results must be an array.");

        // Match starter records by name before inserting: preserve their local IDs
        // and any inventory cars that already reference those records.
        var makes = await db.Makes.ToListAsync(token);
        var make = makes.SingleOrDefault(m => m.VpicMakeId == apiMakeId)
            ?? makes.SingleOrDefault(m => m.Name.Equals(makeName, StringComparison.OrdinalIgnoreCase));
        if (make is null)
        {
            make = new Make { Name = makeName, VpicMakeId = apiMakeId };
            db.Makes.Add(make);
        }
        if (make.VpicMakeId.HasValue && make.VpicMakeId != apiMakeId)
            throw new InvalidDataException($"Conflicting API make ID for {makeName}.");
        make.VpicMakeId = apiMakeId;
        await db.SaveChangesAsync(token);

        var models = await db.CarModels.Where(m => m.MakeId == make.Id).ToListAsync(token);
        foreach (var entry in results.EnumerateArray())
        {
            if (entry.GetProperty("Make_ID").GetInt32() != apiMakeId)
                throw new InvalidDataException("API returned a different make than requested.");
            var apiModelId = entry.GetProperty("Model_ID").GetInt32();
            var name = entry.GetProperty("Model_Name").GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidDataException("API returned a model without a name.");
            var model = models.SingleOrDefault(m => m.VpicModelId == apiModelId)
                ?? models.SingleOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (model is null)
            {
                model = new CarModel { MakeId = make.Id, Name = name, VpicModelId = apiModelId };
                db.CarModels.Add(model);
                models.Add(model);
            }
            if (model.VpicModelId.HasValue && model.VpicModelId != apiModelId)
                throw new InvalidDataException($"Conflicting API model ID for {name}.");
            model.VpicModelId = apiModelId;
            await db.SaveChangesAsync(token);

            if (!await db.CarModelYears.AnyAsync(y => y.CarModelId == model.Id && y.Year == year && y.VehicleType == type, token))
                db.CarModelYears.Add(new CarModelYear
                {
                    CarModelId = model.Id, Year = year, VehicleType = type
                });
        }
        // A successful empty list is a completed lookup, not a fabricated model year.
    }

    private async Task<string> FetchAsync(string path, CancellationToken token)
    {
        for (var attempt = 1; ; attempt++)
        {
            // Sequential calls and a delay avoid flooding the public API.
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            try
            {
                using var response = await http.GetAsync(Api + path, token);
                if ((response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500) && attempt < 4)
                {
                    var delay = response.Headers.RetryAfter?.Delta
                        ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow)
                        ?? TimeSpan.FromSeconds(attempt * 5);
                    await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(5), token);
                    continue;
                }
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(token);
                using var check = JsonDocument.Parse(json);
                if (check.RootElement.GetProperty("Results").ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException("API response is missing its Results array.");
                return json;
            }
            catch (HttpRequestException) when (attempt < 4)
            { await Task.Delay(TimeSpan.FromSeconds(attempt * 5), token); }
            catch (OperationCanceledException) when (!token.IsCancellationRequested && attempt < 4)
            { await Task.Delay(TimeSpan.FromSeconds(attempt * 5), token); }
        }
    }

    private async Task CompleteAsync(string path, string json, CancellationToken token)
    {
        var request = await db.CatalogImportRequests.FindAsync([path], token);
        if (request is null)
        {
            request = new CatalogImportRequest { Id = path };
            db.CatalogImportRequests.Add(request);
        }
        request.Attempts++;
        request.LastAttemptAt = DateTime.UtcNow;
        request.CompletedAt = DateTime.UtcNow;
        request.LastError = null;
        // Preserve the source response for troubleshooting without another API call.
        request.ResponseJson = json;
    }

    private async Task RecordFailureAsync(string path, Exception ex, CancellationToken token)
    {
        db.ChangeTracker.Clear();
        var request = await db.CatalogImportRequests.FindAsync([path], token);
        if (request is null)
        {
            request = new CatalogImportRequest { Id = path };
            db.CatalogImportRequests.Add(request);
        }
        request.Attempts++;
        request.LastAttemptAt = DateTime.UtcNow;
        request.LastError = ex.Message;
        request.CompletedAt = null;
        await db.SaveChangesAsync(token);
        Console.Error.WriteLine($"FAILED {path}: {ex.Message}");
    }
}
