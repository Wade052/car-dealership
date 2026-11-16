using System.ComponentModel.DataAnnotations;

namespace car_dealership.Models;

public class Make
{
    public int Id { get; set; }

    // NHTSA identity is separate from our local database primary key.
    public int? VpicMakeId { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
    public ICollection<CarModel> Models { get; set; } = new List<CarModel>();
}
