using System.ComponentModel.DataAnnotations;
using car_dealership.Models.enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace car_dealership.Models;

// Only fields from the Add a Car form can be bound to this input model.
public sealed class CreateCarInput : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a make.")]
    public int MakeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a model.")]
    public int CarModelId { get; set; }

    [Range(1, 9999, ErrorMessage = "Select a model year.")]
    public int Year { get; set; }

    [StringLength(80)]
    public string? Trim { get; set; }

    public DealershipLocation? Location { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal? Price { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int? Mileage { get; set; }

    [Required, RegularExpression("[A-HJ-NPR-Za-hj-npr-z0-9]{17}",
        ErrorMessage = "Enter a 17-character VIN without the letters I, O, or Q.")]
    public string VIN { get; set; } = "";

    [StringLength(80)]
    public string? ExteriorColor { get; set; }

    [StringLength(80)]
    public string? InteriorColor { get; set; }

    // Blank options prompt the manager to choose rather than silently using the first enum value.
    [Required]
    public CarType? Type { get; set; }

    [Required]
    public FuelType? Fuel { get; set; }

    [Required]
    public TransmissionType? Transmission { get; set; }

    [Required]
    public DriveTrain? Drivetrain { get; set; }

    [StringLength(100)]
    public string? Engine { get; set; }

    [Range(1, int.MaxValue)]
    public int? Horsepower { get; set; }

    [Range(1, int.MaxValue)]
    public int? Doors { get; set; }

    [Range(1, int.MaxValue)]
    public int? Seats { get; set; }

    [Required]
    public Condition? Condition { get; set; }

    [Required]
    public VehicleTitle? Title { get; set; }

    [Required]
    public Status? Status { get; set; } = enums.Status.Available;

    public bool HasWarranty { get; set; }

    public DateTime? WarrantyExpiration { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public const int MaxPhotos = 5;
    public const int MaxPhotoBytes = 5 * 1024 * 1024;

    public List<IFormFile> Photos { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!HasWarranty && WarrantyExpiration.HasValue)
            yield return new ValidationResult("Select Has warranty before entering an expiration date.",
                [nameof(WarrantyExpiration)]);
    }

    [BindNever]
    public IEnumerable<SelectListItem> Makes { get; set; } = [];
}
