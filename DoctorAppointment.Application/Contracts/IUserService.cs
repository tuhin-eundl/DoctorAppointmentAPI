using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Contracts;

public interface IUserService : IService<User>
{
    new Task<IReadOnlyList<UserDto?>> ListAsync();
    Task<UserDto?> IsValidUser(string username, string password);
    Task<bool> UserExistsAsync(string username, string? email, int? id);
    Task<User?> GetUserAsync(int id);
}