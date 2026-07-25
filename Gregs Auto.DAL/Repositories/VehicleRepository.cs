using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.DAL.Repositories.Base;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.DAL.Repositories;

public class VehicleRepository : GenericRepository<GregsAutoContext, Vehicle>, IVehicleRepository
{
    public async Task<IList<Vehicle>> GetAllWithCustomerAsync()
    {
        return await Context.Vehicles
            .Include(v => v.Customer)
            .OrderBy(v => v.Customer.FullName)
            .ToListAsync();
    }
}
