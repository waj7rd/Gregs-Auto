using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.Shared;

namespace Gregs_Auto.DAL.Context;

// Loads and caches the shop record.
//
// Registered as a singleton, so it takes a scope factory rather than a context
// directly — a singleton holding a scoped DbContext is the classic captive
// dependency, and it would hand every request the same stale, eventually
// disposed context.
public class ShopContextProvider : IShopContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _gate = new();
    private Shop? _cached;

    public ShopContextProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Shop Current
    {
        get
        {
            // Double-checked so the common path is a field read, not a lock.
            if (_cached != null)
                return _cached;

            lock (_gate)
            {
                _cached ??= Load();
                return _cached;
            }
        }
    }

    public void Reload()
    {
        lock (_gate)
        {
            _cached = null;
        }
    }

    private Shop Load()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GregsAutoContext>();

        // AsNoTracking: this instance outlives the context it came from, and
        // nothing should be able to save through it by accident.
        var shop = db.Shops.AsNoTracking().OrderBy(s => s.ShopId).FirstOrDefault();

        if (shop == null)
        {
            throw new InvalidOperationException(
                "No row in Shops. Run Gregs Auto.DAL/Scripts/AddShop.sql — " +
                "the application reads its hours, bay count and tier from there.");
        }

        return shop;
    }
}
