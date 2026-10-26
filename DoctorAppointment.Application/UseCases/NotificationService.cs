using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.Exceptions;
using DoctorAppointment.Domain;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointment.Application.UseCases;

public sealed class NotificationService : INotificationService
{
    private const int MaxTake = 200;

    private readonly INotificationRepository _repository;
    private readonly IExecutionContextAccessor _contextAccessor;

    public NotificationService(
        INotificationRepository repository,
        IExecutionContextAccessor contextAccessor)
    {
        _repository = repository;
        _contextAccessor = contextAccessor;
    }

    public async Task<Notification> NotifyAsync(int userId, string message)
    {
        ValidateMessage(message);

        return await _repository.AddAsync(new Notification
        {
            UserId = userId,
            Message = message.Trim(),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyManyAsync(IEnumerable<int> userIds, string message)
    {
        ValidateMessage(message);

        var createdAt = DateTime.UtcNow;
        var notifications = userIds
            .Distinct()
            .Select(userId => new Notification
            {
                UserId = userId,
                Message = message.Trim(),
                CreatedAt = createdAt
            })
            .ToList();

        if (notifications.Count == 0)
        {
            return;
        }

        await _repository.AddRangeAsync(notifications);
    }

    public Task<IReadOnlyList<Notification>> GetForUserAsync(bool unreadOnly = false, int take = 50)
    {
        if(int.TryParse(_contextAccessor.UserId, out var userId))
        {
            return _repository.GetForUserAsync(userId, unreadOnly, Math.Clamp(take, 1, MaxTake));
        }
        throw new UnauthorizedAccessException("User is not authenticated.");
    }

    public Task<int> GetUnreadCountAsync(int userId)
    {
        return _repository.CountUnreadAsync(userId);
    }

    public async Task MarkAsReadAsync(long notificationId)
    {
        if (!int.TryParse(_contextAccessor.UserId, out var userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var notification = await _repository.GetByIdAsync(notificationId);
        if (notification is null || notification.UserId != userId)
        {
            throw new NotFoundException(nameof(Notification), notificationId);
        }

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        await _repository.UpdateAsync(notification);
    }

    public Task<int> MarkAllAsReadAsync(int userId)
    {
        return _repository.MarkAllAsReadAsync(userId);
    }

    private static void ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ValidationException("Notification message cannot be empty.");
        }
    }
}
