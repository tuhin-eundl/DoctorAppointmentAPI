using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using DoctorAppointment.Application.Contracts;


namespace DoctorAppointment.Application.Logging;

public class ExecutionContextAccessor : IExecutionContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExecutionContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId => ExtractUserIdFromToken() ?? string.Empty;
    public string UserName => ExtractUserNameFromToken() ?? string.Empty;

    private string? ExtractUserIdFromToken()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return null;

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
    }

    private string? ExtractUserNameFromToken()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return null;

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(c => c.Type == "userName")?.Value;
    }

    private string? GetAccessToken()
    {
        try
        {
            var headers = _httpContextAccessor.HttpContext?.Request.Headers;
            if (headers == null) return null;

            if (!headers.TryGetValue("Authorization", out var authorizationHeader)) return null;
            var auth = authorizationHeader.ToString();
            if (string.IsNullOrEmpty(auth)) return null;

            if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
            var parts = auth.Split(' ', 2);
            return parts.Length == 2 ? parts[1] : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

