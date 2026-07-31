using Gregs_Auto.Domain.Shared;
using Gregs_Auto.Domain.EntityModels;

namespace Gregs_Auto.Domain.Scheduling;

// Public booking requests, and turning them into real appointments.
public interface IBookingRequestLogic
{
    // Anonymous submission from the public site. Validates the requested time
    // and the service; everything else is taken as typed.
    Task<BookingRequestResult> SubmitAsync(NewBookingRequest request);

    Task<IList<BookingRequest>> GetPendingAsync();

    Task<IList<BookingRequest>> GetRecentlyHandledAsync(int count);

    // Creates (or reuses) the Customer and Vehicle, books the Appointment, and
    // marks the request Accepted. handledByUserId is the staff member acting.
    Task<BookingRequestResult> AcceptAsync(int bookingRequestId, int handledByUserId);

    Task<BookingRequestResult> DeclineAsync(int bookingRequestId, int handledByUserId);
}

// What the public form supplies. A plain carrier — deliberately not the entity,
// so nothing anonymous is ever attached to the context by model binding.
public class NewBookingRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public short VehicleYear { get; set; }
    public string VehicleMake { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? Notes { get; set; }
}

public class BookingRequestResult : IOperationResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int BookingRequestId { get; private set; }

    // Set when accepting produced an appointment.
    public int? AppointmentId { get; private set; }

    public static BookingRequestResult Ok(int bookingRequestId, int? appointmentId = null) =>
        new() { Success = true, BookingRequestId = bookingRequestId, AppointmentId = appointmentId };

    public static BookingRequestResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
