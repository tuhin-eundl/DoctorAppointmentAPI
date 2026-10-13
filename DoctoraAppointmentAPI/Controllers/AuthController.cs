using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace DoctorAppointment.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var isValidUser = await _userService.IsValidUser(request.UserName, request.Password);
        if (!isValidUser)
        {
            return Unauthorized("Invalid username or password");
        }

        return Ok();
    }
}

