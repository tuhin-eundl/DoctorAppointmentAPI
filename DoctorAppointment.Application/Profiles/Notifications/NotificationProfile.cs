using DoctorAppointment.Application.DTOs.Notifications;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Profiles.Notifications;

public static class NotificationProfile
{
    // CreatedAt is saved as UTC, but SQL Server returns it without a kind; mark it so it serializes with "Z".
    public static NotificationDto EntityToDto(Notification notification) =>
        new(
            notification.Id,
            notification.Message,
            notification.IsRead,
            DateTime.SpecifyKind(notification.CreatedAt, DateTimeKind.Utc));
}
