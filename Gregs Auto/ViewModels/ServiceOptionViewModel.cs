namespace Gregs_Auto.ViewModels;

// One entry in the "pick a service" dropdown on the Schedule Appointment page.
public class ServiceOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int EstimatedDurationMinutes { get; set; }
}
