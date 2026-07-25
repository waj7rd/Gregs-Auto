using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories.IBase;

namespace Gregs_Auto.Domain.IRepositories;

public interface IVehicleRepository : IGenericRepository<Vehicle>
{
    // Entity-specific query beyond the generic CRUD: all vehicles with their
    // owning Customer eager-loaded, for the "pick a vehicle" dropdown.
    Task<IList<Vehicle>> GetAllWithCustomerAsync();
}
