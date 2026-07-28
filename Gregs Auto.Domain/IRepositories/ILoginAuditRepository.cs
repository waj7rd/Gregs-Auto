using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories.IBase;

namespace Gregs_Auto.Domain.IRepositories;

public interface ILoginAuditRepository : IGenericRepository<LoginAudit>
{
    // Most recent attempts first, with the user joined where there was one.
    Task<IList<LoginAudit>> GetRecentAsync(int count);
}
