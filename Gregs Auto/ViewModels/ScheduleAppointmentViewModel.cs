namespace Gregs_Auto.ViewModels;

// Everything the Schedule Appointment page needs to render: the picker lists
// plus the current upcoming schedule.
public class ScheduleAppointmentViewModel
{
    public List<VehicleOptionViewModel> Vehicles { get; set; } = new();
    public List<ServiceOptionViewModel> Services { get; set; } = new();
    public List<AppointmentRowViewModel> UpcomingAppointments { get; set; } = new();

    // True only for signed-in staff. This page is reachable anonymously, so
    // customer names and vehicle details are left out of the model entirely
    // rather than merely hidden in the view — otherwise they'd still be sitting
    // in the page source for anyone who looks.
    //
    // It also decides which form renders: staff book directly against a known
    // vehicle, while a visitor submits a request describing their own car.
    public bool ShowCustomerDetail { get; set; }

    // The public form. Only populated (and only rendered) when anonymous.
    public GuestBookingViewModel Guest { get; set; } = new();

    // The next slot the shop could take. Prefills the date field on both forms,
    // and becomes the min attribute so a browser refuses anything earlier.
    public DateTime DefaultSlot { get; set; }

    // Set when a booking attempt fails, e.g. slot already taken.
    public string? ErrorMessage { get; set; }
}
