using System.Net.Http.Headers;

namespace DoctorAppointment.Service.MCPTools;

    public static class HttpHandler
    {
        public static HttpClient CreateClient(IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            var client = httpClientFactory.CreateClient("DoctorAppointmentApi");

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

