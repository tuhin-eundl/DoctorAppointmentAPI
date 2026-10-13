using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace DoctorAppointment.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public AuthController(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var validUser = await _userService.IsValidUser(request.UserName, request.Password);
        if (validUser == null)
        {
            return Unauthorized("Invalid username or password");
        }

        // Create JWT
        var key = _configuration["Jwt:Key"] ?? string.Empty;
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("Jwt:Key is not configured");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expireMinutes = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var m) ? m : 60;

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, validUser.Id.ToString()),
            new Claim("name", $"{validUser.FirstName} {validUser.LastName}"),
            new Claim("userName", validUser.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new TokenResponseDto(tokenString, (int)TimeSpan.FromMinutes(expireMinutes).TotalSeconds));
    }

}

