using System.ComponentModel.DataAnnotations;

namespace car_dealership.Models.enums;

public enum DealershipLocation
{
    [Display(Name = "Altoona, PA")]
    Altoona = 1,

    [Display(Name = "State College, PA")]
    StateCollege = 2
}
