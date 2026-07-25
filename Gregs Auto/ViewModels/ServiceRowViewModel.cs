namespace Gregs_Auto.ViewModels;

// One row in the public Services listing.
public class ServiceRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public decimal Price { get; set; }
}
