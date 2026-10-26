using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Contracts;

public interface INotificationService
{
    Task<Notification> NotifyAsync(int userId, string message);
    Task NotifyManyAsync(IEnumerable<int> userIds, string message);
    Task<IReadOnlyList<Notification>> GetForUserAsync(bool unreadOnly = false, int take = 50);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int userId, long notificationId);
    Task<int> MarkAllAsReadAsync(int userId);
}
