using System.Net.Http.Headers;

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

        var authorization = httpContextAccessor.HttpContext?
            .Request.Headers.Authorization.ToString();

        if (AuthenticationHeaderValue.TryParse(
                authorization, out var header)
            && header.Scheme.Equals(
                "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            client.DefaultRequestHeaders.Authorization = header;
        }

        return client;
    }
}

