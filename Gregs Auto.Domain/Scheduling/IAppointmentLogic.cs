using Gregs_Auto.Domain.EntityModels;

namespace Gregs_Auto.Domain.Scheduling;

// Business-logic contract for scheduling.
public interface IAppointmentLogic
{
    // Appointments from the start of today onwards, excluding cancelled ones —
    // i.e. the slots that are actually spoken for. Soonest first.
    Task<IList<Appointment>> GetUpcomingAsync();

    // Every appointment regardless of date or status, for the staff management
    // board. Completed and cancelled work has to stay visible there.
    Task<IList<Appointment>> GetScheduleAsync();

    // Book a new appointment. Validates the vehicle and service exist and that
    // the slot isn't already taken before creating anything.
    Task<AppointmentResult> BookAsync(int vehicleId, int serviceId, DateTime scheduledAt, string? notes);

    // The next slot the shop could actually take, for prefilling a date field.
    DateTime NextBookableSlot();

    // Scheduled -> InProgress. No-ops if the appointment isn't Scheduled.
    Task StartAsync(int appointmentId);

    // Scheduled/InProgress -> Completed. No-ops otherwise.
    Task CompleteAsync(int appointmentId);

    // Scheduled/InProgress -> Cancelled. No-ops otherwise.
    Task CancelAsync(int appointmentId);
}
