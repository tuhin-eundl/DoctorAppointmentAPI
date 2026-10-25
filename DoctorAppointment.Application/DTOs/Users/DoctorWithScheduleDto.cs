using DoctorAppointment.Application.DTOs.DoctorSchedule;

namespace DoctorAppointment.Application.DTOs.Users;

public record DoctorWithScheduleDto(
    int Id,
    string FirstName,
    string? LastName,
    string? Title,
    string? Department,
    string? Email,
    int? Age,
    string? Gender,
    IReadOnlyList<DoctorScheduleDto> Schedules);
