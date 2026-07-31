using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.DAL.Repositories.Base;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.DAL.Repositories;

public class ShopRepository : GenericRepository<GregsAutoContext, Shop>, IShopRepository
{
    public async Task<Shop?> GetCurrentAsync()
    {
        return await Context.Shops.OrderBy(s => s.ShopId).FirstOrDefaultAsync();
    }
}
