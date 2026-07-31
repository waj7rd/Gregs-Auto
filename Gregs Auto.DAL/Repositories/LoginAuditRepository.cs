using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.DAL.Repositories.Base;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.DAL.Repositories;

public class LoginAuditRepository : GenericRepository<GregsAutoContext, LoginAudit>, ILoginAuditRepository
{
    public LoginAuditRepository(GregsAutoContext context) : base(context) { }

    public async Task<IList<LoginAudit>> GetRecentAsync(int count)
    {
        return await Context.LoginAudits
            .Include(l => l.User)
            .OrderByDescending(l => l.OccurredAt)
            .Take(count)
            .ToListAsync();
    }
}
