using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.Implementations.Interfaces;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.Domain.Implementations;

// Business logic for scheduling. Lives in the Domain; depends only on
// repository interfaces (DI supplies the real ones at runtime).
public class AppointmentLogic : IAppointmentLogic
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IServiceRepository _serviceRepository;

    public AppointmentLogic(IAppointmentRepository appointmentRepository, IVehicleRepository vehicleRepository, IServiceRepository serviceRepository)
    {
        _appointmentRepository = appointmentRepository;
        _vehicleRepository = vehicleRepository;
        _serviceRepository = serviceRepository;
    }

    public async Task<IList<Appointment>> GetUpcomingAsync()
    {
        return await _appointmentRepository.GetAllWithDetailsAsync();
    }

    public async Task<AppointmentResult> BookAsync(int vehicleId, int serviceId, DateTime scheduledAt, string? notes)
    {
        if (scheduledAt <= DateTime.Now)
            return AppointmentResult.Fail("Appointment time must be in the future.");

        var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == vehicleId);
        if (vehicle == null)
            return AppointmentResult.Fail("Vehicle not found.");

        var service = await _serviceRepository.GetAsync(s => s.ServiceId == serviceId);
        if (service == null)
            return AppointmentResult.Fail("Service not found.");

        // Guard against double-booking the same vehicle for the same slot.
        var conflicts = await _appointmentRepository.FindByAsync(a =>
            a.VehicleId == vehicleId &&
            a.ScheduledAt == scheduledAt &&
            a.Status != "Cancelled");
        if (conflicts.Count > 0)
            return AppointmentResult.Fail("This vehicle already has an appointment at that time.");

        var appointment = new Appointment
        {
            VehicleId = vehicleId,
            ServiceId = serviceId,
            ScheduledAt = scheduledAt,
            Status = "Scheduled",
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        await _appointmentRepository.AddAsync(appointment);
        await _appointmentRepository.SaveChangesAsync();

        return AppointmentResult.Ok(appointment.AppointmentId);
    }
}
