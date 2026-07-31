using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories.IBase;

namespace Gregs_Auto.Domain.IRepositories;

public interface IShopRepository : IGenericRepository<Shop>
{
    // The shop this deployment serves. Tracked, unlike the cached copy in
    // IShopContext — this one is meant to be saved through.
    Task<Shop?> GetCurrentAsync();
}
