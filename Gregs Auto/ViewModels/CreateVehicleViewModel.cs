using System.ComponentModel.DataAnnotations;
using Gregs_Auto.ViewModels.Validation;

namespace Gregs_Auto.ViewModels;

// The "add a vehicle" form on the customer detail page. Same rules as
// EditVehicleViewModel, minus the VehicleId that doesn't exist yet.
public class CreateVehicleViewModel
{
    public int CustomerId { get; set; }

    [Required]
    [VehicleYear]
    public short Year { get; set; }

    [Required]
    [StringLength(50)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [RegularExpression(ValidationPatterns.Vin, ErrorMessage = ValidationPatterns.VinMessage)]
    [Display(Name = "VIN")]
    public string? Vin { get; set; }

    [StringLength(20)]
    [Display(Name = "License plate")]
    public string? LicensePlate { get; set; }
}
