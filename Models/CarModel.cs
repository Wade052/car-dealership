using System.ComponentModel.DataAnnotations;

namespace car_dealership.Models;

public class CarModel
{
    public int Id { get; set; }

    // NHTSA identity is separate from our local database primary key.
    public int? VpicModelId { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
    public int MakeId { get; set; }
    public Make Make { get; set; } = null!;
}
