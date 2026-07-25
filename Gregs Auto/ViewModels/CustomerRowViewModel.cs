namespace Gregs_Auto.ViewModels;

// One row in the customer admin list.
public class CustomerRowViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int VehicleCount { get; set; }
}
