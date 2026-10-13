namespace DoctorAppointment.Application.DTOs.Auth;

public record TokenResponseDto(string AccessToken, int ExpiresInSeconds);
