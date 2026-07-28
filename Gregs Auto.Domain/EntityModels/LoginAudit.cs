namespace Gregs_Auto.Domain.EntityModels;

// One row per sign-in attempt. Answers "who logged in, when, and did anyone
// spend the evening guessing at Greg's password?"
public partial class LoginAudit
{
    public int LoginAuditId { get; set; }

    // Null when the attempt was against an email that doesn't exist — those are
    // precisely the rows worth keeping when someone is probing.
    public int? UserId { get; set; }

    // Always recorded, even when no user matched.
    public string EmailAttempted { get; set; } = string.Empty;

    // One of LoginAuditEvent.
    public string Event { get; set; } = string.Empty;

    // Wide enough for IPv6.
    public string? IpAddress { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual User? User { get; set; }
}

// The Event vocabulary. Plain consts rather than an enum so the values written
// to the database read as themselves when someone queries the table by hand.
public static class LoginAuditEvent
{
    public const string Success = "Success";
    public const string Failure = "Failure";
    public const string LockedOut = "LockedOut";
    public const string Inactive = "Inactive";
    public const string Logout = "Logout";
    public const string PasswordChanged = "PasswordChanged";
}
