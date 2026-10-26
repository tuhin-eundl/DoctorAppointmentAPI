namespace DoctorAppointment.Application.DTOs.Users;

/// <summary>The signed-in user's profile. UserType is "Doctor" or "Patient".</summary>
public record CurrentUserDto(
    int Id,
    string FirstName,
    string? LastName,
    string? Title,
    string Username,
    string? Email,
    int? Age,
    string? Gender,
    string UserType,
    string? Department);
