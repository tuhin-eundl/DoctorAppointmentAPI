using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Users;
using DoctorAppointment.Application.Profiles.Users;
using DoctorAppointment.Application.Validations.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.ListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetAsync(id);
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserDto user)
    {
        var errors = UserDtoValidation.IsValid(user);
        if (errors.Any())
        {
            return BadRequest(errors);
        }

        var userEntity = UserProfiles.DtoToEntity(user);
        var createdUser = await _userService.AddAsync(userEntity);
        return CreatedAtAction(nameof(CreateUser), UserProfiles.EntityToDto(createdUser));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto user)
    {
        var existingUser = await _userService.GetAsync(id);
        if (existingUser != null)
        {
            return NotFound();
        }

        var userEntity = UserProfiles.DtoToEntity(user);
        var updatedUser = await _userService.UpdateAsync(userEntity);
        return Ok(UserProfiles.EntityToDto(updatedUser));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var existingUser = await _userService.GetAsync(id);
        if (existingUser == null)
        {
            return NotFound();
        }

        await _userService.DeleteAsync(UserProfiles.DtoToEntity(existingUser));
        return NoContent();
    }
}

