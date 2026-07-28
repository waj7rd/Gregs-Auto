using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories.IBase;

namespace Gregs_Auto.Domain.IRepositories;

public interface IAppointmentRepository : IGenericRepository<Appointment>
{
    // Entity-specific query beyond the generic CRUD: all appointments with their
    // Vehicle (+ owning Customer) and Service eager-loaded.
    Task<IList<Appointment>> GetAllWithDetailsAsync();

    // Appointments starting inside the window, Service included so the caller
    // can work out how long each one runs. Cancelled ones are left out — that
    // time is free again.
    Task<IList<Appointment>> GetActiveBetweenAsync(DateTime fromInclusive, DateTime toExclusive);
}
