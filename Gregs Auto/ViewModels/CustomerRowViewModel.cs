namespace Gregs_Auto.ViewModels;

// One row in the customer admin list.
public class CustomerRowViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    // Active vehicles only — archived cars aren't part of what the shop still
    // looks after.
    public int VehicleCount { get; set; }

    // Archived customers stay on the list, greyed out, rather than vanishing.
    public bool IsActive { get; set; } = true;
}

public class CustomerListViewModel
{
    public List<CustomerRowViewModel> Customers { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
