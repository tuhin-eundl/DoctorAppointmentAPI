using DoctorAppointment.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Infrastructures;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected DbContext _db;
    protected BaseRepository(DbContext db)
    {
        _db = db;
    }

    private DbSet<T> Table => _db.Set<T>();

    public virtual async Task<T?> GetAsync(object id)
    {
        return await Table.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> ListAsync()
    {
        return await Table.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await Table.AddAsync(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        Table.Update(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(T entity)
    {
        Table.Remove(entity);
        return await _db.SaveChangesAsync() > 0;
    }

    public IQueryable<T> Query()
    {
        return Table;
    }
}

