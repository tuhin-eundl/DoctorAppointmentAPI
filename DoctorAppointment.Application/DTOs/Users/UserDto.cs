using DoctorAppointment.Application.DTOs.Hospital;

namespace DoctorAppointment.Application.DTOs.Users;

public record CreateUserDto(
    int Id,
    string FirstName,
    string? LastName,
    string? Title,
    string Username,
    string Password,
    string? Email,
    int? Age,
    string? Gender,
    string Role,
    string? Department,
    List<int?> HospitalIds);

public record UserDto(
    int Id,
    string FirstName,
    string? LastName,
    string? Title,
    string Username,
    string Password,
    string? Email,
    int? Age,
    string? Gender,
    string Role,
    string? Department,
    List<HospitalDto?> Hospitals);

