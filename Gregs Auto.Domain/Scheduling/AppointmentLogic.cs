using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.Shared;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.Domain.Scheduling;

// Business logic for scheduling. Lives in the Domain; depends only on
// repository interfaces (DI supplies the real ones at runtime).
public class AppointmentLogic : IAppointmentLogic
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IShopClock _clock;

    private readonly IShopSettings _settings;

    // How many jobs the shop can genuinely run at once.
    private readonly int _bayCount;

    public AppointmentLogic(IAppointmentRepository appointmentRepository, IVehicleRepository vehicleRepository, IServiceRepository serviceRepository, IShopClock clock, IShopSettings settings)
    {
        _appointmentRepository = appointmentRepository;
        _vehicleRepository = vehicleRepository;
        _serviceRepository = serviceRepository;
        _clock = clock;
        _settings = settings;
        _bayCount = Math.Max(1, settings.BayCount);
    }

    public async Task<IList<Appointment>> GetUpcomingAsync()
    {
        var all = await _appointmentRepository.GetAllWithDetailsAsync();

        // From the start of today, not from this instant — someone booking at
        // 4pm still needs to see that this morning's slots were taken. Cancelled
        // appointments are excluded because that time is free again.
        var startOfToday = _clock.LocalNow.Date;

        return all
            .Where(a => a.ScheduledAt >= startOfToday && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.ScheduledAt)
            .ToList();
    }

    public async Task<IList<Appointment>> GetScheduleAsync()
    {
        return await _appointmentRepository.GetAllWithDetailsAsync();
    }

    public DateTime NextBookableSlot() => ShopHours.NextOpenSlot(_settings, _clock.LocalNow);

    public async Task<AppointmentResult> BookAsync(int vehicleId, int serviceId, DateTime scheduledAt, string? notes)
    {
        // Compare against wall-clock time at the shop, not the server's clock —
        // scheduledAt arrives from a datetime-local field as shop wall time.
        if (scheduledAt <= _clock.LocalNow)
            return AppointmentResult.Fail("Appointment time must be in the future.");

        var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == vehicleId);
        if (vehicle == null)
            return AppointmentResult.Fail("Vehicle not found.");

        // Same reasoning as archived services: the id still resolves, so the
        // check has to live here rather than only in the dropdown.
        if (!vehicle.IsActive)
            return AppointmentResult.Fail("That vehicle has been archived.");

        var service = await _serviceRepository.GetAsync(s => s.ServiceId == serviceId);
        if (service == null)
            return AppointmentResult.Fail("Service not found.");

        // Checked here rather than only in the view: an archived service still
        // has an id, and a stale form or a hand-made POST would otherwise book it.
        if (!service.IsActive)
            return AppointmentResult.Fail("That service isn't offered any more.");

        var outsideHours = ShopHours.Check(_settings, scheduledAt, service.EstimatedDurationMinutes);
        if (outsideHours != null)
            return AppointmentResult.Fail(outsideHours);

        var conflict = await FindConflictAsync(vehicleId, scheduledAt, service.EstimatedDurationMinutes);
        if (conflict != null)
            return AppointmentResult.Fail(conflict);

        var appointment = new Appointment
        {
            VehicleId = vehicleId,
            ServiceId = serviceId,
            ScheduledAt = scheduledAt,
            Status = AppointmentStatus.Scheduled,
            Notes = notes,

            // Copied, not referenced. Editing the service afterwards must not
            // change what this job cost or how long it was booked for.
            Price = service.Price,
            DurationMinutes = service.EstimatedDurationMinutes,

            CreatedAt = _clock.UtcNow
        };

        await _appointmentRepository.AddAsync(appointment);
        await _appointmentRepository.SaveChangesAsync();

        return AppointmentResult.Ok(appointment.AppointmentId);
    }

    // Returns a message describing why this slot won't work, or null if it will.
    //
    // Two separate rules:
    //   - the same vehicle can't be in two places at once
    //   - the shop can't run more jobs at once than it has bays
    //
    // Both are overlap questions, not equality questions. A 90-minute brake job
    // at 9:00 and another at 9:15 collide, even though the start times differ —
    // which the old exact-match check happily allowed.
    private async Task<string?> FindConflictAsync(int vehicleId, DateTime start, int durationMinutes)
    {
        var end = start.AddMinutes(durationMinutes);

        // Widen the window by the longest job the shop offers, so an appointment
        // that starts before this one but runs into it is still considered.
        var longestService = await _serviceRepository.GetAllAsync();
        var maxDuration = longestService.Count == 0 ? 0 : longestService.Max(s => s.EstimatedDurationMinutes);

        var candidates = await _appointmentRepository.GetActiveBetweenAsync(
            start.AddMinutes(-maxDuration), end);

        // Each existing appointment's own duration, not whatever its service
        // says today. Otherwise editing a service silently reshuffles a
        // schedule that has already been agreed with people.
        var overlapping = candidates
            .Where(a => Overlaps(a.ScheduledAt, a.DurationMinutes, start, end))
            .ToList();

        if (overlapping.Any(a => a.VehicleId == vehicleId))
            return "That vehicle is already booked in over that time.";

        if (overlapping.Count >= _bayCount)
        {
            return _bayCount == 1
                ? "The shop is already booked at that time. Please pick another slot."
                : $"All {_bayCount} bays are taken over that time. Please pick another slot.";
        }

        return null;
    }

    // Half-open intervals: a job ending exactly when another starts is fine.
    private static bool Overlaps(DateTime existingStart, int existingMinutes, DateTime start, DateTime end)
    {
        var existingEnd = existingStart.AddMinutes(existingMinutes);
        return existingStart < end && existingEnd > start;
    }

    public async Task StartAsync(int appointmentId)
    {
        var appointment = await _appointmentRepository.GetAsync(a => a.AppointmentId == appointmentId);

        // Business rule: only a Scheduled appointment can move to InProgress.
        if (appointment == null || appointment.Status != AppointmentStatus.Scheduled)
            return;

        appointment.Status = AppointmentStatus.InProgress;
        await _appointmentRepository.SaveChangesAsync();
    }

    public async Task CompleteAsync(int appointmentId)
    {
        var appointment = await _appointmentRepository.GetAsync(a => a.AppointmentId == appointmentId);

        // Business rule: a Completed or Cancelled appointment can't be completed again.
        if (appointment == null || AppointmentStatus.IsFinal(appointment.Status))
            return;

        appointment.Status = AppointmentStatus.Completed;
        await _appointmentRepository.SaveChangesAsync();
    }

    public async Task CancelAsync(int appointmentId)
    {
        var appointment = await _appointmentRepository.GetAsync(a => a.AppointmentId == appointmentId);

        // Business rule: a Completed or already-Cancelled appointment can't be cancelled.
        if (appointment == null || AppointmentStatus.IsFinal(appointment.Status))
            return;

        appointment.Status = AppointmentStatus.Cancelled;
        await _appointmentRepository.SaveChangesAsync();
    }
}
