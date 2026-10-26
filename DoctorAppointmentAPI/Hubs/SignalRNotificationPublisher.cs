using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.Profiles.Notifications;
using DoctorAppointment.Domain;
using Microsoft.AspNetCore.SignalR;

namespace DoctorAppointment.Service.Hubs;

public sealed class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(IHubContext<NotificationHub> hub, ILogger<SignalRNotificationPublisher> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PublishAsync(IReadOnlyCollection<Notification> notifications)
    {
        foreach (var notification in notifications)
        {
            try
            {
                await _hub.Clients
                    .User(notification.UserId.ToString())
                    .SendAsync(NotificationHub.NotificationMethod, NotificationProfile.EntityToDto(notification));
            }
            catch (Exception ex)
            {
                // The notification is already saved, so the request that caused it must not fail here.
                _logger.LogWarning(ex, "Could not push notification {NotificationId} to user {UserId}.", notification.Id, notification.UserId);
            }
        }
    }
}
