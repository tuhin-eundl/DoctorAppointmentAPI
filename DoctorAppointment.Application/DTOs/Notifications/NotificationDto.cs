namespace DoctorAppointment.Application.DTOs.Notifications;

public record NotificationDto(
    long Id,
    string Message,
    bool IsRead,
    DateTime CreatedAt);
