using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DoctorAppointment.Service.Hubs;

/// <summary>
/// Pushes each new notification to its user as a "notification" message carrying a NotificationDto.
/// Clients only listen; SignalR routes messages by the JWT's user id (the NameIdentifier claim).
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public const string Path = "/hubs/notifications";
    public const string NotificationMethod = "notification";
}
