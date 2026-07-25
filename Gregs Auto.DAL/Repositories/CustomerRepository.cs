using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.DAL.Repositories.Base;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.DAL.Repositories;

public class CustomerRepository : GenericRepository<GregsAutoContext, Customer>, ICustomerRepository
{
    public async Task<IList<Customer>> GetAllWithVehiclesAsync()
    {
        return await Context.Customers
            .Include(c => c.Vehicles)
            .OrderBy(c => c.FullName)
            .ToListAsync();
    }

    public async Task<Customer?> GetByIdWithVehiclesAsync(int customerId)
    {
        return await Context.Customers
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }
}
