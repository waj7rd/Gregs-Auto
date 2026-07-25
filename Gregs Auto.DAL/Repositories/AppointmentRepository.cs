using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.DAL.Repositories.Base;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.DAL.Repositories;

public class AppointmentRepository : GenericRepository<GregsAutoContext, Appointment>, IAppointmentRepository
{
    // Entity-specific query. EF Core's Include lives HERE, in the DAL — never
    // in the Domain. Uses Context (the DbContext) from the generic base class.
    public async Task<IList<Appointment>> GetAllWithDetailsAsync()
    {
        return await Context.Appointments
            .Include(a => a.Vehicle).ThenInclude(v => v.Customer)
            .Include(a => a.Service)
            .OrderBy(a => a.ScheduledAt)
            .ToListAsync();
    }
}
