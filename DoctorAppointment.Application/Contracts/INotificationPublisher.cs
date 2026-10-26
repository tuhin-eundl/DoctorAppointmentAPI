using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Contracts;

/// <summary>Delivers saved notifications to their users straight away (over SignalR in the API).</summary>
public interface INotificationPublisher
{
    /// <summary>Best effort: a user who isn't connected still finds the notification on their next fetch.</summary>
    Task PublishAsync(IReadOnlyCollection<Notification> notifications);
}
