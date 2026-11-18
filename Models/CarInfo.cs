using System.ComponentModel.DataAnnotations;
using car_dealership.Models.enums;

namespace car_dealership.Models
{

    public class Car
    {
        // Basic Vehicle Information
        public int Id { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Select an approved vehicle model.")]
        public int CarModelId { get; set; }
        public CarModel? CarModel { get; set; }
        public int Year { get; set; }
        [StringLength(80)]
        public string? Trim { get; set; }
        public decimal Price { get; set; }
        public int Mileage { get; set; }
        [Required]
        [RegularExpression(@"[A-HJ-NPR-Z0-9]{17}$")]
        public string VIN { get; set; } = "";

        // Vehicle Appearance
        public string ExteriorColor { get; set; } = "";
        public string InteriorColor { get; set; } = "";

        // Vehicle Specifications
        public CarType Type { get; set; }
        public FuelType Fuel { get; set; }
        public TransmissionType Transmission { get; set; }
        public DriveTrain Drivetrain { get; set; }

        // Mechanical Information
        public string Engine { get; set; } = "";
        public int? Horsepower { get; set; }
        public int? Doors { get; set; }
        public int? Seats { get; set; }

        // Vehicle History
        public Condition Condition { get; set; }
        public VehicleTitle Title { get; set; }

        // Dealership Information

        public DealershipLocation? Location { get; set; }

        public string LocationName => Location switch
        {
            DealershipLocation.Altoona => "Altoona, PA",
            DealershipLocation.StateCollege => "State College, PA",
            _ => "Location not assigned"
        };
        
        public Status Status { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Warranty
        public bool HasWarranty { get; set; }
        public DateTime? WarrantyExpiration { get; set; }

        // Vehicle Listing
        public string Description { get; set; } = "";
        public string? ImageUrl { get; set; } // Legacy single-photo listings.
        public ICollection<CarPhoto> Photos { get; set; } = new List<CarPhoto>();
    }
}