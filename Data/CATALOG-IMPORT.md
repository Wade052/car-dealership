# Local vehicle catalog import

Run from the dealership project directory. Stop the development web server first
to avoid output-file locks. This command uses the project's configured SQLite database.
Back up the database before a new bulk import. Do not run two importers simultaneously.

## Small first run (Ford, one year)

```powershell
dotnet run -- --import-catalog --from 2026 --to 2026 --make-id 460
```

## Full supported range

```powershell
dotnet run -- --import-catalog --from 1996 --to 2027
```

The default is 1996 through next calendar year. This is a finite range, not a claim
that the API has published all vehicles for the last year. NHTSA documents the
model-by-year endpoint for years greater than 1995; older years need another source.

The importer discovers Passenger Car, Truck, and Multipurpose Passenger Vehicle
(MPV) makes, then requests models for each make/year/type. This includes broad truck
categories, not only pickups. It cannot reliably distinguish every SUV/van/trim
using these catalog responses. These are catalog identities, not engine, trim,
color, ownership history, mileage or vehicle condition. VIN decoding is separate.

## Resume and failures

Press Ctrl+C to stop. Rerun exactly the same command to resume. Completed endpoints
are skipped. Each make/year/type batch commits atomically with its checkpoint.
CatalogImportRequests records failed paths, error messages, timestamps, and original
successful JSON. Failed requests remain incomplete and are retried next run.
Exit code 0 means all requested lookups completed; 1 means failures remain; 2 means
the run was cancelled. An empty successful response does not create imaginary models.

Calls are sequential and paced, with bounded retries and Retry-After handling.
A full import involves thousands of requests and can take hours. Website requests
never call the external API. New years can be imported later with --from and --to.
Completed queries are snapshots; reruns skip them rather than automatically refreshing.

## Database design

Make.VpicMakeId and CarModel.VpicModelId store external IDs separately from Id.
Existing starter entries match by case-insensitive name and retain their local IDs.
Ambiguous/conflicting IDs fail visibly instead of silently combining records.
CarModelYear stores the model, supported year and NHTSA vehicle type. A unique index
prevents duplicate combinations. Existing cars and starter records are not deleted.

The current Create dropdowns still use the existing local catalog. Year-dependent
dropdowns and server validation of model/year combinations are a separate form change.
Unimported starter records have no confirmed model years and must not be treated as
evidence that a model existed in a particular year.

Source: https://vpic.nhtsa.dot.gov/api/
