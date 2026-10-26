using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Notifications;
using DoctorAppointment.Application.Profiles.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Security.Claims;

namespace DoctorAppointment.Service.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize]
public sealed class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("Returns the signed-in user's notifications, newest first.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the notifications.", typeof(IEnumerable<NotificationDto>))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int take = 50)
    {
        var notifications = await _notificationService.GetForUserAsync(unreadOnly, take);

        return Ok(notifications.Select(NotificationProfile.EntityToDto).ToList());
    }

    [HttpGet("unread-count")]
    [Produces("application/json")]
    [SwaggerOperation("Returns how many of the signed-in user's notifications are unread.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the unread count.", typeof(UnreadCountDto))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(new UnreadCountDto(await _notificationService.GetUnreadCountAsync(userId)));
    }

    [HttpPatch("read-all")]
    [SwaggerOperation("Marks all of the signed-in user's notifications as read.")]
    [SwaggerResponse((int)HttpStatusCode.NoContent, "The notifications were marked as read.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        await _notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }

    [HttpPatch("{notificationId:long}/read")]
    [SwaggerOperation("Marks one of the signed-in user's notifications as read.")]
    [SwaggerResponse((int)HttpStatusCode.NoContent, "The notification was marked as read.")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "The notification doesn't exist for this user.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> MarkAsRead(long notificationId)
    {
        await _notificationService.MarkAsReadAsync(notificationId);
        return NoContent();
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(userIdClaim, out userId);
    }
}
