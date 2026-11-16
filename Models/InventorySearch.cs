using car_dealership.Models.enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace car_dealership.Models;

public sealed class InventorySearch
{
    public CarType? Type { get; set; }
    public int? MakeId { get; set; }
    public int? Year { get; set; }
    public DealershipLocation? Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    [BindNever]
    public List<Car> Cars { get; set; } = [];

    [BindNever]
    public List<SelectListItem> Makes { get; set; } = [];

    [BindNever]
    public List<SelectListItem> Years { get; set; } = [];

    [BindNever]
    public double SliderMaxPrice { get; set; }
}
