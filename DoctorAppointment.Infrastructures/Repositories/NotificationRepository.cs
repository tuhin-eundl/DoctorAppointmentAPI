using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Domain;
using DoctorAppointment.Infrastructures.Database;
using Microsoft.EntityFrameworkCore;

namespace DoctorAppointment.Infrastructures.Repositories;

public sealed class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications)
    {
        await _db.Notifications.AddRangeAsync(notifications);
        await _db.SaveChangesAsync();
    }

    public Task<Notification?> GetByIdAsync(long id)
    {
        return _db.Notifications.SingleOrDefaultAsync(n => n.Id == id);
    }

    public async Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool unreadOnly, int take)
    {
        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(take)
            .ToListAsync();
    }

    public Task<int> CountUnreadAsync(int userId)
    {
        return _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public Task<int> MarkAllAsReadAsync(int userId)
    {
        return _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
    }
}
