using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoctorAppointment.Service.MCPTools;

#region DTOs for Tool Schema Binding

public record UserDto(
    [property: JsonPropertyName("id"), Description("The unique user ID")] int Id,
    [property: JsonPropertyName("name"), Description("The full name of the user")] string Name,
    [property: JsonPropertyName("email"), Description("The user's email address")] string Email,
    [property: JsonPropertyName("role"), Description("The user's system role (e.g., Patient, Doctor, Admin)")] string Role
);

public record CreateUserRequest(
    [property: JsonPropertyName("name"), Description("The full name of the new user")] string Name,
    [property: JsonPropertyName("email"), Description("The unique email address of the new user")] string Email,
    [property: JsonPropertyName("role"), Description("The assigned system role (e.g., Patient, Doctor, Admin)")] string Role
);

public record UpdateUserRequest(
    [property: JsonPropertyName("name"), Description("The updated full name of the user")] string? Name = null,
    [property: JsonPropertyName("email"), Description("The updated email address of the user")] string? Email = null,
    [property: JsonPropertyName("role"), Description("The updated system role")] string? Role = null
);

#endregion

[McpServerToolType]
public sealed class UserTools(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    private const string BasePath = "/api/user";

    [KernelFunction, Description("Returns a list of all registered system users.")]
    [McpServerTool]
    public async Task<List<UserDto>> GetAllUsers(CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync(BasePath, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(cancellationToken: cancellationToken);
        return users ?? [];
    }

    [KernelFunction, Description("Returns a user by their unique ID.")]
    [McpServerTool]
    public async Task<UserDto?> GetUserById(
        [Description("The unique integer ID of the user to retrieve")] int id,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.GetAsync($"{BasePath}/{id}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description("Creates a new user with name, email, and role details.")]
    [McpServerTool]
    public async Task<UserDto?> CreateUser(
        [Description("The user details required to create the account")] CreateUserRequest user,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PostAsJsonAsync(BasePath, user, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description("Updates an existing user's name, email, or role by their ID.")]
    [McpServerTool]
    public async Task<UserDto?> UpdateUser(
        [Description("The unique integer ID of the user to update")] int id,
        [Description("The updated user fields")] UpdateUserRequest user,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.PutAsJsonAsync($"{BasePath}/{id}", user, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
    }

    [KernelFunction, Description("Deletes a user permanently from the system using their ID.")]
    [McpServerTool]
    public async Task<string> DeleteUser(
        [Description("The unique integer ID of the user to delete")] int id,
        CancellationToken cancellationToken = default)
    {
        var client = HttpHandler.CreateClient(httpClientFactory, httpContextAccessor);

        using var response = await client.DeleteAsync($"{BasePath}/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return $"User with ID {id} deleted successfully.";
    }
}
