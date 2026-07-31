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

    // The next moment the shop could actually take a booking, used to prefill
    // the date field rather than leaving it empty.
    //
    // Deliberately not "now": now is in the past by the time the form posts,
    // and it may be 3am on a Sunday. A default the rules would immediately
    // reject is worse than no default — the customer fixes an error they didn't
    // cause.
    //
    // Rounds up to the next half hour, because nobody books at 10:37.
    public static DateTime NextOpenSlot(IShopSettings settings, DateTime from)
    {
        var slot = RoundUpToHalfHour(from);

        // Two weeks is far more than enough to find an open day, and stops a
        // shop configured as closed every day spinning forever.
        for (var attempt = 0; attempt < 14; attempt++)
        {
            var day = slot.Date;

            if (!settings.ClosedDays.Contains(day.DayOfWeek))
            {
                var opens = day + settings.OpensAt.ToTimeSpan();
                var closes = day + settings.ClosesAt.ToTimeSpan();

                if (slot < opens)
                    return opens;

                if (slot < closes)
                    return slot;
            }

            // Too late today, or closed today — try tomorrow from opening.
            slot = day.AddDays(1) + settings.OpensAt.ToTimeSpan();
        }

        return slot;
    }

    private static DateTime RoundUpToHalfHour(DateTime value)
    {
        var half = TimeSpan.FromMinutes(30);
        var ticks = (value.Ticks + half.Ticks - 1) / half.Ticks * half.Ticks;
        return new DateTime(ticks, value.Kind);
    }
}
