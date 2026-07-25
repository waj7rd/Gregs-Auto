namespace Gregs_Auto.ViewModels;

// One entry in the "pick a vehicle" dropdown on the Schedule Appointment page.
public class VehicleOptionViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // e.g. "2019 Ford F-150"
}
