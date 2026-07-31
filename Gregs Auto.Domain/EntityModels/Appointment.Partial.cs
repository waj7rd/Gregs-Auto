namespace Gregs_Auto.Domain.EntityModels;

// What was actually sold, captured when the appointment was booked.
//
// These duplicate what's on the Service, deliberately. Reading through the
// ServiceId means the catalogue can rewrite history: put the oil change up a
// dollar and every completed oil change retroactively cost a dollar more; change
// a job from 90 minutes to 120 and the overlap rules re-evaluate bookings made
// months ago.
//
// The ServiceId stays — it's still the link to the catalogue, and it's how the
// name is resolved. Only the numbers are copied.
public partial class Appointment
{
    // What the customer was quoted, at the moment of booking.
    public decimal Price { get; set; }

    // How long the job was expected to take when it was booked. This is what
    // the overlap and bay-capacity rules use, so that editing a service never
    // silently reshuffles a schedule that's already been agreed with people.
    public int DurationMinutes { get; set; }

    public DateTime EndsAt => ScheduledAt.AddMinutes(DurationMinutes);
}
