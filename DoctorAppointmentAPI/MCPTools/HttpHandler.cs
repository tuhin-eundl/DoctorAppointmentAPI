using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace DoctorAppointment.Service.MCPTools;

public static class HttpHandler
{
    public static HttpClient CreateClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        var client = httpClientFactory.CreateClient("DoctorAppointmentApi");
        // If the current HTTP request is available, prefer using its scheme/host
        // so calls to local endpoints use the actual runtime port (avoids 404 when
        // Api:BaseUrl points to a different port than the application is running on).
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.Request != null)
        {
            try
            {
                var request = httpContext.Request;
                var baseUri = new Uri($"{request.Scheme}://{request.Host.Value}");
                client.BaseAddress = baseUri;
            }
            catch
            {
                // If anything goes wrong building the URI, fall back to configured BaseAddress
            }
        }

        // Forward the caller's bearer token so every internal request runs as the authorized user.
        var authorization = httpContext?.Request.Headers.Authorization.ToString();

        if (AuthenticationHeaderValue.TryParse(
                authorization, out var header)
            && header.Scheme.Equals(
                "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            client.DefaultRequestHeaders.Authorization = header;
        }

        return client;
    }

    /// <summary>
    /// Reads the authenticated caller's user ID from the validated token claims.
    /// </summary>
    public static int? GetCurrentUserId(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(sub, out var userId) ? userId : null;
    }

    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"API returned {(int)response.StatusCode} ({response.StatusCode}): {body}",
            null,
            response.StatusCode);
    }
}

