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
}
