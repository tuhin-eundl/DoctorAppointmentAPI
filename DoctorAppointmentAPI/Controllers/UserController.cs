using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Application.Profiles.Users;
using DoctorAppointment.Application.Validations.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Security.Claims;

namespace DoctorAppointment.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IHospitalService _hospitalService;

    public UserController(
        IUserService userService,
        IHospitalService hospitalService)
    {
        _userService = userService;
        _hospitalService = hospitalService;
    }

    [HttpGet]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Returns a list of all users.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the list of users.", typeof(IEnumerable<UserDto>))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Returned if no users exist.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Returned if the user doesn't have the correct permissions to execute the action.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.ListAsync();
        return Ok(users);
    }

    [HttpGet("me")]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Returns the signed-in user with their type (Doctor or Patient).")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the signed-in user.", typeof(CurrentUserDto))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Returned if the signed-in user no longer exists.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new CurrentUserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Title,
            user.Username,
            user.Email,
            user.Age,
            user.Gender,
            user.Role.ToString(),
            user.Department?.ToString()));
    }

    [HttpGet("doctors-with-schedule")]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Returns doctors that have upcoming schedules, including their schedule days.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the doctors with their upcoming schedules.", typeof(IEnumerable<DoctorWithScheduleDto>))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Returned if the department is invalid.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> GetDoctorsWithSchedule([FromQuery] string? department = null)
    {
        var doctors = await _userService.GetDoctorsWithScheduleAsync(department);
        return Ok(doctors);
    }

    [HttpGet("{id}")]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Returns a specific user by ID.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the requested user.", typeof(UserDto))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Returned if the user does not exist.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Returned if the user doesn't have the correct permissions to execute the action.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserAsync(id);
        if(user == null)
        {
            return NotFound();
        }
        return Ok(UserProfiles.EntityToDto(user));
    }

    [HttpPost]
    [Produces("application/json")]
    [SwaggerOperation("Creates a new user.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the created user.", typeof(CreateUserDto))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Returned if the user does not exist.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Returned if the user doesn't have the correct permissions to execute the action.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto user)
    {
        var errors = UserDtoValidation.IsValid(user);
        if (errors.Any())
        {
            return BadRequest(errors);
        }

        var userEntity = UserProfiles.DtoToEntity(user);

        if(user.HospitalIds != null && user.HospitalIds.Any())
        {
            var hospitals = await Task.WhenAll(
                user.HospitalIds.Select(id => _hospitalService.GetAsync(id))
            );
            userEntity.Hospitals = hospitals.Where(h => h != null).ToList();
        }

        var createdUser = await _userService.AddAsync(userEntity);
        return CreatedAtAction(
            nameof(GetUserById),
            new { id = createdUser.Id },
            UserProfiles.EntityToDto(createdUser));
    }

    [HttpPut("{id}")]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Modify a specific user.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the modified user.", typeof(CreateUserDto))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Returned if the user does not exist.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Returned if the user doesn't have the correct permissions to execute the action.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] CreateUserDto user)
    {
        var existingUser = await _userService.GetUserAsync(id);
        if (existingUser == null)
        {
            return NotFound();
        }

        if(user.HospitalIds.Count > 0)
        {
            var hospitals = await Task.WhenAll(
                user.HospitalIds.Select(id => _hospitalService.GetAsync(id))
            );
            existingUser.Hospitals = hospitals.Where(h => h != null).ToList();
        }

        var updatedUser = await _userService.UpdateAsync(existingUser);
        return Ok(UserProfiles.EntityToDto(updatedUser));
    }

    [HttpDelete("{id}")]
    [Authorize]
    [Produces("application/json")]
    [SwaggerOperation("Deletes a specific user by ID.")]
    [SwaggerResponse((int)HttpStatusCode.NoContent, "User deleted successfully.")]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Returned if the user does not exist.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Returned if the user doesn't have the correct permissions to execute the action.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var existingUser = await _userService.GetAsync(id);
        if (existingUser == null)
        {
            return NotFound();
        }
        await _userService.DeleteAsync(existingUser);
        return NoContent();
    }
}

