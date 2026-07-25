using Gregs_Auto.DAL.Context;
using Gregs_Auto.DAL.Repositories.Base;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.DAL.Repositories;

public class ServiceRepository : GenericRepository<GregsAutoContext, Service>, IServiceRepository
{
}
