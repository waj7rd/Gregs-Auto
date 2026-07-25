namespace Gregs_Auto.ViewModels;

// Everything the Schedule Appointment page needs to render: the picker lists
// plus the current upcoming schedule.
public class ScheduleAppointmentViewModel
{
    public List<VehicleOptionViewModel> Vehicles { get; set; } = new();
    public List<ServiceOptionViewModel> Services { get; set; } = new();
    public List<AppointmentRowViewModel> UpcomingAppointments { get; set; } = new();

    // Set when a booking attempt fails, e.g. slot already taken.
    public string? ErrorMessage { get; set; }
}
