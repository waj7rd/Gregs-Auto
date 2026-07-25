using Gregs_Auto.Domain.EntityModels;

namespace Gregs_Auto.Domain.Implementations.Interfaces;

// Business-logic contract for scheduling.
public interface IAppointmentLogic
{
    // All appointments with Vehicle/Customer/Service eager-loaded, soonest first.
    Task<IList<Appointment>> GetUpcomingAsync();

    // Book a new appointment. Validates the vehicle and service exist and that
    // the slot isn't already taken before creating anything.
    Task<AppointmentResult> BookAsync(int vehicleId, int serviceId, DateTime scheduledAt, string? notes);

    // Scheduled -> InProgress. No-ops if the appointment isn't Scheduled.
    Task StartAsync(int appointmentId);

    // Scheduled/InProgress -> Completed. No-ops otherwise.
    Task CompleteAsync(int appointmentId);

    // Scheduled/InProgress -> Cancelled. No-ops otherwise.
    Task CancelAsync(int appointmentId);
}
