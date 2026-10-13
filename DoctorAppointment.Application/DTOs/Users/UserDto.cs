using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.DTOs.Users;

public record UserDto(
    int Id,
    string FirstName,
    string? LastName,
    string? Title,
    string Username,
    string? Password,
    string? Email,
    int? Age,
    string? Gender,
    UserRole Role,
    Department? Department);

