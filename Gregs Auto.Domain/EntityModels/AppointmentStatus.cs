namespace Gregs_Auto.Domain.EntityModels;

// The Appointment.Status vocabulary. Plain consts rather than an enum so the
// values stored in the database read as themselves when someone queries the
// table by hand — same approach as BookingRequestStatus and LoginAuditEvent.
public static class AppointmentStatus
{
    // Booked, not started.
    public const string Scheduled = "Scheduled";

    // Car is in the bay.
    public const string InProgress = "InProgress";

    // Work finished. Terminal.
    public const string Completed = "Completed";

    // Called off. Terminal, and the slot is free again.
    public const string Cancelled = "Cancelled";

    // A slot held by one of these is genuinely occupied; a Cancelled one isn't.
    public static bool OccupiesSlot(string status) => status != Cancelled;

    // Nothing more can happen to an appointment in one of these.
    public static bool IsFinal(string status) => status is Completed or Cancelled;
}
