using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using car_dealership.Models.enums;

namespace car_dealership.Models;

// Only the listing fields supported by the Edit form are accepted.
public sealed class EditCarColorsInput
{
    [BindNever]
    public int CarId { get; set; }

    [BindNever]
    public string VehicleName { get; set; } = "";

    [StringLength(80)]
    public string? Trim { get; set; }

    public DealershipLocation? Location { get; set; }

    [StringLength(80)]
    public string? ExteriorColor { get; set; }

    [StringLength(80)]
    public string? InteriorColor { get; set; }

    [Required]
    public Status? Status { get; set; }
}
