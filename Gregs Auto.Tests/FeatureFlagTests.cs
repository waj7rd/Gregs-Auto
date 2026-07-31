using Gregs_Auto.Domain.Licensing;

namespace Gregs_Auto.Tests;

public class FeatureFlagTests
{
    [Fact]
    public void The_base_tier_gets_the_scheduling_features()
    {
        var flags = new TierFeatureFlags(Tier.Scheduling);

        Assert.True(flags.IsEnabled(Feature.OnlineBooking));
        Assert.True(flags.IsEnabled(Feature.ServiceCatalog));
        Assert.True(flags.IsEnabled(Feature.StaffAccounts));
    }

    [Fact]
    public void The_base_tier_does_not_get_what_it_hasnt_bought()
    {
        var flags = new TierFeatureFlags(Tier.Scheduling);

        Assert.False(flags.IsEnabled(Feature.Invoicing));
        Assert.False(flags.IsEnabled(Feature.DigitalInspections));
    }

    [Fact]
    public void Tiers_are_cumulative()
    {
        // The whole point of an ordered chain: a higher tier includes
        // everything below it, so there's no such thing as buying inspections
        // and losing the ability to book a job.
        var flags = new TierFeatureFlags(Tier.Inspections);

        foreach (Feature feature in Enum.GetValues<Feature>())
            Assert.True(flags.IsEnabled(feature), $"{feature} should be enabled at the top tier");
    }

    [Fact]
    public void The_middle_tier_reaches_down_but_not_up()
    {
        var flags = new TierFeatureFlags(Tier.Invoicing);

        Assert.True(flags.IsEnabled(Feature.OnlineBooking));
        Assert.True(flags.IsEnabled(Feature.Invoicing));
        Assert.False(flags.IsEnabled(Feature.DigitalInspections));
    }

    [Fact]
    public void Every_feature_declares_a_tier()
    {
        // Guards the switch expression in FeatureTiers: adding a Feature and
        // forgetting to place it should fail here rather than silently
        // defaulting to available-to-everyone.
        foreach (Feature feature in Enum.GetValues<Feature>())
        {
            var tier = feature.MinimumTier();
            Assert.True(Enum.IsDefined(tier), $"{feature} maps to an undefined tier");
        }
    }

    [Theory]
    [InlineData(Tier.Scheduling, 3)]
    [InlineData(Tier.Invoicing, 6)]
    [InlineData(Tier.Inspections, 8)]
    public void Each_tier_unlocks_a_known_number_of_features(Tier tier, int expected)
    {
        // A blunt canary: if someone adds a feature without thinking about
        // which tier sells it, this fails and makes them decide.
        var flags = new TierFeatureFlags(tier);
        var enabled = Enum.GetValues<Feature>().Count(flags.IsEnabled);

        Assert.Equal(expected, enabled);
    }
}
