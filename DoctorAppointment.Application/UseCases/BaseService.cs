using DoctorAppointment.Application.Contracts;

namespace DoctorAppointment.Application.UseCases;

public abstract class BaseService<T> : IService<T> where T : class
{
    protected IBaseRepository<T> _repository;

    public BaseService(IBaseRepository<T> repository)
    {
        _repository = repository;
    }

    public virtual async Task<T?> GetAsync(int id)
    {
        return await _repository.GetAsync(id);
    }

    public virtual async Task<IEnumerable<T>> ListAsync()
    {
        return await _repository.ListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        return await _repository.AddAsync(entity);
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        return await _repository.UpdateAsync(entity);
    }

    public virtual async Task<bool> DeleteAsync(T entity)
    {
        return await _repository.DeleteAsync(entity);
    }
}

