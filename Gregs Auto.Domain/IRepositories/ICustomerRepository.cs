using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories.IBase;

namespace Gregs_Auto.Domain.IRepositories;

public interface ICustomerRepository : IGenericRepository<Customer>
{
    // All customers with their Vehicles eager-loaded, for the admin list
    // (vehicle count per customer).
    Task<IList<Customer>> GetAllWithVehiclesAsync();

    // A single customer with their Vehicles eager-loaded, for the detail page.
    Task<Customer?> GetByIdWithVehiclesAsync(int customerId);
}
