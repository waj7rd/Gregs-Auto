namespace Gregs_Auto.ViewModels;

// Everything the customer detail page needs: the customer's own info plus
// their vehicles and the "add a vehicle" form.
public class CustomerDetailsViewModel
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public List<VehicleRowViewModel> Vehicles { get; set; } = new();

    // Backs the "add a vehicle" form, so a failed submit can redisplay what
    // was typed alongside the per-field validation messages.
    public CreateVehicleViewModel NewVehicle { get; set; } = new();

    // Set when an operation fails for a reason that isn't field-level,
    // e.g. a delete blocked by existing appointment history.
    public string? ErrorMessage { get; set; }
}
