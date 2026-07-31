namespace Gregs_Auto.Domain.Licensing;

// What a shop has bought.
//
// Deliberately an ordered chain rather than a set of independent switches:
// each tier contains everything below it. Three tiers means three valid
// configurations, not eight — which is the difference between testing this
// and hoping.
//
// The numbers are explicit because the comparison is the whole mechanism, and
// because these values end up in configuration and later in a database column.
public enum Tier
{
    Scheduling = 1,
    Invoicing = 2,
    Inspections = 3,
}

// Individual capabilities. Each declares the lowest tier that includes it —
// the feature owns that fact, rather than a central per-tier list that someone
// will eventually forget to update.
public enum Feature
{
    OnlineBooking,
    ServiceCatalog,
    StaffAccounts,

    Estimates,
    Invoicing,
    Payments,

    DigitalInspections,
    InspectionApprovals,
}

public static class FeatureTiers
{
    // A switch expression rather than a dictionary on purpose: add a Feature
    // without placing it and the compiler complains, instead of it silently
    // defaulting to available-everywhere.
    public static Tier MinimumTier(this Feature feature) => feature switch
    {
        Feature.OnlineBooking       => Tier.Scheduling,
        Feature.ServiceCatalog      => Tier.Scheduling,
        Feature.StaffAccounts       => Tier.Scheduling,

        Feature.Estimates           => Tier.Invoicing,
        Feature.Invoicing           => Tier.Invoicing,
        Feature.Payments            => Tier.Invoicing,

        Feature.DigitalInspections  => Tier.Inspections,
        Feature.InspectionApprovals => Tier.Inspections,

        _ => throw new ArgumentOutOfRangeException(nameof(feature), feature,
                 "Feature has no tier. Every feature must declare one."),
    };
}
