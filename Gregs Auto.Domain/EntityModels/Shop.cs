using Gregs_Auto.Domain.Licensing;
using Gregs_Auto.Domain.Shared;

namespace Gregs_Auto.Domain.EntityModels;

// The shop this deployment serves. One row today; the tenant row later.
//
// Note the split in what these columns mean. Everything down to ClosedDays is
// the shop's own business and belongs on their settings screen. Tier is what
// they've paid for and must never be bound from a form they can post to —
// otherwise a settings page becomes a free upgrade button.
public partial class Shop : IShopSettings
{
    public int ShopId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }

    public string TimeZoneId { get; set; } = "America/Chicago";

    public int BayCount { get; set; } = 3;

    public TimeOnly OpensAt { get; set; } = new(8, 0);

    public TimeOnly ClosesAt { get; set; } = new(17, 0);

    // Stored comma-separated; exposed as the set the booking rules want.
    public string ClosedDaysRaw { get; set; } = "Sunday";

    // Not shop-editable. See the note above.
    public string TierName { get; set; } = nameof(Licensing.Tier.Scheduling);

    public DateTime CreatedAt { get; set; }

    // ---- IShopSettings ----

    public IReadOnlyCollection<DayOfWeek> ClosedDays => ParseDays(ClosedDaysRaw);

    public Tier Tier =>
        Enum.TryParse<Tier>(TierName, out var tier) ? tier : Licensing.Tier.Scheduling;

    public static IReadOnlyCollection<DayOfWeek> ParseDays(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<DayOfWeek>();

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => Enum.TryParse<DayOfWeek>(d, ignoreCase: true, out var day) ? day : (DayOfWeek?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .Distinct()
            .ToArray();
    }

    public static string FormatDays(IEnumerable<DayOfWeek> days) => string.Join(",", days.Distinct());
}
