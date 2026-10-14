namespace DoctorAppointment.Application.DTOs.Auth;

public record TokenResponseDto(
    string AccessToken,
    string UserRole,
    int ExpiresInSeconds);
