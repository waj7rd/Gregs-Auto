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

    // Set when an "add vehicle" attempt fails, e.g. duplicate VIN.
    public string? ErrorMessage { get; set; }
}
