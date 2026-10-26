using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Contracts;

public interface INotificationRepository : IBaseRepository<Notification>
{
    Task AddRangeAsync(IEnumerable<Notification> notifications);
    Task<Notification?> GetByIdAsync(long id);
    Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool unreadOnly, int take);
    Task<int> CountUnreadAsync(int userId);
    Task<int> MarkAllAsReadAsync(int userId);
}
