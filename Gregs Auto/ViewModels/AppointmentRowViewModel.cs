namespace Gregs_Auto.ViewModels;

// One row in the upcoming-appointments list on the Schedule Appointment page.
public class AppointmentRowViewModel
{
    public int Id { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string VehicleDescription { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
}
