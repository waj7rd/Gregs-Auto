using Gregs_Auto.Domain.Implementations.Interfaces;

namespace Gregs_Auto.Domain.Implementations;

// Shop-local clock backed by TimeProvider, so tests can hand it a fixed "now"
// instead of depending on when they happen to run.
public class ShopClock : IShopClock
{
    private readonly TimeProvider _timeProvider;

    public ShopClock(TimeProvider timeProvider, TimeZoneInfo timeZone)
    {
        _timeProvider = timeProvider;
        TimeZone = timeZone;
    }

    public TimeZoneInfo TimeZone { get; }

    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    // Kind is Unspecified deliberately: it has to match the values the
    // datetime-local form field posts and the values stored in ScheduledAt, so
    // that comparing the two compares like with like.
    public DateTime LocalNow =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZone), DateTimeKind.Unspecified);
}
