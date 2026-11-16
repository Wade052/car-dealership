namespace car_dealership.Models;

// An API-confirmed model/year/type combination, not an individual inventory car.
public class CarModelYear
{
    public int Id { get; set; }
    public int CarModelId { get; set; }
    public CarModel CarModel { get; set; } = null!;

    public int Year { get; set; }
    // Keep NHTSA's broad type: an SUV or van may be classified as an MPV or truck.
    public string VehicleType { get; set; } = "";
}
