using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Application.Exceptions;
using DoctorAppointment.Application.Profiles.Users;
using DoctorAppointment.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Application.UseCases;

public sealed class UserService : BaseService<User>, IUserService, IService<User>
{
    private readonly IUserRepository _repository;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UserService(IUserRepository repository) : base(repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<UserDto?>> ListAsync()
    {
        var users = await _repository.ListAsync();
        return users
            .Select(UserProfiles.EntityToDto)
            .ToList();
    }

    public override async Task<User> AddAsync(User entity)
    {
        if (await UserExistsAsync(entity.Username, entity.Email, null))
        {
            throw new ConflictException("User with the same username or email already exists.");
        }

        entity.Password = _passwordHasher.HashPassword(
            entity,
            entity.Password);
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = entity.Username;

        return await base.AddAsync(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _repository.GetAsync(id);

        if (user is null)
        {
            throw new NotFoundException(nameof(User), id);
        }

        await _repository.DeleteAsync(user);
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
        if (result == PasswordVerificationResult.Success)
        {
            return UserProfiles.EntityToDto(user);
        }

        return null;
    }

    public async Task<bool> UserExistsAsync(
        string username,
        string? email,
        int? id)
    {
        var query = _repository.Query()
            .Where(u => u.Username == username ||
                        (email != null && u.Email == email));

        if (id.HasValue)
        {
            query = query.Where(u => u.Id != id.Value);
        }

        return await query.AnyAsync();

    }
}