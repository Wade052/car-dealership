using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using car_dealership.Models;

namespace car_dealership.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Car> Cars { get; set; }
    public DbSet<CarPhoto> CarPhotos { get; set; }
    public DbSet<CarPurchase> CarPurchases { get; set; }
    public DbSet<Make> Makes { get; set; }
    public DbSet<CarModel> CarModels { get; set; }
    public DbSet<CarModelYear> CarModelYears { get; set;}

    public DbSet<CatalogImportRequest> CatalogImportRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CarPurchase>().HasIndex(purchase => purchase.CarId).IsUnique();
        builder.Entity<CarPurchase>().HasOne(purchase => purchase.Car).WithMany()
            .HasForeignKey(purchase => purchase.CarId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CarPurchase>().HasOne(purchase => purchase.Buyer).WithMany()
            .HasForeignKey(purchase => purchase.BuyerId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<CarPhoto>().HasIndex(photo => new { photo.CarId, photo.DisplayOrder }).IsUnique();

        // Nullable external IDs allow existing starter entries to be matched later.
        builder.Entity<Make>().HasIndex(m => m.VpicMakeId).IsUnique();
        builder.Entity<CarModel>().HasIndex(m => new { m.MakeId, m.VpicModelId }).IsUnique();
        // Preserve the existing lowercase SQLite column while using C# naming conventions.
        builder.Entity<CarModelYear>().Property(y => y.Year).HasColumnName("year");

        builder.Entity<Make>().HasIndex(m => m.Name).IsUnique();
        builder.Entity<CarModel>().HasIndex(m => new { m.MakeId, m.Name }).IsUnique();
        builder.Entity<CarModel>()
            .HasOne(m => m.Make).WithMany(m => m.Models)
            .HasForeignKey(m => m.MakeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Car>()
            .HasOne(c => c.CarModel).WithMany()
            .HasForeignKey(c => c.CarModelId).OnDelete(DeleteBehavior.Restrict);

        // Starter catalog: add other approved makes and models as needed.
        builder.Entity<Make>().HasData(
            new Make { Id = 1, Name = "Ford" },
            new Make { Id = 2, Name = "Toyota" },
            new Make { Id = 3, Name = "Honda" });
        builder.Entity<CarModel>().HasData(
            new CarModel { Id = 1, MakeId = 1, Name = "Mustang" },
            new CarModel { Id = 2, MakeId = 1, Name = "F-150" },
            new CarModel { Id = 3, MakeId = 2, Name = "Camry" },
            new CarModel { Id = 4, MakeId = 2, Name = "Corolla" },
            new CarModel { Id = 5, MakeId = 3, Name = "Civic" },
            new CarModel { Id = 6, MakeId = 3, Name = "Accord" });

        builder.Entity<CarModelYear>()
            .HasIndex(entry => new { entry.CarModelId, entry.Year, entry.VehicleType})
            .IsUnique();
    }
}
