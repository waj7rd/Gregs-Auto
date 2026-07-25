namespace Gregs_Auto.ViewModels;

// One row in a customer's vehicle list.
public class VehicleRowViewModel
{
    public int Id { get; set; }
    public short Year { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Vin { get; set; }
    public string? LicensePlate { get; set; }
}
