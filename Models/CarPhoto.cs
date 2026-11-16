namespace car_dealership.Models;

public sealed class CarPhoto
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
    public string Url { get; set; } = "";

    // Position zero is the cover photo in inventory.
    public int DisplayOrder { get; set; }
}
