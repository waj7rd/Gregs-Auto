using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.Domain.IRepositories.IBase;

namespace Gregs_Auto.DAL.Repositories.Base;

// The single implementation of IGenericRepository, shared by every entity repo.
// C is the context type; T is the entity type.
//
// The context is injected, not constructed here. It used to be `new C()`, which
// gave every repository its own context on its own connection — so a request
// touching customers, vehicles and appointments opened three of them, an entity
// loaded by one repository was invisible to the others, and no transaction could
// ever span two repositories. That last one made the Unit of Work silently
// useless: it opened a transaction on a context nobody else was writing through.
//
// With one scoped context per request, SaveChangesAsync on any repository writes
// through the same connection, and IUnitOfWork can wrap the lot.
public abstract class GenericRepository<C, T> : IGenericRepository<T>
    where T : class
    where C : GregsAutoContext
{
    private readonly C _entities;

    protected GenericRepository(C context)
    {
        _entities = context;
    }

    public C Context => _entities;

    #region ASYNCHRONOUS
    public virtual async Task<IList<T>> GetAllAsync()
    {
        return await _entities.Set<T>().ToListAsync();
    }
    public virtual async Task AddAsync(T entity)
    {
        await _entities.Set<T>().AddAsync(entity);
    }
    public virtual async Task SaveChangesAsync()
    {
        await _entities.SaveChangesAsync();
    }
    public virtual async Task<IList<T>> FindByAsync(Expression<Func<T, bool>> predicate)
    {
        return await _entities.Set<T>().Where(predicate).ToListAsync();
    }
    public virtual async Task<T?> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return await _entities.Set<T>().FirstOrDefaultAsync(predicate);
    }
    #endregion

    #region SYNCHRONOUS
    public virtual IQueryable<T> GetAll()
    {
        return _entities.Set<T>();
    }
    public IQueryable<T> FindBy(Expression<Func<T, bool>> predicate)
    {
        return _entities.Set<T>().Where(predicate);
    }
    public virtual void Add(T entity)
    {
        _entities.Set<T>().Add(entity);
    }
    public virtual void Delete(T entity)
    {
        _entities.Set<T>().Remove(entity);
    }
    public virtual void Edit(T entity)
    {
        _entities.Entry(entity).State = EntityState.Modified;
    }
    public virtual void Save()
    {
        _entities.SaveChanges();
    }
    #endregion
}
