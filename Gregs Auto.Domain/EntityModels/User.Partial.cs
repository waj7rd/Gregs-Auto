namespace Gregs_Auto.Domain.EntityModels;

// Hand-written half of User. Lives outside User.cs so that regenerating the
// scaffolded entities doesn't wipe these out.
public partial class User
{
    // Deactivated rather than deleted — an account that has touched appointments
    // still has to resolve in the audit trail.
    public bool IsActive { get; set; } = true;

    // Consecutive failed sign-ins. Reset to zero on a successful login.
    public int FailedLoginCount { get; set; }

    // Set when FailedLoginCount trips the threshold. UTC. Null means not locked.
    public DateTime? LockedOutUntil { get; set; }

    // UTC. Null until the account has been used at least once.
    public DateTime? LastLoginAt { get; set; }
}
