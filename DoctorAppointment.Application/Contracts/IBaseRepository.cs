namespace DoctorAppointment.Application.Contracts;

public interface IBaseRepository<T> where T : class
{
    Task<T?> GetAsync(object id);
    Task<IEnumerable<T>> ListAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(T entity);
    IQueryable<T> Query();
}

