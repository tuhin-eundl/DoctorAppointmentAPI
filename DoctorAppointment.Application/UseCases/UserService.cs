using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Application.Exceptions;
using DoctorAppointment.Application.Profiles.Users;
using DoctorAppointment.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Application.UseCases;

public sealed class UserService : BaseService<User>,IUserService, IService<User>
{
    private readonly IUserRepository _repository;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UserService(IUserRepository repository) : base(repository)
    {
        _repository = repository;
    }

    public async Task<UserDto?> GetAsync(int id)
    {
        var user = await base.GetAsync(id);
        if(user == null)
            throw new NotFoundException(nameof(User), id);
        var userDto = UserProfiles.EntityToDto(user);
        return userDto;
    }

    public async Task<IReadOnlyList<UserDto?>> ListAsync()
    {
        var users = await _repository.ListAsync();
        return users
            .Select(UserProfiles.EntityToDto)
            .ToList();
    }

    public override Task<User> AddAsync(User entity)
    {
        entity.Password = _passwordHasher.HashPassword(
            entity,
            entity.Password);
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = entity.Username;

        return base.AddAsync(entity);
    }

    public async Task<UserDto?> IsValidUser(string username, string password)
    {
        var user = await _repository.Query()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.Password,
            password);

        return UserProfiles.EntityToDto(user);
    }
}

