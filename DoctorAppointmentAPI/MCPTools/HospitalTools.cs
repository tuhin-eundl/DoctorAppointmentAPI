using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace DoctorAppointment.Service.MCPTools;

[McpServerToolType]
public sealed class HospitalTools(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    private const string BasePath = "/api/hospital";

    [KernelFunction, Description(
        "Lists all hospitals with id, name, address, latitude and longitude. " +
        "Use it to find hospitalIds when creating or updating a user, or when the user asks where a hospital is.")]
    [McpServerTool]
    public async Task<JsonElement> GetHospitals(CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(BasePath, cancellationToken);
        await HttpHandler.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }
}
