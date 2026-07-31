using Gregs_Auto.Domain.EntityModels;

namespace Gregs_Auto.Domain.Shared;

// The shop's own settings.
//
// Note what isn't here: nothing that changes Tier. Tier is what the shop has
// paid for, and this is the one place a shop-facing form reaches the shop
// record — so the operation that saves settings must be incapable of touching
// it, rather than merely choosing not to.
public interface IShopLogic
{
    Task<Shop> GetAsync();

    Task<ShopResult> UpdateSettingsAsync(ShopSettingsUpdate update);
}

// A carrier rather than the entity, so a model binder can never be pointed at
// Shop directly and set Tier along the way.
public class ShopSettingsUpdate
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }

    public string TimeZoneId { get; set; } = "America/Chicago";
    public int BayCount { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
    public IReadOnlyCollection<DayOfWeek> ClosedDays { get; set; } = Array.Empty<DayOfWeek>();
}

public class ShopResult : IOperationResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ShopResult Ok() => new() { Success = true };
    public static ShopResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
