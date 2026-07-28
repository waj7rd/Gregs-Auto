namespace Gregs_Auto;

// Named rate-limit policies, referenced from [EnableRateLimiting].
public static class RateLimitPolicies
{
    // The anonymous booking request form.
    public const string PublicBooking = "public-booking";

    // The sign-in form.
    public const string Login = "login";
}
