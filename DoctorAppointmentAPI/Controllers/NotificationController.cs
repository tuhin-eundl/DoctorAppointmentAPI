using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

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

        return Ok(notifications
            .Select(n => new NotificationDto(n.Id, n.Message, n.IsRead, n.CreatedAt))
            .ToList());
    }

}
