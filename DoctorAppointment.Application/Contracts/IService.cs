namespace DoctorAppointment.Application.Contracts;

public interface IService<T> where T : class
{
    Task<T?> GetAsync(int id);
    Task<IEnumerable<T>> ListAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(T entity);
}
