namespace Gregs_Auto.Domain.Implementations.Interfaces;

// The outcome of a booking attempt. A customer needs to know WHY a booking
// failed (bad vehicle/service, slot already taken, etc.) so the UI can show it.
public class AppointmentResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int AppointmentId { get; private set; }

    public static AppointmentResult Ok(int appointmentId) => new() { Success = true, AppointmentId = appointmentId };
    public static AppointmentResult Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
