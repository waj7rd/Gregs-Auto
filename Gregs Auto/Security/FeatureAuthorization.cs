using Microsoft.AspNetCore.Authorization;
using Gregs_Auto.Domain.Licensing;

namespace Gregs_Auto.Security;

// Feature gating expressed as an authorization requirement, so it composes with
// the role policies already in place — one [Authorize] on a controller can
// require both a role and a tier.
//
// This lives in the web project rather than the Domain: the Domain shouldn't
// take a dependency on ASP.NET just to answer a licensing question.
public class FeatureRequirement : IAuthorizationRequirement
{
    public FeatureRequirement(Feature feature)
    {
        Feature = feature;
    }

    public Feature Feature { get; }
}

public class FeatureAuthorizationHandler : AuthorizationHandler<FeatureRequirement>
{
    private readonly IFeatureFlags _features;

    public FeatureAuthorizationHandler(IFeatureFlags features)
    {
        _features = features;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, FeatureRequirement requirement)
    {
        if (_features.IsEnabled(requirement.Feature))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

// Policy names for tier-gated areas. Kept beside the role policies in
// Domain.Security.Policies conceptually, but separate because these combine a
// role check with a licensing check.
public static class FeaturePolicies
{
    public const string Invoicing = "feature:invoicing";
    public const string Inspections = "feature:inspections";
}
