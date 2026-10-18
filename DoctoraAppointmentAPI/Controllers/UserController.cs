using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Application.Profiles.Users;
using DoctorAppointment.Application.Validations.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace DoctorAppointment.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
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
        var user = await _userService.GetAsync(id);
        if(user == null)
        {
            return NotFound();
        }
        return Ok(UserProfiles.EntityToDto(user));
    }

    [HttpPost]
    [Produces("application/json")]
    [SwaggerOperation("Creates a new user.")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the created user.", typeof(UserDto))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Returned if the user does not exist.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Returned if the user doesn't have the correct permissions to execute the action.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> CreateUser([FromBody] UserDto user)
    {
        var errors = UserDtoValidation.IsValid(user);
        if (errors.Any())
        {
            return BadRequest(errors);
        }

        var userEntity = UserProfiles.DtoToEntity(user);
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
    [SwaggerResponse((int)HttpStatusCode.OK, "Returns the modified user.", typeof(UserDto))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Returned if the user does not exist.")]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Returned if no valid credentials are provided.")]
    [SwaggerResponse((int)HttpStatusCode.Forbidden, "Returned if the user doesn't have the correct permissions to execute the action.")]
    [SwaggerResponse((int)HttpStatusCode.InternalServerError, "If any unspecified error occurs.")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto user)
    {
        var existingUser = await _userService.GetAsync(id);
        if (existingUser == null)
        {
            return NotFound();
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

