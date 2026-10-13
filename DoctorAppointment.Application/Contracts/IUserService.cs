using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Contracts;

public interface IUserService : IService<User>
{
    Task<UserDto?> GetAsync(int id);
    new Task<IReadOnlyList<UserDto?>> ListAsync();
    Task<bool> IsValidUser(string username, string password);
}