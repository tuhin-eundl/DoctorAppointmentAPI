using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.DoctorSchedule;
using DoctorAppointment.Application.Profiles.DoctorSchedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Security.Claims;

namespace DoctorAppointment.Service.Controllers;

[Route("api/doctor-schedules")]
[ApiController]
public sealed class DoctorScheduleController : ControllerBase
{
    private readonly IDoctorScheduleService _doctorScheduleService;

    public DoctorScheduleController(IDoctorScheduleService doctorScheduleService)
    {
        _doctorScheduleService = doctorScheduleService;
    }

    [HttpPost]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Creates a schedule for the authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.Created, "The doctor schedule was created.", typeof(DoctorScheduleDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "The schedule request is invalid.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "The request has no valid authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "The authenticated user is not a doctor.")]
    [SwaggerResponse((int)HttpStatusCode.Conflict, "A schedule already exists for one or more dates in this range.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "An unspecified error occurred.")]
    public async Task<IActionResult> CreateSchedule([FromBody] DoctorScheduleDto request)
    {
        if (!TryGetCurrentUserId(out var doctorId))
        {
            return Unauthorized();
        }

        var schedule = await _doctorScheduleService.CreateScheduleAsync(doctorId, request);
        return CreatedAtAction(
            nameof(GetSchedulesByDoctorId),
            new { doctorId = doctorId },
            DoctorScheduleProfile.EntityToDto(schedule));
    }

    [HttpGet]
    //[Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Returns all doctor schedules to any authenticated user.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns all doctor schedules.", typeof(IEnumerable<DoctorScheduleDto>))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "The request is unauthenticated.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "An unspecified error occurred.")]
    public async Task<IActionResult> GetAllSchedules()
    {
        var schedules = await _doctorScheduleService.GetAllSchedulesAsync();
        return Ok(schedules.Select(DoctorScheduleProfile.EntityToDto).ToList());
    }

    [HttpGet("doctor/{doctorId:int}")]
    //[Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Returns schedules belonging to the authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns schedules for the doctor.", typeof(IEnumerable<DoctorScheduleDto>))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "The request has no valid authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Doctors can only access their own schedules.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "An unspecified error occurred.")]
    public async Task<IActionResult> GetSchedulesByDoctorId(int doctorId)
    {
        var schedules = await _doctorScheduleService
            .GetSchedulesByDoctorIdAsync(doctorId);
        return Ok(schedules.Select(DoctorScheduleProfile.EntityToDto).ToList());
    }

    [HttpGet("{scheduleId:int}")]
    //[Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Returns a schedule.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the requested schedule.", typeof(DoctorScheduleDto))]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "The request has no valid authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "The authenticated user is not a doctor.")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "The schedule does not exist for this doctor.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "An unspecified error occurred.")]
    public async Task<IActionResult> GetScheduleById(int scheduleId)
    {
        var schedule = await _doctorScheduleService.GetScheduleByIdAsync(scheduleId);
        return Ok(DoctorScheduleProfile.EntityToDto(schedule!));
    }

    [HttpPut("{scheduleId:int}")]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Updates a schedule owned by the authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "The schedule was updated.", typeof(DoctorScheduleDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "The schedule request is invalid.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "The request has no valid authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "The authenticated user is not a doctor.")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "The schedule does not exist for this doctor.")]
    [SwaggerResponse((int)HttpStatusCode.Conflict, "The requested date range conflicts with another schedule.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "An unspecified error occurred.")]
    public async Task<IActionResult> UpdateSchedule(int scheduleId, [FromBody] UpdateDoctorScheduleDto request)
    {
        var schedule = await _doctorScheduleService.UpdateScheduleAsync(scheduleId, request);
        return Ok(DoctorScheduleProfile.EntityToDto(schedule));
    }

    [HttpDelete("{scheduleId:int}")]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Deletes a schedule owned by the authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.NoContent, "The schedule was deleted.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "The request has no valid authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "The authenticated user is not a doctor.")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "The schedule does not exist for this doctor.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "An unspecified error occurred.")]
    public async Task<IActionResult> DeleteSchedule(int scheduleId)
    {
        if (!TryGetCurrentUserId(out var doctorId))
        {
            return Unauthorized();
        }

        await _doctorScheduleService.DeleteScheduleAsync(doctorId, scheduleId);
        return NoContent();
    }

    [HttpDelete("{scheduleId:int}/days/{scheduleDayId:int}")]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Deletes one daily timing from a schedule owned by the authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.NoContent, "The schedule day was deleted.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "The request has no valid authenticated doctor.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "The authenticated user is not a doctor.")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "The schedule or schedule day does not exist for this doctor.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "An unspecified error occurred.")]
    public async Task<IActionResult> DeleteScheduleDay(int scheduleId, int scheduleDayId)
    {
        if (!TryGetCurrentUserId(out var doctorId))
        {
            return Unauthorized();
        }

        await _doctorScheduleService.DeleteScheduleDayAsync(doctorId, scheduleId, scheduleDayId);
        return NoContent();
    }

    private bool TryGetCurrentUserId(out int doctorId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(userIdClaim, out doctorId);
    }

}
