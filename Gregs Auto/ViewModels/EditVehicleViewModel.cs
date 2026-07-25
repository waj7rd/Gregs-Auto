using System.ComponentModel.DataAnnotations;

namespace Gregs_Auto.ViewModels;

public class EditVehicleViewModel
{
    public int VehicleId { get; set; }
    public int CustomerId { get; set; }

    [Required]
    [Range(1900, 2100)]
    public short Year { get; set; }

    [Required]
    [StringLength(50)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [StringLength(17)]
    [Display(Name = "VIN")]
    public string? Vin { get; set; }

    [StringLength(20)]
    [Display(Name = "License plate")]
    public string? LicensePlate { get; set; }
}
