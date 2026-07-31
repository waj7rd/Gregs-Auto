namespace Gregs_Auto.Domain.Shared;

// Facts about how this particular shop runs, supplied from configuration.
// Behind an interface so the Domain doesn't take a dependency on
// IConfiguration, and so tests can state the shop's shape directly.
public interface IShopSettings
{
    // How many jobs can genuinely be in progress at once. This is the number
    // that stops the whole day being booked into one hour.
    int BayCount { get; }

    // When the doors open and shut, shop-local.
    TimeOnly OpensAt { get; }
    TimeOnly ClosesAt { get; }

    // Days the shop isn't open at all.
    IReadOnlyCollection<DayOfWeek> ClosedDays { get; }
}

public class ShopSettings : IShopSettings
{
    public ShopSettings(
        int bayCount,
        TimeOnly? opensAt = null,
        TimeOnly? closesAt = null,
        IReadOnlyCollection<DayOfWeek>? closedDays = null)
    {
        BayCount = bayCount;
        OpensAt = opensAt ?? new TimeOnly(8, 0);
        ClosesAt = closesAt ?? new TimeOnly(17, 0);
        ClosedDays = closedDays ?? new[] { DayOfWeek.Sunday };
    }

    public int BayCount { get; }
    public TimeOnly OpensAt { get; }
    public TimeOnly ClosesAt { get; }
    public IReadOnlyCollection<DayOfWeek> ClosedDays { get; }
}

// Whether a proposed job fits inside the shop's opening hours.
public static class ShopHours
{
    // Returns why the slot doesn't work, or null if it does.
    //
    // The whole job has to fit: a 90-minute brake job starting at 4:30pm on a
    // day the shop shuts at 5 doesn't fit, even though 4:30 is inside hours.
    public static string? Check(IShopSettings settings, DateTime start, int durationMinutes)
    {
        if (settings.ClosedDays.Contains(start.DayOfWeek))
            return $"The shop is closed on {start.DayOfWeek}s.";

        var startTime = TimeOnly.FromDateTime(start);
        if (startTime < settings.OpensAt || startTime >= settings.ClosesAt)
            return $"The shop is open {Describe(settings.OpensAt)} to {Describe(settings.ClosesAt)}.";

        var end = start.AddMinutes(durationMinutes);

        // Running past midnight is out for the same reason as running past close.
        if (end.Date != start.Date || TimeOnly.FromDateTime(end) > settings.ClosesAt)
            return $"That job takes {durationMinutes} minutes and wouldn't be finished by closing at {Describe(settings.ClosesAt)}.";

        return null;
    }

    private static string Describe(TimeOnly time) => time.ToString("h:mm tt").ToLowerInvariant();
}
