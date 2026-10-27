using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Application.Exceptions;
using DoctorAppointment.Application.Profiles.DoctorSchedules;
using DoctorAppointment.Application.Profiles.Users;
using DoctorAppointment.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointment.Application.UseCases;

public sealed class UserService : BaseService<User>, IUserService
{
    private readonly IUserRepository _repository;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UserService(IUserRepository repository) : base(repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<UserDto?>> ListAsync()
    {
        var query = _repository.Query()
            .AsNoTracking()
            .Include(u => u.Hospitals);

        var users = await query
            .ToListAsync();

        return users
            .Select(UserProfiles.EntityToDto)
            .ToList();
    }

    public override async Task<User> AddAsync(User entity)
    {
        await EnsureUserIsUniqueAsync(entity.Username, entity.Email, null);

        entity.Password = _passwordHasher.HashPassword(
            entity,
            entity.Password);
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = entity.Username;

        return await base.AddAsync(entity);
    }

    public override async Task<User> UpdateAsync(User entity)
    {
        await EnsureUserIsUniqueAsync(entity.Username, entity.Email, entity.Id);

        return await base.UpdateAsync(entity);
    }

    private async Task EnsureUserIsUniqueAsync(string username, string? email, int? id)
    {
        if (await UserExistsAsync(username, email, id))
        {
            throw new ConflictException("User with the same username or email already exists.");
        }
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
                        (!string.IsNullOrEmpty(email) && u.Email == email));

        if (id.HasValue)
        {
            query = query.Where(u => u.Id != id.Value);
        }

        return await query.AnyAsync();

    }

    public async Task<User?> GetUserAsync(int id)
    {
        var query = _repository.Query().Include(u => u.Hospitals);
        return await query.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IReadOnlyList<DoctorWithScheduleDto>> GetDoctorsWithScheduleAsync(string? department)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var query = _repository.Query()
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Doctor &&
                        u.DoctorSchedules.Any(s => !s.IsDeleted &&
                                                   s.ToDate >= today &&
                                                   s.ScheduleDays.Any(d => !d.IsDeleted && d.ScheduleDate >= today)));

        if (!string.IsNullOrWhiteSpace(department))
        {
            if (!Enum.TryParse<Department>(department, ignoreCase: true, out var parsedDepartment))
            {
                throw new ValidationException($"Department '{department}' is invalid.");
            }

            query = query.Where(u => u.Department == parsedDepartment);
        }

        var doctors = await query
            .Include(u => u.DoctorSchedules.Where(s => !s.IsDeleted && s.ToDate >= today))
                .ThenInclude(s => s.ScheduleDays.Where(d => !d.IsDeleted && d.ScheduleDate >= today))
            .ToListAsync();

        return doctors
            .Select(doctor => new DoctorWithScheduleDto(
                doctor.Id,
                doctor.FirstName,
                doctor.LastName,
                doctor.Title,
                doctor.Department?.ToString(),
                doctor.Email,
                doctor.Age,
                doctor.Gender,
                doctor.DoctorSchedules
                    .Where(s => s.ScheduleDays.Count > 0)
                    .OrderBy(s => s.FromDate)
                    .Select(s =>
                    {
                        s.ScheduleDays = s.ScheduleDays.OrderBy(d => d.ScheduleDate).ToList();
                        return DoctorScheduleProfile.EntityToDto(s);
                    })
                    .ToList()))
            .ToList();
    }
}