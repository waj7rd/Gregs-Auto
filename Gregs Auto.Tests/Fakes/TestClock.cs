
namespace Gregs_Auto.Tests.Fakes;

// A clock pinned to a fixed moment, so tests state the time rather than
// depending on when they happen to run.
//
// The default is a Wednesday morning in July — Central Daylight Time, so the
// shop is UTC-5 and the two clocks are visibly different. A test that
// accidentally compares against UTC will fail rather than pass by luck.
public class TestClock : IShopClock
{
    public static readonly DateTime DefaultLocal = new(2026, 7, 15, 9, 0, 0);

    public TestClock(DateTime? shopLocalNow = null)
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        LocalNow = shopLocalNow ?? DefaultLocal;
    }

    public DateTime LocalNow { get; set; }

    public DateTime UtcNow => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(LocalNow, DateTimeKind.Unspecified), TimeZone);

    public TimeZoneInfo TimeZone { get; }

    // Moves the shop clock forward, for testing things that expire.
    public void Advance(TimeSpan by) => LocalNow = LocalNow.Add(by);
}

public class TestShopSettings : IShopSettings
{
    // Wide open by default — 24/7, no closed days — so tests about overlap and
    // capacity aren't quietly also testing opening hours. Tests that care about
    // hours pass their own values.
    public TestShopSettings(
        int bayCount = 3,
        TimeOnly? opensAt = null,
        TimeOnly? closesAt = null,
        IReadOnlyCollection<DayOfWeek>? closedDays = null)
    {
        BayCount = bayCount;
        OpensAt = opensAt ?? new TimeOnly(0, 0);
        ClosesAt = closesAt ?? new TimeOnly(23, 59);
        ClosedDays = closedDays ?? Array.Empty<DayOfWeek>();
    }

    public int BayCount { get; }
    public TimeOnly OpensAt { get; }
    public TimeOnly ClosesAt { get; }
    public IReadOnlyCollection<DayOfWeek> ClosedDays { get; }
}
