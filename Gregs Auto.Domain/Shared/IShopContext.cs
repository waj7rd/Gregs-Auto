using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.Licensing;

namespace Gregs_Auto.Domain.Shared;

// Supplies the shop record that everything else reads its settings from.
//
// Cached rather than fetched per request: the row changes when someone saves
// the settings screen, which is roughly never, and a database round-trip on
// every booking check to re-read the same four values would be wasteful.
// Whoever writes the record calls Reload.
//
// When this becomes multi-tenant, this is the seam — Current stops meaning
// "the shop" and starts meaning "the shop this request belongs to", and the
// cache becomes keyed rather than singular.
public interface IShopContext
{
    Shop Current { get; }

    void Reload();
}

// IShopSettings and IFeatureFlags both read from the shop record, so they're
// thin adapters over it rather than separate sources of truth. Registered
// against the same cached context, which is why a settings change takes effect
// everywhere at once.
public class ShopSettingsFromContext : IShopSettings
{
    private readonly IShopContext _context;

    public ShopSettingsFromContext(IShopContext context) => _context = context;

    public int BayCount => _context.Current.BayCount;
    public TimeOnly OpensAt => _context.Current.OpensAt;
    public TimeOnly ClosesAt => _context.Current.ClosesAt;
    public IReadOnlyCollection<DayOfWeek> ClosedDays => _context.Current.ClosedDays;
}

public class FeatureFlagsFromContext : IFeatureFlags
{
    private readonly IShopContext _context;

    public FeatureFlagsFromContext(IShopContext context) => _context = context;

    public Tier CurrentTier => _context.Current.Tier;

    public bool IsEnabled(Feature feature) => CurrentTier >= feature.MinimumTier();
}

// The clock has to resolve its timezone per call rather than capturing it once.
// A shop that corrects its timezone on the settings screen would otherwise keep
// being judged against the old one until the application restarted — which is
// exactly the class of bug the whole timezone fix existed to remove.
public class ShopClockFromContext : IShopClock
{
    private readonly TimeProvider _timeProvider;
    private readonly IShopContext _context;

    public ShopClockFromContext(TimeProvider timeProvider, IShopContext context)
    {
        _timeProvider = timeProvider;
        _context = context;
    }

    public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById(_context.Current.TimeZoneId);

    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public DateTime LocalNow =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZone), DateTimeKind.Unspecified);
}
