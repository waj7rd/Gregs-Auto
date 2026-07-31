using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.Domain.Shared;

public class ShopLogic : IShopLogic
{
    private const int MinBays = 1;
    private const int MaxBays = 50;

    private readonly IShopRepository _shopRepository;
    private readonly IShopContext _shopContext;

    public ShopLogic(IShopRepository shopRepository, IShopContext shopContext)
    {
        _shopRepository = shopRepository;
        _shopContext = shopContext;
    }

    public async Task<Shop> GetAsync()
    {
        var shop = await _shopRepository.GetCurrentAsync();

        if (shop == null)
            throw new InvalidOperationException("No row in Shops. Run AddShop.sql.");

        return shop;
    }

    public async Task<ShopResult> UpdateSettingsAsync(ShopSettingsUpdate update)
    {
        var validation = Validate(update);
        if (validation != null)
            return ShopResult.Fail(validation);

        var shop = await _shopRepository.GetCurrentAsync();
        if (shop == null)
            return ShopResult.Fail("No shop record to update.");

        shop.Name = update.Name.Trim();
        shop.Phone = Blank(update.Phone);
        shop.AddressLine = Blank(update.AddressLine);
        shop.City = Blank(update.City);
        shop.State = Blank(update.State);
        shop.PostalCode = Blank(update.PostalCode);

        shop.TimeZoneId = update.TimeZoneId;
        shop.BayCount = update.BayCount;
        shop.OpensAt = update.OpensAt;
        shop.ClosesAt = update.ClosesAt;
        shop.ClosedDaysRaw = Shop.FormatDays(update.ClosedDays);

        // TierName is deliberately not assigned. See IShopLogic.

        await _shopRepository.SaveChangesAsync();

        // Everything reads settings through the cached context, so it has to be
        // told. Without this the shop saves new hours and the booking rules
        // keep using the old ones until the app restarts.
        _shopContext.Reload();

        return ShopResult.Ok();
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Validate(ShopSettingsUpdate update)
    {
        if (string.IsNullOrWhiteSpace(update.Name))
            return "The shop needs a name.";

        if (update.BayCount < MinBays || update.BayCount > MaxBays)
            return $"Bays has to be between {MinBays} and {MaxBays}.";

        if (update.OpensAt >= update.ClosesAt)
            return "Closing time has to be after opening time.";

        // A shop closed every day can never be booked, which is a state nobody
        // means to save.
        if (update.ClosedDays.Distinct().Count() >= 7)
            return "You can't be closed every day of the week.";

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(update.TimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return "That isn't a timezone this server recognises.";
        }

        return null;
    }
}
