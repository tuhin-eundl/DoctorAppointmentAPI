using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Appointments;
using DoctorAppointment.Application.Profiles.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Security.Claims;

namespace DoctorAppointment.Service.Controllers;

[Route("api/appointments")]
[ApiController]
[Authorize]
public sealed class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet("doctors")]
    [Produces("application/json")]
    [SwaggerOperation("Lists doctors patients can book, with each one's next day that still has room.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the doctors.", typeof(IEnumerable<DoctorSummaryDto>))]
    public async Task<IActionResult> GetDoctors()
    {
        return Ok(await _appointmentService.GetDoctorsAsync());
    }

    [HttpGet("doctors/{doctorId:int}/availability")]
    [Produces("application/json")]
    [SwaggerOperation("Returns a doctor's upcoming schedule days with how many places are booked and left.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the upcoming days.", typeof(IEnumerable<AvailableDayDto>))]
    public async Task<IActionResult> GetAvailability(int doctorId)
    {
        return Ok(await _appointmentService.GetAvailabilityAsync(doctorId));
    }

    [HttpPost]
    [Produces("application/json")]
    [SwaggerOperation("Requests an appointment on a schedule day. It starts as Pending until the doctor approves it.")]
    [SwaggerResponse((int)HttpStatusCode.Created, "The appointment was requested.", typeof(AppointmentDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "The day is no longer available or the request is invalid.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Only patients can book appointments.")]
    [SwaggerResponse((int)HttpStatusCode.Conflict, "The day is full, or the patient already booked this doctor that day.")]
    public async Task<IActionResult> Book([FromBody] BookAppointmentDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var appointment = await _appointmentService.BookAsync(userId, request);
        return StatusCode((int)HttpStatusCode.Created, AppointmentProfile.EntityToDto(appointment));
    }

    [HttpGet]
    [Produces("application/json")]
    [SwaggerOperation("Returns the signed-in user's appointments: a doctor's bookings, or a patient's own.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the appointments, newest date first.", typeof(IEnumerable<AppointmentDto>))]
    public async Task<IActionResult> GetMine()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var appointments = await _appointmentService.GetForUserAsync(userId);
        return Ok(appointments.Select(AppointmentProfile.EntityToDto).ToList());
    }

    [HttpPatch("{appointmentId:int}/status")]
    [Produces("application/json")]
    [SwaggerOperation("Doctors approve or decline requests; doctors and patients can cancel.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "The status was changed.", typeof(AppointmentDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "That status change isn't allowed.")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "The appointment doesn't exist for this user.")]
    public async Task<IActionResult> UpdateStatus(int appointmentId, [FromBody] UpdateAppointmentStatusDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var appointment = await _appointmentService.UpdateStatusAsync(userId, appointmentId, request.Status);
        return Ok(AppointmentProfile.EntityToDto(appointment));
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(userIdClaim, out userId);
    }
}
