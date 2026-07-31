namespace Gregs_Auto.Domain.Shared;

// Supplies the current time to business logic. Two distinct notions, and the
// difference matters:
//
//   LocalNow — wall-clock time at the shop. An appointment time is a wall-clock
//              time at a physical address ("10:00 AM at the shop"), so anything
//              compared against ScheduledAt has to use this.
//   UtcNow   — absolute time, for audit stamps like CreatedAt.
//
// Behind an interface so the logic layer never reads DateTime.Now, which
// silently means "whatever timezone the server happens to be set to" and breaks
// as soon as the app is hosted somewhere other than a machine running on the
// shop's local time.
public interface IShopClock
{
    DateTime LocalNow { get; }

    DateTime UtcNow { get; }

    TimeZoneInfo TimeZone { get; }
}
