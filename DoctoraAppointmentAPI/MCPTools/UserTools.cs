using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace DoctorAppointment.Service.MCPTools;

[McpServerToolType]
public sealed class UserTools(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    private const string BasePath = "/api/user";

    [McpServerTool]
    [Description("Returns a list of all users.")]
    public async Task<JsonElement> GetAllUsers(
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(
            BasePath,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [McpServerTool]
    [Description("Returns a user by their ID.")]
    public async Task<JsonElement> GetUserById(
        [Description("The ID of the user to retrieve")] int id,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(
            $"{BasePath}/{id}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [McpServerTool]
    [Description("Creates a new user.")]
    public async Task<JsonElement> CreateUser(
        [Description("The user data as JSON")] JsonElement user,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PostAsJsonAsync(
            BasePath,
            user,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [McpServerTool]
    [Description("Updates an existing user.")]
    public async Task<JsonElement> UpdateUser(
        [Description("The ID of the user to update")] int id,
        [Description("The updated user data as JSON")] JsonElement user,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PutAsJsonAsync(
            $"{BasePath}/{id}",
            user,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
    }

    [McpServerTool]
    [Description("Deletes a user by ID.")]
    public async Task<string> DeleteUser(
        [Description("The ID of the user to delete")] int id,
        CancellationToken cancellationToken)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.DeleteAsync(
            $"{BasePath}/{id}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return "User deleted successfully.";
    }
}
