namespace Gregs_Auto.Domain.EntityModels;

// A booking asked for by someone on the public site, who has no account and no
// way to prove which vehicle is theirs.
//
// Everything from CustomerName through Notes is untrusted free text — a claim
// about who they are, not a record of it. Accepting the request is what turns
// it into real Customer / Vehicle / Appointment rows.
public partial class BookingRequest
{
    public int BookingRequestId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }

    public short VehicleYear { get; set; }
    public string VehicleMake { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;

    public int ServiceId { get; set; }

    // Shop wall-clock time, same frame as Appointment.ScheduledAt.
    public DateTime RequestedAt { get; set; }

    public string? Notes { get; set; }

    // One of BookingRequestStatus.
    public string Status { get; set; } = BookingRequestStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public int? HandledByUserId { get; set; }
    public DateTime? HandledAt { get; set; }

    // Set when the request was accepted and became a real appointment.
    public int? AppointmentId { get; set; }

    public virtual Service Service { get; set; } = null!;
    public virtual User? HandledByUser { get; set; }
    public virtual Appointment? Appointment { get; set; }

    public string VehicleDescription => $"{VehicleYear} {VehicleMake} {VehicleModel}";
}

public static class BookingRequestStatus
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string Declined = "Declined";
}
