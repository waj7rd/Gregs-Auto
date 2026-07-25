using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.Domain.IRepositories.IBase;

namespace Gregs_Auto.DAL.Repositories.Base;

// The single implementation of IGenericRepository, shared by every entity repo.
// C is the context type; T is the entity type.
public abstract class GenericRepository<C, T> : IGenericRepository<T>
    where T : class
    where C : GregsAutoContext, new()
{
    private C _entities = new();
    public C Context
    {
        get { return _entities; }
        set { _entities = value; }
    }

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
