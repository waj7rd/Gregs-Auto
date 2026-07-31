namespace Gregs_Auto.Domain.Licensing;

// Whether a capability is available to the shop currently being served.
//
// Behind an interface so the backing store can change without touching a single
// call site: configuration today, a column on the shop record once more than one
// shop shares a deployment. Same trick as IShopClock.
public interface IFeatureFlags
{
    Tier CurrentTier { get; }

    bool IsEnabled(Feature feature);
}

// Tier-driven implementation. The entire mechanism is one comparison — that is
// the payoff for keeping tiers as a chain rather than a set.
public class TierFeatureFlags : IFeatureFlags
{
    public TierFeatureFlags(Tier currentTier)
    {
        CurrentTier = currentTier;
    }

    public Tier CurrentTier { get; }

    public bool IsEnabled(Feature feature) => CurrentTier >= feature.MinimumTier();
}
